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
		private float timer;

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
			PhysGrabCart[]? carts = null;
			CharmsApi.ForEachCarrier(Charm, (avatar, magnet) =>
			{
				if (carts == null)
				{
					carts = Object.FindObjectsByType<PhysGrabCart>(FindObjectsSortMode.None);
				}

				PullForCarrier(avatar, magnet, carts);
			});
		}

		private void PullForCarrier(PlayerAvatar avatar, ValuableObject magnet, PhysGrabCart[] carts)
		{
			PhysGrabCart? cart = FindHeldCart(avatar, carts);
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
					Charm.LogDebug($"Cart Magnet pulled {entry.Value.name} ({entry.Key:0.0} m) into the cart held by {avatar.name}");
				}
			}
		}

		/// <summary>
		/// The cart this player holds. Looked up through <c>PhysGrabObject.playerGrabbing</c> of each cart, because the host fills it
		/// for every player (GrabPlayerAddRPC) whereas <c>PhysGrabber.grabbedPhysGrabObject</c> may only be set on the owner's machine.
		/// </summary>
		private static PhysGrabCart? FindHeldCart(PlayerAvatar avatar, PhysGrabCart[] carts)
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

			candidates.Sort((a, b) => a.Key.CompareTo(b.Key));
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
			foreach (Collider collider in valuable.GetComponentsInChildren<Collider>())
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

			Vector3 extents = any ? bounds.extents : new Vector3(0.1f, 0.1f, 0.1f);
			Vector3 pivotOffset = any ? bounds.center - valuable.transform.position : Vector3.zero;

			int level = MagnetRules.NextLevel(cart.itemsInCart.Count + RecentCountFor(cart), StackLevels);
			LocalOffset offset = MagnetRules.PlaceInBox(size.x, size.y, size.z, extents.x, extents.y, extents.z, Random.value, Random.value, level);
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
