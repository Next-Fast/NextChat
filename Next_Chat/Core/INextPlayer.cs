using System;

namespace Next_Chat.Core;

public interface INextPlayer : IDisposable
{
    public PlayerControl player { get; set; }
    public PlayerStates state { get; set; }
}