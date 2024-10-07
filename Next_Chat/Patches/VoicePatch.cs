using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
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
        var UseButton = GameObject.Find("UseButton").gameObject;
        if (!UseButton) return;
        UpdateSprite();
        return;

        void UpdateSprite()
        {
            if (MicButtonSpriteRenderer == null || MicButtonText == null) return;
            MicButtonSpriteRenderer.sprite = LocalPlayer.MicEnabled ? Sprites.MicOn : Sprites.MicOff;
            MicButtonText.text = LocalPlayer.MicEnabled ? "开麦" : "闭麦";
        }
    }
}
