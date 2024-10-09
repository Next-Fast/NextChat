using HarmonyLib;
using InnerNet;

namespace Next_Chat.Patches;

[Harmony]
public class PlayerPatch
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined)), HarmonyPostfix]
    private static void OnPlayerCreate(ClientData data)
    {
        NextVoiceManager.Instance.CreatePlayer(data.Character);
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.OnDestroy)), HarmonyPrefix]
    private static void OnPlayerDestroy(PlayerControl __instance)
    {
        NextVoiceManager.Instance.Players.Where(n => n.player == __instance).Do(n => n.Dispose());
    }
}