using System;
using CharmMagnet.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CharmMagnet.Tests;

[TestClass]
public class MagnetRulesTests
{
	private static CandidateInfo Loose(float distance = 3f)
	{
		return new CandidateInfo { IsTinyOrSmall = true, DistanceToCart = distance };
	}

	[TestMethod]
	public void LooseSmallNearby_IsEligible()
	{
		Assert.IsTrue(MagnetRules.IsEligible(Loose(), 6f));
	}

	[TestMethod]
	public void ExactlyAtRadius_IsEligible_BeyondIsNot()
	{
		Assert.IsTrue(MagnetRules.IsEligible(Loose(6f), 6f));
		Assert.IsFalse(MagnetRules.IsEligible(Loose(6.01f), 6f));
	}

	[TestMethod]
	public void BiggerSizes_AreNotEligible()
	{
		CandidateInfo medium = Loose();
		medium.IsTinyOrSmall = false;
		Assert.IsFalse(MagnetRules.IsEligible(medium, 6f));
	}

	[TestMethod]
	public void EachExclusion_BlocksTheValuable()
	{
		CandidateInfo c = Loose();
		c.IsHeld = true;
		Assert.IsFalse(MagnetRules.IsEligible(c, 6f), "held");
		c = Loose();
		c.IsPocketed = true;
		Assert.IsFalse(MagnetRules.IsEligible(c, 6f), "pocketed");
		c = Loose();
		c.IsInCart = true;
		Assert.IsFalse(MagnetRules.IsEligible(c, 6f), "in cart");
		c = Loose();
		c.CommittedToExtraction = true;
		Assert.IsFalse(MagnetRules.IsEligible(c, 6f), "committed");
		c = Loose();
		c.InExtractionRoom = true;
		Assert.IsFalse(MagnetRules.IsEligible(c, 6f), "extraction room");
		c = Loose();
		c.IsInactiveOrParked = true;
		Assert.IsFalse(MagnetRules.IsEligible(c, 6f), "inactive");
		c = Loose();
		c.IsDead = true;
		Assert.IsFalse(MagnetRules.IsEligible(c, 6f), "dead");
		c = Loose();
		c.IsCarriersMagnet = true;
		Assert.IsFalse(MagnetRules.IsEligible(c, 6f), "the magnet itself");
		c = Loose();
		c.RecentlyMoved = true;
		Assert.IsFalse(MagnetRules.IsEligible(c, 6f), "recently moved");
	}

	[TestMethod]
	public void PlaceInBox_StaysInsideTheBox_ForAnyInput()
	{
		for (int level = 0; level < 8; level++)
		{
			foreach (float u in new[] { -1f, 0f, 0.3f, 1f, 2f })
			{
				foreach (float v in new[] { 0f, 0.5f, 1f })
				{
					LocalOffset p = MagnetRules.PlaceInBox(1.2f, 0.8f, 1.6f, 0.1f, 0.05f, 0.1f, u, v, level);
					Assert.IsTrue(Math.Abs(p.X) + 0.1f <= 0.6f + 1e-4f);
					Assert.IsTrue(Math.Abs(p.Z) + 0.1f <= 0.8f + 1e-4f);
					Assert.IsTrue(p.Y - 0.05f >= -0.4f - 1e-4f);
					Assert.IsTrue(p.Y + 0.05f <= 0.4f + 1e-4f);
				}
			}
		}
	}

	[TestMethod]
	public void PlaceInBox_LevelZero_SitsAboveTheFloorGap()
	{
		LocalOffset p = MagnetRules.PlaceInBox(1f, 1f, 1f, 0.1f, 0.1f, 0.1f, 0.5f, 0.5f, 0);
		Assert.AreEqual(-0.5f + MagnetRules.FloorGap + 0.1f, p.Y, 1e-5f);
		Assert.AreEqual(0f, p.X, 1e-5f);
		Assert.AreEqual(0f, p.Z, 1e-5f);
	}

	[TestMethod]
	public void PlaceInBox_HigherLevels_AreHigherUntilTheTop()
	{
		float previous = float.MinValue;
		for (int level = 0; level < 3; level++)
		{
			LocalOffset p = MagnetRules.PlaceInBox(1f, 2f, 1f, 0.1f, 0.1f, 0.1f, 0.5f, 0.5f, level);
			Assert.IsTrue(p.Y > previous);
			previous = p.Y;
		}

		LocalOffset top = MagnetRules.PlaceInBox(1f, 0.5f, 1f, 0.1f, 0.1f, 0.1f, 0.5f, 0.5f, 50);
		Assert.AreEqual(0.25f - 0.1f, top.Y, 1e-5f);
	}

	[TestMethod]
	public void PlaceInBox_ValuableBiggerThanBox_IsCentred()
	{
		LocalOffset p = MagnetRules.PlaceInBox(0.2f, 0.2f, 0.2f, 0.5f, 0.5f, 0.5f, 0.9f, 0.9f, 0);
		Assert.AreEqual(0f, p.X);
		Assert.AreEqual(0f, p.Y);
		Assert.AreEqual(0f, p.Z);
	}

	[TestMethod]
	public void IsInsideBox_DetectsInsideAndOutside()
	{
		Assert.IsTrue(MagnetRules.IsInsideBox(new LocalOffset(0.1f, 0f, -0.2f), 1f, 1f, 1f));
		Assert.IsFalse(MagnetRules.IsInsideBox(new LocalOffset(0.6f, 0f, 0f), 1f, 1f, 1f));
	}

	[TestMethod]
	public void NextLevel_CyclesThroughTheLayers()
	{
		Assert.AreEqual(0, MagnetRules.NextLevel(0, 3));
		Assert.AreEqual(2, MagnetRules.NextLevel(2, 3));
		Assert.AreEqual(0, MagnetRules.NextLevel(3, 3));
		Assert.AreEqual(0, MagnetRules.NextLevel(5, 0));
	}
}
