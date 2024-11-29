using UnityEngine;

namespace NextChat.Core;

public class ChatChannel(Sprite sprite, Func<PlayerControl, bool>? canUse = null)
{
    public Sprite Icon { get; } = sprite;
    public List<ChatBubble> Bubbles { get; } = [];

    public Func<PlayerControl, bool>? CanUse { get; set; } = canUse;

    public ChatBubble this[Index index] => Bubbles[index];
    public ChatBubble this[int index] => Bubbles[index];
    
    public static implicit operator Sprite(ChatChannel channel) => channel.Icon;
}