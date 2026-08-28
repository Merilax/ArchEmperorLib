using System.IO;
using ArchEmperorLib.UI;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace ArchEmperorLib;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("BREAK ARTS III.exe")]

public class Plugin : BasePlugin
{
	internal static new ManualLogSource Log;
	public static ConfigEntry<bool> logVerbose;

	public static UniverseLib.AssetBundle assets;

	public override void Load()
	{
		Log = Logger.CreateLogSource("ArchEmperorCore");

		logVerbose = Config.Bind("Debug", "logVerbose", false, "Log additional events for debugging purposes.");

		string bundlePath = Path.Combine(Paths.PluginPath, "ArchEmperorLib", "archemperor_ui");
		if (!File.Exists(bundlePath))
			throw new System.Exception($"AssetBundle could not be located found at: {bundlePath}. Aborting mod initializaton.");

		assets = UniverseLib.AssetBundle.LoadFromFile(bundlePath) ?? throw new System.Exception("Failed to load assets! Please report this bug to the developer, along with any game logs. Aborting mod initializaton.");

		var harmony = Harmony.CreateAndPatchAll(typeof(OnlinePatch));
		harmony.PatchAll(typeof(MainMenuPatch));

		IL2CPPChainloader.AddUnityComponent<ModManager>();

		ModRegistry.Register(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION, RequirementScope.Everyone, VersionStrictness.Exact);
	}
	public static void LogInfo(object data) => Log.LogInfo(data);
	public static void LogDebug(object data) { if (logVerbose.Value == true) Log.LogInfo(data); }
}