using BepInEx;
using CharmCore.Api;
using UnityEngine;

namespace CharmMagnet
{
	[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
	[BepInDependency("com.osmar.Pockets", BepInDependency.DependencyFlags.HardDependency)]
	[BepInDependency("com.osmar.CharmCore", BepInDependency.DependencyFlags.HardDependency)]
	public class CharmMagnetPlugin : BaseUnityPlugin
	{
		public const string PluginGuid = "com.osmar.CharmMagnet";
		public const string PluginName = "CharmMagnet";
		public const string PluginVersion = "1.0.0";

		internal static new BepInEx.Logging.ManualLogSource Logger { get; private set; } = null!;

		private void Awake()
		{
			Logger = base.Logger;

			// Prevent the plugin from being deleted
			this.gameObject.transform.parent = null;
			this.gameObject.hideFlags = HideFlags.HideAndDontSave;

			// Registering binds Enabled, WearPercent, CooldownSeconds and the Debug section (DebugMode) in this mod's config file.
			MagnetCharm charm = new MagnetCharm();
			CharmsApi.Register(charm, Config);
			CartMagnet runtime = this.gameObject.AddComponent<CartMagnet>();
			runtime.Charm = charm;
			charm.Runtime = runtime;

			Logger.LogInfo($"{PluginName} {PluginVersion} loaded!");
		}
	}
}
