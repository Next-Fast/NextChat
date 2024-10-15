using HarmonyLib;
using Hazel;
using InnerNet;
using Next_Chat.Core;
using Next_Chat.Default;
using UnityEngine;

namespace Next_Chat.Patches;

[Harmony]
public class PlayerPatch
{
    public static PlayersOverlay? Overlay { get; set; }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined)), HarmonyPostfix]
    private static void OnPlayerJoinedPatch(ClientData data)
    {
        NextVoiceManager.Instance.CreatePlayer(data.Character);
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update)), HarmonyPostfix]
    private static void UpdatePatch()
    {
        Overlay ??= new PlayersOverlay(n => NextVoiceManager.Instance.GetPlayer(n.player!.PlayerId).IsSpeaking);
    }
    
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined)), HarmonyPostfix]
    private static void OnPlayerCreate(ClientData data)
    {
        RPCFlag.HasInstall.SendRpcToAll(SendOption.None, writer => writer.Write(LocalPlayer.Instance!.player.PlayerId) );
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.OnDestroy)), HarmonyPrefix]
    private static void OnPlayerDestroy(PlayerControl __instance)
    {
        foreach (var p in NextVoiceManager.Instance.Players.Where(n => n.player == __instance))
        {
            p.Dispose();
        }
        
        var icon = PlayersOverlay.Instance?._AllInstance.FirstOrDefault(n => n.player == __instance);
        if (icon is null) return;
        icon.player = null;
    }
    
    [HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.SetTargetPlayerId)), HarmonyPostfix]
    private static void MeetingVCFramePatch(PlayerVoteArea __instance)
    {
        var player = NextVoiceManager.Instance.GetPlayer(__instance.TargetPlayerId);
        try
        {
            player.Frame = !__instance.TryGetComponent(out VCFrame _frame) 
                ? _frame 
                : UnityHelper.CreateObject<VCFrame>("VCFrame", __instance.transform, new Vector3(0, 0, -0.5f));


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
    }

    [HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.OnDestroy)), HarmonyPrefix]
    private static void MeetingVCFrameDestroyPatch(PlayerVoteArea __instance)
    {
        var player = NextVoiceManager.Instance.GetPlayer(__instance.TargetPlayerId);
        if (player.Frame is null) return;
        player.Frame.Destroy();
        player.Frame = null;
    }
}