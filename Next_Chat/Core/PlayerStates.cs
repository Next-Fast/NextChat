using System;

namespace Next_Chat.Core;

[Flags]
public enum PlayerStates
{
    Mute,
    Play,
    Silence,
    Ban
}