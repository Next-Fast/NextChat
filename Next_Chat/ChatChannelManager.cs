using AmongUs.Data;
using Next_Chat.Core;
using UnityEngine;

namespace Next_Chat;

public class ChatChannelManager : InstanceClass<ChatChannelManager>
{
    public static readonly List<ChatChannel> DefaultChannels =
    [
        new(Sprites.all, n => MeetingHud.Instance),
        new(Sprites.impostor, n => n.Data.Role.IsImpostor),
        new(Sprites.jackal),
        new(Sprites.lover)
    ];

    private List<ChatChannel> _Channels = [..DefaultChannels];
    
    public IReadOnlyList<ChatChannel> Channels => _Channels;
    
    private List<ChatChannel> _CanUseChannels = [];
    
    public ChatChannel CurrentChannel { get; private set; } = DefaultChannels[0];

    public int Max { get; private set; } 
    public int CurrentIndex { get; private set; }
    
    public int CurrentChannelIndex => _Channels.IndexOf(CurrentChannel);

    public bool hasNext { get; private set; } 

    public void UpdateCanUseChannels()
    {
        if (PlayerControl.LocalPlayer == null) return;
        _CanUseChannels = _Channels.Where(n => n.CanUse?.Invoke(PlayerControl.LocalPlayer) ?? false).ToList();
    }
    
    public void Register(ChatChannel channel)
    {
        if (_Channels.Exists( n => n == channel))
            return;
        
        _Channels.Add(channel);
        Max = _CanUseChannels.Count - 1;
    }


    public void NextChannel()
    {
        var org = CurrentIndex;
        if (CurrentIndex + 1 > Max)
            CurrentIndex = 0;
        else
            CurrentIndex++;
        
        hasNext = org != CurrentIndex;
        CurrentChannel = _Channels[CurrentIndex];
    }

    public void ChangeChannel(ChatChannel channel)
    {
        if (!_Channels.Contains(channel))
            Register(channel);
        
        UpdateCanUseChannels();
        if (!_CanUseChannels.Contains(channel)) return;
        CurrentChannel = channel;
        CurrentIndex = _Channels.IndexOf(channel);
        hasNext = true;
    }

    public void UnRegister(ChatChannel channel)
    {
        _CanUseChannels.Remove(channel);
        _Channels.Remove(channel);
    }
    
    private static HudManager _hudManager => DestroyableSingleton<HudManager>.Instance;
    private static ChatController _chatController => _hudManager.Chat;
    public void UpdateChatBubbles()
    {
        foreach (var channel in _Channels.Where(channel => channel.Bubbles.Any()))
        {
            channel.Bubbles.RemoveAll(n => _chatController.chatBubblePool.inactiveChildren.Contains(n));
            channel.Bubbles.ForEach(n => n.transform.gameObject.SetActive(channel == CurrentChannel));
        }

        AlignAllBubbles();
    }

    public void AddChat(PlayerControl sourcePlayer, string chatText, int channel, bool censor = true) =>
        AddChat(sourcePlayer, chatText, _Channels[channel], censor);
    public void AddChat(PlayerControl sourcePlayer,  string chatText, ChatChannel channel, bool censor = true)
    {
        if (!sourcePlayer || !PlayerControl.LocalPlayer)
        {
            return;
        }
        var data = PlayerControl.LocalPlayer.Data;
        var data2 = sourcePlayer.Data;
        if (data2 == null || data == null || (data2.IsDead && !data.IsDead))
        {
            return;
        }
        var pooledBubble = _chatController.GetPooledBubble();
        try
        {
            pooledBubble.transform.SetParent(_chatController.scroller.Inner);
            pooledBubble.transform.localScale = Vector3.one;
            var flag = sourcePlayer == PlayerControl.LocalPlayer;
            if (flag)
            {
                pooledBubble.SetRight();
            }
            else
            {
                pooledBubble.SetLeft();
            }
            var didVote = MeetingHud.Instance && MeetingHud.Instance.DidVote(sourcePlayer.PlayerId);
            pooledBubble.SetCosmetics(data2);
            _chatController.SetChatBubbleName(pooledBubble, data2, data2.IsDead, didVote, PlayerNameColor.Get(data2));
            if (censor && DataManager.Settings.Multiplayer.CensorChat)
            {
                chatText = BlockedWords.CensorWords(chatText);
            }
            pooledBubble.SetText(chatText);
            pooledBubble.AlignChildren();
            channel.Bubbles.Add(pooledBubble);
            UpdateChatBubbles();
            if (_chatController is { IsOpenOrOpening: false, notificationRoutine: null })
            {
                _chatController.notificationRoutine = _chatController.StartCoroutine(_chatController.BounceDot());
            }

            if (flag || !_CanUseChannels.Contains(channel)) return;
            SoundManager.Instance.PlaySound(_chatController.messageSound, false).pitch = 0.5f + sourcePlayer.PlayerId / 15f;
            _chatController.chatNotification.SetUp(sourcePlayer, chatText);
        }
        catch 
        {
            _chatController.chatBubblePool.Reclaim(pooledBubble);
        }
    }

    public void AlignAllBubbles()
    {
        var num = 0f;
        var bubbles = CurrentChannel.Bubbles;
        if (!bubbles.Any()) return;
        for (var i = bubbles.Count - 1; i >= 0; i--)
        {
            var chatBubble = bubbles[i];
            num += chatBubble.Background.size.y;
            var localPosition = chatBubble.transform.localPosition;
            localPosition.y = -1.85f + num;
            chatBubble.transform.localPosition = localPosition;
            num += 0.15f;
        }
        const float num2 = -0.3f;
        _chatController.scroller.SetYBoundsMin(Mathf.Min(0f, -num + _chatController.scroller.Hitbox.bounds.size.y + num2));
    }


    public void UpdateChatVisible()
    {
        if (!_CanUseChannels.Any())
            return;
        
        _hudManager.Chat.SetVisible(true);
    }
}