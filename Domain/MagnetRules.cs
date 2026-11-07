using System;

namespace CharmMagnet.Domain
{
	/// <summary>What the magnet needs to know about one loose valuable. Filled from the game by <c>CartMagnet</c>.</summary>
	internal struct CandidateInfo
	{
		internal bool IsTinyOrSmall;
		internal float DistanceToCart;
		internal bool IsHeld;
		internal bool IsPocketed;
		internal bool IsInCart;
		internal bool CommittedToExtraction;
		internal bool InExtractionRoom;
		internal bool IsInactiveOrParked;
		internal bool IsDead;
		internal bool IsCarriersMagnet;
		internal bool RecentlyMoved;
	}

	/// <summary>A position relative to the cart volume centre, along the volume's own axes (metres).</summary>
	internal readonly struct LocalOffset
	{
		internal LocalOffset(float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		internal float X { get; }

		internal float Y { get; }

		internal float Z { get; }
	}

	/// <summary>Game-free rules of the Cart Magnet: which valuables are pulled and where inside the cart they land.</summary>
	internal static class MagnetRules
	{
		/// <summary>Gap between the floor of the cart volume and the bottom of a pulled valuable, in metres.</summary>
		internal const float FloorGap = 0.1f;

		/// <summary>Extra space between stacked pieces, in metres.</summary>
		internal const float StackGap = 0.02f;

		/// <summary>True when the magnet may pull this valuable into the cart.</summary>
		internal static bool IsEligible(CandidateInfo candidate, float radius)
		{
			if (!candidate.IsTinyOrSmall || candidate.DistanceToCart > radius)
			{
				return false;
			}

			return !(candidate.IsHeld
				|| candidate.IsPocketed
				|| candidate.IsInCart
				|| candidate.CommittedToExtraction
				|| candidate.InExtractionRoom
				|| candidate.IsInactiveOrParked
				|| candidate.IsDead
				|| candidate.IsCarriersMagnet
				|| candidate.RecentlyMoved);
		}

		/// <summary>True when <paramref name="local"/> already lies inside a volume of the given size centred on the origin.</summary>
		internal static bool IsInsideBox(LocalOffset local, float sizeX, float sizeY, float sizeZ)
		{
			return Math.Abs(local.X) <= sizeX / 2f && Math.Abs(local.Y) <= sizeY / 2f && Math.Abs(local.Z) <= sizeZ / 2f;
		}

		/// <summary>
		/// A safe spot for the CENTRE of a valuable inside the cart volume. <paramref name="u"/> and <paramref name="v"/> (0 to 1) pick
		/// the spot on the floor plane (0.5 is the middle), <paramref name="level"/> stacks pieces upward (0 is on the floor) so that
		/// several pieces do not overlap. The result keeps the valuable's extents inside the volume; when the valuable is bigger
		/// than the volume on an axis it is centred on that axis.
		/// </summary>
		internal static LocalOffset PlaceInBox(float sizeX, float sizeY, float sizeZ, float extentX, float extentY, float extentZ, float u, float v, int level)
		{
			float spanX = Math.Max(0f, sizeX - 2f * extentX);
			float spanZ = Math.Max(0f, sizeZ - 2f * extentZ);
			float x = (Clamp01(u) - 0.5f) * spanX;
			float z = (Clamp01(v) - 0.5f) * spanZ;

			float floorY = -sizeY / 2f + FloorGap + extentY;
			float topY = sizeY / 2f - extentY;
			float y = floorY + Math.Max(0, level) * (2f * extentY + StackGap);
			if (topY < floorY)
			{
				y = 0f;
			}
			else if (y > topY)
			{
				y = topY;
			}

			return new LocalOffset(x, y, z);
		}

		/// <summary>Stack level for the next piece: spreads pieces over <paramref name="levels"/> layers by how many are already placed.</summary>
		internal static int NextLevel(int alreadyPlaced, int levels)
		{
			if (levels < 1)
			{
				return 0;
			}

			return Math.Max(0, alreadyPlaced) % levels;
		}

		private static float Clamp01(float value)
		{
			return value < 0f ? 0f : value > 1f ? 1f : value;
		}
	}
}
