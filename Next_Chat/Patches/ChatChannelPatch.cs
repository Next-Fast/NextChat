using System.Text.RegularExpressions;
using AmongUs.QuickChat;
using HarmonyLib;
using Hazel;
using Next_Chat.Core;
using Next_Chat.Default;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;


namespace Next_Chat.Patches;

[Harmony]
internal static class ChatChannelPatch
{
    public static GameObject?  ChatChannelButton { get; internal set; }
    public static PassiveButton? ChatChannelPassiveButton { get; internal set; }
    
    public static SpriteRenderer? ChatChannelButtonSpriteRenderer { get; internal set; }
    
    
    internal static void UpdateChannelButton()
    {
        if (ChatChannelButtonSpriteRenderer) 
            ChatChannelButtonSpriteRenderer!.sprite = ChatChannelManager.Instance.CurrentChannel;
        if (ChatChannelManager.Instance.hasNext)
        {
            ChatChannelManager.Instance.UpdateChatBubbles();
        }
    }
    
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Toggle)), HarmonyPostfix]
    private static void ChatControllerAwake(ChatController __instance)
    {
        if (ChatChannelButton) return;
        var banMenuButton = __instance.banButton.transform.parent.Find("BanMenuButton").gameObject;
        ChatChannelButton = Object.Instantiate(banMenuButton, banMenuButton.transform.parent, true);
        ChatChannelButton.name = nameof(ChatChannelButton);
        ChatChannelButton.transform.localPosition += new Vector3(0, 0.7f, 0);
        ChatChannelPassiveButton = ChatChannelButton.GetComponent<PassiveButton>();
        ChatChannelButton.transform.GetChild(1).gameObject.Destroy();
        var readerObj = ChatChannelButton.transform.GetChild(0).gameObject;
        readerObj.transform.localPosition += new Vector3(0, 0.06f, 0);
        ChatChannelButtonSpriteRenderer = readerObj.GetComponent<SpriteRenderer>();
        ChatChannelPassiveButton.OnClick = new Button.ButtonClickedEvent();
        ChatChannelPassiveButton.OnClick.AddListener(() =>
        {
            ChatChannelManager.Instance.NextChannel();
            UpdateChannelButton();
        });
        UpdateChannelButton();

    }
    
    
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSendChat)), HarmonyPrefix]
    private static bool SendChatPrefix(PlayerControl __instance, string chatText, ref bool __result)
    {
        chatText = Regex.Replace(chatText, "<.*?>", string.Empty);
        if (string.IsNullOrWhiteSpace(chatText))
        {
            __result = false;
            return false;
        }
        if (AmongUsClient.Instance.AmClient && DestroyableSingleton<HudManager>.Instance)
        {
            ChatChannelManager.Instance.AddChat(__instance, chatText, ChatChannelManager.Instance.CurrentChannel);
        }
        var messageWriter = AmongUsClient.Instance.StartRpc(__instance.NetId, 13);
        messageWriter.Write(chatText);
        messageWriter.Write(ChatChannelManager.Instance.CurrentChannelIndex);
        messageWriter.EndMessage();
        __result = true;
        return false;
    }
    
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSendQuickChat)), HarmonyPrefix]
    private static bool SendQuickChatPrefix(PlayerControl __instance, QuickChatPhraseBuilderResult data, ref bool __result)
    {
        var text = data.ToChatText();
        if (string.IsNullOrWhiteSpace(text) || !data.IsValid())
        {
            __result = false;
        }
        if (AmongUsClient.Instance.AmClient && DestroyableSingleton<HudManager>.Instance)
        {
            ChatChannelManager.Instance.AddChat(__instance, text, ChatChannelManager.Instance.CurrentChannel,false);
        }
        var messageWriter = AmongUsClient.Instance.StartRpc(__instance.NetId, 33);
        QuickChatNetData.Serialize(data, messageWriter);
        messageWriter.Write(ChatChannelManager.Instance.CurrentChannelIndex);
        messageWriter.EndMessage();
        return false;
    }
    
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc)), HarmonyPrefix]
    private static bool ChatPRCPrefix(PlayerControl __instance, byte callId, MessageReader reader)
    {
        if (callId is not 11 and not 33) return true;
        
        if (callId == 11)
        {
            var text = reader.ReadString();
            var channel = reader.ReadInt32();
            if (DestroyableSingleton<HudManager>.Instance)
            {
                ChatChannelManager.Instance.AddChat(__instance, text, channel);
            }
        }

        if (callId == 33)
        {
            var quickChatPhraseBuilderResult = QuickChatNetData.Deserialize(reader);
            var channel = reader.ReadInt32();
            if (DestroyableSingleton<HudManager>.Instance)
            {
                ChatChannelManager.Instance.AddChat(__instance, quickChatPhraseBuilderResult.ToChatText(), channel, false);
            }
        }
        
        return false;
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.AlignAllBubbles)), HarmonyPrefix]
    private static bool AlignAllBubblesPrefix(ChatController __instance)
    {
        ChatChannelManager.Instance.UpdateChatBubbles();
        return false;
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.OnGameStart))]
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
    [HarmonyPostfix]
    private static void ChatSetVisible() => ChatChannelManager.Instance.UpdateChatVisible();

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.ServerStart)), HarmonyPostfix]
    private static void OnMeetingHudStart() => ChatChannelManager.Instance.UpdateCanUseChannels();
}