using System.Linq;
using System.Threading.Tasks;
using BepInEx;
using DG.Tweening;
using Fusion;
using Fusion.Photon.Realtime;
using HarmonyLib;
using UnityEngine;

namespace ArchEmperorLib;

public class OnlinePatch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(NetworkRunner), nameof(NetworkRunner.StartGame))]
	public static void NetRunner_StartGame(NetworkRunner __instance, ref StartGameArgs args)
	{
		if (ModRegistry.IsVanillaCompatible())
		{
			Plugin.LogInfo("No online required mods loaded, starting room in vanilla mode.");
			return;
		}

		if (args.SessionProperties != null)
		{
			if (args.SessionProperties.ContainsKey("ArchEmperorVersion"))
			{
				args.SessionProperties["ArchEmperorVersion"] = MyPluginInfo.PLUGIN_VERSION;
				args.SessionProperties["ArchEmperorManifest"] = ModRegistry.ComputeDigest();//string.Join(':', RuntimeData.GetOnlineManifests());
			}
			else
			{
				args.SessionProperties.Add("ArchEmperorVersion", MyPluginInfo.PLUGIN_VERSION);
				args.SessionProperties.Add("ArchEmperorManifest", ModRegistry.ComputeDigest());
			}
		}

		FusionAppSettings fusionAppSettings = PhotonAppSettings.Global.AppSettings.GetCopy();
		fusionAppSettings.UseNameServer = true;
		fusionAppSettings.AppVersion = "ArchEmperor." + Application.version;

		string region = MatchmakingUI.ins.GetCurrentRegion();
		bool bVar1 = region.IsNullOrWhiteSpace();
		if (!bVar1)
		{
			region = region.ToLower();
			fusionAppSettings.FixedRegion = region;
		}

		args.CustomPhotonAppSettings = fusionAppSettings;

		Plugin.LogDebug("Session Name: " + args.SessionName);
		Plugin.LogDebug("Mod Version: " + args.CustomPhotonAppSettings.AppVersion);
		//Plugin.LogDebug("Mod Manifests: " + string.Join(':', LobbyCompatibilityRegistry.BuildManifest())); //RuntimeData.GetOnlineManifests()));
		if (args.SessionProperties != null) foreach (var prop in args.SessionProperties) Plugin.LogDebug($"PROP {prop.Key} : {prop.Value.ToString()}");

	}


	// [HarmonyPrefix]
	// [HarmonyPatch(typeof(NetworkManager._JoinLobby_d__41), nameof(NetworkManager._JoinLobby_d__41.MoveNext))]
	public static async Task NetRunner_JoinSessionLobby(NetworkManager __instance)//NetworkManager._JoinLobby_d__41 __instance, ref bool __runOriginal)
	{
		// if (__instance.__1__state != 0) __runOriginal = false;
		NetworkRunner NetRunner = UnityEngine.Object.Instantiate(__instance.networkRunnerPrefab);
		__instance.networkRunner = NetRunner;

		INetworkRunnerCallbacks[] callbacks = [null];
		callbacks[0] = __instance.Cast<INetworkRunnerCallbacks>();

		NetRunner.AddCallbacks(callbacks);

		PhotonAppSettings PhotonAppSets = PhotonAppSettings.Global;
		FusionAppSettings FusionAppSets = PhotonAppSets.AppSettings;

		FusionAppSets = FusionAppSets.GetCopy();

		FusionAppSets.UseNameServer = true;
		FusionAppSets.AppVersion = (!ModRegistry.IsVanillaCompatible() ? "ArchEmperor." : "") + Application.version;

		string region = MatchmakingUI.ins.GetCurrentRegion();
		bool bVar1 = region.IsNullOrWhiteSpace();
		if (!bVar1)
		{
			region = region.ToLower();
			FusionAppSets.FixedRegion = region;
		}

		var task = NetRunner.JoinSessionLobby(SessionLobby.Shared, null, null, FusionAppSets, new(false), new(null), true);
		// task.Wait(5000);
		await task;

		// __instance.__1__state = 0;

		// 	mscorlib.dll::System::Runtime::CompilerServices::AsyncTaskMethodBuilder::
		//   AsyncTaskMethodBuilder_AwaitUnsafeOnCompleted_33
		// 			  (&__instance.__t__builder, &local_res20,
		// 			   (UnityServicesInternal_c_DisplayClass33_0_InitializeServicesAsync_g_InitializePackagesAsync_1_d*)__instance,
		// 			   void_MethodInfo::System::Runtime::CompilerServices::AsyncTaskMethodBuilder::AwaitUnsafeOnCompleted<System::Runtime::CompilerServices::TaskAwaiter<Fusion::StartGameResult>, _NetworkManager::_JoinLobby_d__41>(System::Runtime::CompilerServices::TaskAwaiter<Fusion::StartGameResult> &, _NetworkManager::_JoinLobby_d__41 &)
		// 			  );

		// End of routine block in Ghidra

		bool ok = task.Result.Ok;
		if (ok)
		{
			Plugin.LogDebug("Connected to lobby.");
			MatchmakingUI.ins.ShowInLobby();
		}
		else
		{
			__instance.ForceDisconnectRoutine();
		}
		// __instance.__1__state = -2;
		UI_Loading.ins.EnableLoading(false);
		return;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(NetworkManager._JoinLobby_d__41), nameof(NetworkManager._JoinLobby_d__41.MoveNext))]
	public static void NetManager_JoinLobby(NetworkManager._JoinLobby_d__41 __instance, ref bool __runOriginal)
	{
		// Plugin.LogInfo("B Pre-State: " + __instance.__1__state);
		__runOriginal = false;
		_ = NetRunner_JoinSessionLobby(__instance.__4__this);
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(NetworkManager), nameof(NetworkManager.OnSessionListUpdated))]
	public static void NetManager_OnSessionListUpdated(NetworkManager __instance, ref NetworkRunner runner, ref Il2CppSystem.Collections.Generic.List<SessionInfo> sessionList)
	{
		if (ModRegistry.IsVanillaCompatible()) return;
		Plugin.LogDebug("OnSessionListUpdated:" + sessionList.Count);
		sessionList.RemoveAll((Il2CppSystem.Predicate<SessionInfo>)ValidateVersion);
		Plugin.LogDebug("OnSessionListUpdated Filtered:" + sessionList.Count);
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(MatchmakingUI), nameof(MatchmakingUI.DisconnectInLooby))]
	[HarmonyPatch(typeof(MatchmakingUI), nameof(MatchmakingUI.DisconnectInSession))]
	[HarmonyPatch(typeof(MatchmakingUI), nameof(MatchmakingUI.DisconnectInCoopSession))]
	public static void MatchmakingUI_DisconnectInLooby(MatchmakingUI __instance)
	{
		__instance.CloseAll(); // Bug with JoinLobby's MoveNext patch.
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(MatchmakingUI), nameof(MatchmakingUI.IS_MoveDesigner))]
	[HarmonyPatch(typeof(MatchmakingUI), nameof(MatchmakingUI.HideInSessionForMatchStart))]
	public static void MatchmakingUI_HideInSession(MatchmakingUI __instance)
	{
		__instance.InSession_Gp.DOFade(0, 0.5f).WaitForCompletion();
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(MatchmakingUI), nameof(MatchmakingUI.CloseInLobby))]
	public static void MatchmakingUI_CloseInLobby(MatchmakingUI __instance)
	{
		__instance.InLobby_Gp.DOFade(0, 0.5f).WaitForCompletion();
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(MatchmakingUI), nameof(MatchmakingUI.ShowInSession))]
	public static void MatchmakingUI_ShowInSession(MatchmakingUI __instance)
	{
		__instance.mainMenu.MainMenuGp.enabled = true;
	}

	private static bool ValidateVersion(SessionInfo sesh)
	{
		// Match core version. True = remove room.
		if (!sesh.Properties.ContainsKey("ArchEmperorVersion") || sesh.Properties["ArchEmperorVersion"] != MyPluginInfo.PLUGIN_VERSION) return true;

		// Match manifests in room, or skip rooms without manifests if you have any.
		if (!sesh.Properties.ContainsKey("ArchEmperorManifest") || sesh.Properties["ArchEmperorManifest"] != ModRegistry.ComputeDigest()) return true;

		return false;
	}
	

	// public static bool SendBundleInfo(NetworkManager __instance)
	// {
	// 	if (__instance.IsHostOrOffline())
	// 	{
	// 		return true;
	// 	}
	// 	return false;
	// }

	// WIP, technical challenge
	// [HarmonyPrefix]
	// [HarmonyPatch(typeof(NetworkManager), nameof(.RPC_))]
	private static unsafe void RPC_SendBundleManifest()
	{
		if (NetworkManager.ins.IsHostOrOffline()) return;

		NetworkBool dummy = true;

		var runner = NetworkManager.ins.networkRunner;

		SimulationMessage* message = SimulationMessage.Allocate(NetworkManager.ins.networkRunner.Simulation, 12); // I don't know what the capacity should be.

		NetworkId id = new(0);
		if (NetworkManager.ins.mySync._object) id.Raw = (uint)NetworkManager.ins.mySync._object.Ptr;

		RpcHeader header = RpcHeader.Create(id, NetworkManager.ins.mySync.ObjectIndex, 6); // Method 6? Are the methods registered as ints during compilation? Can I add my own?
		*(RpcHeader*)(message + 1) = header;
		message[1].Capacity = dummy._value;
		message->Offset = 96; // For no data, appears to be 64. Capicity is also not touched then.

		NetworkManager.ins.networkRunner.SendRpc(message);

		// SimulationMessage message = new()
		// {
		// 	Capacity = 4,
		// 	Source = NetworkManager.ins.myPlayer,
		// 	Flags = SimulationMessage.BuiltInFlags.USER_MESSAGE & SimulationMessage.BuiltInFlags.USER_MESSAGE,
		// 	Target =  
		// };
	}
}
