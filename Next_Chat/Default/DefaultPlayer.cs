using Next_Chat.Core;

namespace Next_Chat.Default;

public class DefaultPlayer(PlayerControl player) : INextPlayer
{
    public PlayerControl player { get; set; } = player;
    public PlayerStates state { get; set; } = PlayerStates.Play;
    
    public bool IsSpeaking { get; set; } = false;

    public virtual void Dispose()
    {
        NextVoiceManager.Instance.Players.Remove(this);
    }
}