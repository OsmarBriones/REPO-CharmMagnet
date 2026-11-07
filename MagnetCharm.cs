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
			// Extra host-synced numbers for this power go here, for example:
			// Amount = AddSetting("Amount", 30f, 1f, 100f, "Description for the config file.");
		}
	}
}
