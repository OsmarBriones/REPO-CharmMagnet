using CharmCore.Api;

namespace CharmMagnet
{
	/// <summary>Cart Magnet: while you hold a cart, loose small valuables nearby jump into it.</summary>
	internal sealed class MagnetCharm : Charm
	{
		internal const string PrefabName = "Valuable Desert Magnet";

		internal MagnetCharm()
			: base("Magnet", "Magnet - Cart Magnet", defaultWearPercent: 4f, defaultCooldownSeconds: null, PrefabName)
		{
			Radius = AddSetting("Radius", 6f, 1f, 20f, "Distance in metres from the cart you hold within which loose Tiny and Small valuables are pulled into it.");
			IntervalSeconds = AddSetting("IntervalSeconds", 1f, 0.25f, 10f, "Seconds between pulls. Every pull moves up to MaxPerCarrier valuables.");
			MaxPerCarrier = AddSetting("MaxPerCarrier", 1f, 1f, 5f, "How many valuables each Magnet carrier pulls at every pull (whole numbers). Each valuable moved wears the Magnet by WearPercent.");
		}

		/// <summary>The behaviour that holds the per-level state; set by the plugin.</summary>
		internal CartMagnet? Runtime { get; set; }

		protected override void OnLevelEnded()
		{
			if (Runtime != null)
			{
				Runtime.ClearLevelState();
			}
		}

		/// <summary>Metres from the held cart (1 to 20).</summary>
		internal CharmSetting Radius { get; }

		/// <summary>Seconds between pulls (0.25 to 10).</summary>
		internal CharmSetting IntervalSeconds { get; }

		/// <summary>Valuables moved per pull and per carrier (1 to 5).</summary>
		internal CharmSetting MaxPerCarrier { get; }
	}
}
