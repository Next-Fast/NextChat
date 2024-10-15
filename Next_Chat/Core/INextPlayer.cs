using System;

namespace Next_Chat.Core;

public interface INextPlayer : IDisposable
{
    public PlayerControl player { get; set; }
    public PlayerStates state { get; set; }
    
    public bool IsSpeaking { get; set; }
    
    public bool HasInstall { get; set; }

    public void AddData(NextAudioData data);

    public void OnUpdate();
}