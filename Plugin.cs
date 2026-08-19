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
	public static string BUNDLE_ROOT;
	public override void Load()
	{
		Log = BepInEx.Logging.Logger.CreateLogSource("ArchEmperorCore");

		logVerbose = Config.Bind("Debug", "logVerbose", false, "Log additional events for debugging purposes.");

		var harmony = Harmony.CreateAndPatchAll(typeof(OnlinePatch));
	}
	public static void LogInfo(object data) => Log.LogInfo(data);
	public static void LogDebug(object data) { if (logVerbose.Value == true) Log.LogInfo(data); }
}