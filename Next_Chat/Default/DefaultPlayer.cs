using Next_Chat.Core;

namespace Next_Chat.Default;

public class DefaultPlayer(PlayerControl player) : INextPlayer
{
    public PlayerControl player { get; set; } = player;
    public PlayerStates state { get; set; }

    public virtual void Dispose()
    {
        NextVoiceManager.Instance.Players.Remove(this);
    }
}