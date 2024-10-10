using HarmonyLib;
using InnerNet;
using Next_Chat.Core;
using UnityEngine;

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
    
    [HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.SetTargetPlayerId)), HarmonyPostfix]
    private static void MeetingVCFramePatch(PlayerVoteArea __instance)
    {
        try
        {
            if (!__instance.TryGetComponent(out VCFrame frame))
                frame = UnityHelper.CreateObject<VCFrame>("VCFrame", __instance.transform, new Vector3(0, 0, -0.5f));

            if (!frame.Renderer)
                frame.Renderer = UnityHelper.CreateObject<SpriteRenderer>("VCFrameSprite", __instance.transform, new Vector3(0, 0, -0.5f));

            if (!frame) return;
            frame.Renderer!.sprite = Sprites.MeetingVCFrame;
            frame.Renderer.color = Color.clear;
                
            var client = NextVoiceManager.Instance.GetPlayer(__instance.TargetPlayerId);
            frame.SetPlayer(__instance, client);
        }
        catch
        {
            LogWarning($"Create VCFrame failed Id:{__instance.TargetPlayerId}");
        }
    }
}

public class VCFrame : MonoBehaviour
{
    public SpriteRenderer? Renderer;
    public INextPlayer Player = null!;
    public float alpha;
    public Color col;
    public void SetPlayer(PlayerVoteArea area, INextPlayer player)
    {
        Player = player;
        col = Palette.PlayerColors[area.TargetPlayerId];
        if(Mathf.Max(col.r, col.g, col.b) < 100) 
            col = Color.Lerp(col, Color.white, 0.4f);
    }

    public void Update()
    {
        if (!Renderer) return;
        alpha = Player.IsSpeaking ? 
            Mathf.Clamp(alpha + Time.deltaTime * 4f, 0f, 1f) 
            : 
            Mathf.Clamp(alpha - Time.deltaTime * 4f, 0f, 1f);
        col.a = (byte)(alpha * 255f);
        Renderer!.color = col;
    }
}