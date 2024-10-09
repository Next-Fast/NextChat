using HarmonyLib;
using Hazel;
using Il2CppInterop.Runtime.Injection;
using InnerNet;
using Next_Chat.Core;
using Next_Chat.Default;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Next_Chat.Patches;

[Harmony]
public class VoicePatch
{
    public static GameObject? MicButton { get; internal set; }
    
    public static PassiveButton? MicPassiveButton { get; internal set; }
    
    public static SpriteRenderer? MicButtonSpriteRenderer { get; internal set; }
    
    public static TextMeshPro? MicButtonText { get; internal set; }
    
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.ToggleUseAndPetButton)), HarmonyPostfix]
    private static void VoiceButtonPatch(HudManager __instance)
    {
        if (MicButton) return;
        var Button = __instance.KillButton;
        if (!Button) return;
        
        MicButton = Object.Instantiate(Button.gameObject, Button.transform.parent);
        MicButton.name = "MicButton";
        MicButtonSpriteRenderer = MicButton.GetComponent<SpriteRenderer>();
        MicButton.GetComponent<KillButton>().Destroy();
        var text2 = MicButton.transform.GetChild(2);
        MicButtonText = text2.GetComponent<TextMeshPro>();
        MicButton.DestroyAllChildren<TextTranslatorTMP>();
        MicPassiveButton = MicButton.GetComponent<PassiveButton>();     
        MicPassiveButton.OnClick = new Button.ButtonClickedEvent();
        MicPassiveButton.OnClick.AddListener(() =>
        {
            LocalPlayer.Instance?.SetMicState();
        });
        MicButton.SetActive(true);
        MicButton.GetComponent<BoxCollider2D>().size = MicButtonSpriteRenderer.size;
        UpdateSprite();
    }
    
    public static void UpdateSprite()
    {
        if (MicButtonSpriteRenderer == null || MicButtonText == null) return;
        MicButtonSpriteRenderer.sprite = LocalPlayer.MicEnabled ? Sprites.MicOn : Sprites.MicOff;
        MicButtonText.text = LocalPlayer.MicEnabled ? "开麦" : "闭麦";
        LogInfo("UpdateMicSprite");
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start)), HarmonyPostfix]
    private static void GameStartManagerPatch(GameStartManager __instance)
    {
        NextVoiceManager.Instance.GetAllComponents();
        NextVoiceManager.Instance.SetDefault();
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined)), HarmonyPostfix]
    private static void OnPlayerJoinedPatch(AmongUsClient __instance, ClientData data)
    {
        if (LocalPlayer.Instance == null) return;
        RPCFlag.SyncConfig.SendRpcToPlayer(SendOption.None, write =>
        {
            write.Write(LocalPlayer.Instance.player.PlayerId);
            NextVoiceManager.Instance._Config!.RpcWrite(write);
        }, data.Id);
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined)), HarmonyPostfix]
    private static void OnGameJoinedPatch(AmongUsClient __instance)
    {
        if (LocalPlayer.Instance == null) return;
        RPCFlag.SyncConfig.SendRpcToAll(SendOption.None, write =>
        {
            write.Write(LocalPlayer.Instance.player.PlayerId);
            NextVoiceManager.Instance._Config!.RpcWrite(write);
        });
    }
}
