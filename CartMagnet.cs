using System;
using System.Collections.Generic;
using CharmCore.Api;
using CharmMagnet.Domain;
using Pockets.Api;
using UnityEngine;

namespace CharmMagnet
{
	/// <summary>
	/// Cart Magnet, host side: while a player carries the Magnet in a pocket and is holding a cart, loose Tiny and Small
	/// valuables near the cart are teleported into the cart's volume, a few at a time. Only the master can move a world
	/// valuable, so this runs on the host (or alone) for every carrier; the wear is applied to the carrier's Magnet.
	/// </summary>
	internal sealed class CartMagnet : MonoBehaviour
	{
		/// <summary>How long a moved valuable is ignored, in seconds (covers the delay before the cart notices its new content).</summary>
		private const float RecentSeconds = 3f;

		/// <summary>Layers used to spread pulled pieces over the height of the cart volume.</summary>
		private const int StackLevels = 4;

		private readonly Dictionary<int, MovedRecord> recentlyMoved = new Dictionary<int, MovedRecord>();
		private readonly List<KeyValuePair<float, ValuableObject>> candidates = new List<KeyValuePair<float, ValuableObject>>();
		private readonly List<int> expired = new List<int>();
		private readonly List<PhysGrabCart> carts = new List<PhysGrabCart>();
		private readonly List<Collider> colliderBuffer = new List<Collider>();
		private readonly Action<PlayerAvatar, ValuableObject> pullAction;
		private float timer;
		private float nextCartScan;

		/// <summary>Seconds to wait before searching for carts again after a search that found none or found a destroyed one.</summary>
		private const float CartScanCooldown = 5f;

		private readonly Comparison<KeyValuePair<float, ValuableObject>> ByDistance = (a, b) => a.Key.CompareTo(b.Key);

		public CartMagnet()
		{
			pullAction = PullForCarrier;
		}

		internal MagnetCharm Charm { get; set; } = null!;

		private void Update()
		{
			if (Charm == null || !SemiFunc.IsMasterClientOrSingleplayer() || GameDirector.instance == null || ValuableDirector.instance == null)
			{
				return;
			}

			timer -= Time.deltaTime;
			if (timer > 0f)
			{
				return;
			}

			timer = Charm.IntervalSeconds.Value;
			PruneRecent();
			CharmsApi.ForEachCarrier(Charm, pullAction);
		}

		/// <summary>Forgets everything remembered about the level; called by the charm when a level ends.</summary>
		internal void ClearLevelState()
		{
			recentlyMoved.Clear();
			candidates.Clear();
			expired.Clear();
			carts.Clear();
			colliderBuffer.Clear();
			nextCartScan = 0f;
			timer = 0f;
		}

		/// <summary>
		/// The game keeps no list of carts, so they are searched for once and cached. The search repeats (at most every
		/// <see cref="CartScanCooldown"/> seconds) only when the cache is empty or holds a destroyed cart; the cache is dropped with the level.
		/// </summary>
		private void EnsureCarts()
		{
			bool valid = carts.Count > 0;
			for (int i = 0; valid && i < carts.Count; i++)
			{
				valid = carts[i] != null;
			}

			if (valid || Time.time < nextCartScan)
			{
				return;
			}

			nextCartScan = Time.time + CartScanCooldown;
			carts.Clear();
			// hygiene-ok: the game has no cart registry; searched once per level and cached, rescanned at most every 5 s while invalid.
			carts.AddRange(UnityEngine.Object.FindObjectsByType<PhysGrabCart>(FindObjectsSortMode.None));
		}

		private void PullForCarrier(PlayerAvatar avatar, ValuableObject magnet)
		{
			EnsureCarts();
			PhysGrabCart? cart = FindHeldCart(avatar);
			if (cart == null || cart.inCart == null)
			{
				return;
			}

			float radius = Charm.Radius.Value;
			int max = Mathf.Clamp(Mathf.RoundToInt(Charm.MaxPerCarrier.Value), 1, 5);
			CollectCandidates(cart, magnet, radius);
			int moved = 0;
			foreach (KeyValuePair<float, ValuableObject> entry in candidates)
			{
				if (moved >= max)
				{
					break;
				}

				if (MoveIntoCart(entry.Value, cart))
				{
					moved++;
					Charm.WearValuable(magnet, Charm.WearPercent);
					if (Charm.DebugMode)
					{
						Charm.LogDebug($"Cart Magnet pulled {entry.Value.name} ({entry.Key:0.0} m) into the cart held by {avatar.name}");
					}
				}
			}
		}

		/// <summary>
		/// The cart this player holds. Looked up through <c>PhysGrabObject.playerGrabbing</c> of each cart, because the host fills it
		/// for every player (GrabPlayerAddRPC) whereas <c>PhysGrabber.grabbedPhysGrabObject</c> may only be set on the owner's machine.
		/// </summary>
		private PhysGrabCart? FindHeldCart(PlayerAvatar avatar)
		{
			PhysGrabber grabber = avatar.physGrabber;
			if (grabber == null)
			{
				return null;
			}

			foreach (PhysGrabCart cart in carts)
			{
				if (cart == null)
				{
					continue;
				}

				PhysGrabObject grabObject = cart.GetComponent<PhysGrabObject>();
				if (grabObject != null && grabObject.impactDetector != null && grabObject.impactDetector.isCart && grabObject.playerGrabbing.Contains(grabber))
				{
					return cart;
				}
			}

			return null;
		}

		private void CollectCandidates(PhysGrabCart cart, ValuableObject magnet, float radius)
		{
			candidates.Clear();
			Vector3 center = cart.inCart.position;
			foreach (ValuableObject valuable in ValuableDirector.instance.valuableList)
			{
				if (valuable == null)
				{
					continue;
				}

				CandidateInfo info = Describe(valuable, cart, magnet, center);
				if (MagnetRules.IsEligible(info, radius))
				{
					candidates.Add(new KeyValuePair<float, ValuableObject>(info.DistanceToCart, valuable));
				}
			}

			candidates.Sort(ByDistance);
		}

		private CandidateInfo Describe(ValuableObject valuable, PhysGrabCart cart, ValuableObject magnet, Vector3 cartCenter)
		{
			PhysGrabObject grabObject = valuable.physGrabObject;
			CandidateInfo info = new CandidateInfo
			{
				IsTinyOrSmall = valuable.volumeType == ValuableVolume.Type.Tiny || valuable.volumeType == ValuableVolume.Type.Small,
				IsCarriersMagnet = valuable == magnet,
				CommittedToExtraction = valuable.committedToExtraction,
				IsInactiveOrParked = !valuable.gameObject.activeInHierarchy || grabObject == null,
				RecentlyMoved = recentlyMoved.ContainsKey(valuable.GetInstanceID()),
			};

			if (!info.IsTinyOrSmall || info.IsInactiveOrParked)
			{
				return info;
			}

			Vector3 position = valuable.transform.position;
			info.DistanceToCart = Vector3.Distance(position, cartCenter);
			info.IsInactiveOrParked = position == AssetManager.instance.physDisabledPosition;
			info.IsDead = grabObject!.dead;
			info.IsHeld = grabObject.grabbed || grabObject.playerGrabbing.Count > 0;
			info.IsPocketed = PocketsApi.IsPocketed(valuable);
			info.IsInCart = (grabObject.impactDetector != null && grabObject.impactDetector.inCart) || cart.itemsInCart.Contains(grabObject) || IsInsideVolume(cart, position);
			info.InExtractionRoom = InExtractionRoom(valuable);
			return info;
		}

		private static bool InExtractionRoom(ValuableObject valuable)
		{
			if (valuable.roomVolumeCheck == null)
			{
				return false;
			}

			foreach (RoomVolume room in valuable.roomVolumeCheck.CurrentRooms)
			{
				if (room != null && room.Extraction)
				{
					return true;
				}
			}

			return false;
		}

		private static bool IsInsideVolume(PhysGrabCart cart, Vector3 worldPosition)
		{
			Transform volume = cart.inCart;
			Vector3 local = Quaternion.Inverse(volume.rotation) * (worldPosition - volume.position);
			Vector3 size = volume.localScale;
			return MagnetRules.IsInsideBox(new LocalOffset(local.x, local.y, local.z), size.x, size.y, size.z);
		}

		private bool MoveIntoCart(ValuableObject valuable, PhysGrabCart cart)
		{
			PhysGrabObject grabObject = valuable.physGrabObject;
			Transform volume = cart.inCart;
			Vector3 size = volume.localScale;

			// Extents come from the colliders (world axis-aligned); the pivot may not sit at the middle of the piece.
			Bounds bounds = new Bounds(valuable.transform.position, Vector3.zero);
			bool any = false;
			colliderBuffer.Clear();
			valuable.GetComponentsInChildren(colliderBuffer);
			foreach (Collider collider in colliderBuffer)
			{
				if (collider.isTrigger)
				{
					continue;
				}

				if (!any)
				{
					bounds = collider.bounds;
					any = true;
				}
				else
				{
					bounds.Encapsulate(collider.bounds);
				}
			}

			colliderBuffer.Clear();
			Vector3 extents = any ? bounds.extents : new Vector3(0.1f, 0.1f, 0.1f);
			Vector3 pivotOffset = any ? bounds.center - valuable.transform.position : Vector3.zero;

			int level = MagnetRules.NextLevel(cart.itemsInCart.Count + RecentCountFor(cart), StackLevels);
			LocalOffset offset = MagnetRules.PlaceInBox(size.x, size.y, size.z, extents.x, extents.y, extents.z, UnityEngine.Random.value, UnityEngine.Random.value, level);
			Vector3 centerWorld = volume.position + volume.rotation * new Vector3(offset.X, offset.Y, offset.Z);
			Vector3 target = centerWorld - pivotOffset;

			Rigidbody body = grabObject.rb;
			if (body != null)
			{
				body.linearVelocity = Vector3.zero;
				body.angularVelocity = Vector3.zero;
			}

			grabObject.Teleport(target, valuable.transform.rotation);
			recentlyMoved[valuable.GetInstanceID()] = new MovedRecord(Time.time, cart.GetInstanceID());
			return true;
		}

		private int RecentCountFor(PhysGrabCart cart)
		{
			int id = cart.GetInstanceID();
			int count = 0;
			foreach (MovedRecord record in recentlyMoved.Values)
			{
				if (record.CartId == id)
				{
					count++;
				}
			}

			return count;
		}

		private void PruneRecent()
		{
			expired.Clear();
			foreach (KeyValuePair<int, MovedRecord> entry in recentlyMoved)
			{
				if (Time.time - entry.Value.Time > RecentSeconds)
				{
					expired.Add(entry.Key);
				}
			}

			foreach (int key in expired)
			{
				recentlyMoved.Remove(key);
			}
		}

		private readonly struct MovedRecord
		{
			internal MovedRecord(float time, int cartId)
			{
				Time = time;
				CartId = cartId;
			}

			internal float Time { get; }

			internal int CartId { get; }
		}
	}
}
