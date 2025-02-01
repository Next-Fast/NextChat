using HarmonyLib;
using Hazel;
using InnerNet;
using NextChat.Core;
using NextChat.Default;
using UnityEngine;

namespace NextChat.Patches;

[Harmony]
public class PlayerPatch
{
    /*[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined)), HarmonyPostfix]
    private static void OnPlayerJoinedPatch(ClientData data)
    {
        NextVoiceManager.Instance.CreatePlayer(data.Character);
    }*/

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Awake)), HarmonyPostfix]
    private static void AwakePatch(PlayerControl __instance)
    {
        if (!PlayerControl.AllPlayerControls.Contains(__instance))
            return;
        
        NextVoiceManager.Instance.CreatePlayer(__instance);
    }

    public static bool HasHudManager;
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start)), HarmonyPostfix]
    private static void StartPatch(HudManager __instance)
    {
        HasHudManager = true;
    }
    
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined)), HarmonyPostfix]
    private static void OnPlayerCreate(ClientData data)
    {
        RPCFlag.HasInstall.SendRpcToAll(SendOption.None, writer => writer.Write(LocalPlayer.Instance!.player.PlayerId));
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.OnDestroy)), HarmonyPrefix]
    private static void OnPlayerDestroy(PlayerControl __instance)
    {
        foreach (var p in NextVoiceManager.Instance.Players.Where(n => n.player == __instance))
        {
            p.Dispose();
        }

        /*if (PlayersOverlay.Instance == null)
            return;
        var icon = PlayersOverlay.Instance._AllInstance.FirstOrDefault(n => n.id == __instance.PlayerId);
        if (icon == null) return;
        PlayersOverlay.Instance._AllInstance.Remove(icon);
        icon.Destroy();*/
    }
    
    /*[HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.SetTargetPlayerId)), HarmonyPostfix]
    private static void MeetingVcFramePatch(PlayerVoteArea __instance)
    {
        var player = NextVoiceManager.Instance.GetPlayer(__instance.TargetPlayerId);
        if (player == null) return;
        try
        {
            player.Frame = !__instance.TryGetComponent(out VcFrame _frame) 
                ? _frame 
                : UnityHelper.CreateObject<VcFrame>("VCFrame", __instance.transform, new Vector3(0, 0, -0.5f));


            var frame = player.Frame;
            frame.Renderer ??= UnityHelper.CreateObject<SpriteRenderer>("VCFrameSprite", __instance.transform, new Vector3(0, 0, -0.5f));

            frame.Renderer.sprite = Sprites.MeetingVCFrame;
            frame.Renderer.color = Color.clear;
            
            frame.Player = player;
            frame.SetPlayer(__instance);
        }
        catch
        {
            LogWarning($"Create VCFrame failed Id:{__instance.TargetPlayerId}");
        }
    }*/

    /*[HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.OnDestroy)), HarmonyPrefix]
    private static void MeetingVcFrameDestroyPatch(PlayerVoteArea __instance)
    {
        var player = NextVoiceManager.Instance.GetPlayer(__instance.TargetPlayerId);
        if (player?.Frame is null) return;
        player.Frame.Destroy();
        player.Frame = null;
    }*/
}