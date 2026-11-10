using HarmonyLib;

namespace ArchEmperorLib;

public class GeoramaPatch
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(GeoramaSystem._ChangePoseDelay_d__120), nameof(GeoramaSystem._ChangePoseDelay_d__120.MoveNext))]
	public static void ChangePoseDelay(GeoramaSystem._ChangePoseDelay_d__120 __instance, bool __result)
	{
		if (!__result)
			__instance.__4__this.animationTester.Animator_Play();
	}
}