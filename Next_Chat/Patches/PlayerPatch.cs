using HarmonyLib;

namespace Next_Chat.Patches;

[Harmony]
public class PlayerPatch
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Awake))]
    private static void OnPlayerCreate(PlayerControl __instance)
    {
        NextVoiceManager.Instance.CreatePlayer(__instance);
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.OnDestroy)), HarmonyPrefix]
    private static void OnPlayerDestroy(PlayerControl __instance)
    {
        NextVoiceManager.Instance.Players.Where(n =>n.player == __instance).Do(n => n.Dispose());
    }
}