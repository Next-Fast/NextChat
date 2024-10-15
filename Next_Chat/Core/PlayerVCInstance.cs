namespace Next_Chat.Core;

public abstract class PlayerVCInstance
{
    public PlayerIconInstance? Icon
    {
        get; set;
    }

    protected abstract INextPlayer _player { get;  }

    public VCFrame? Frame
    {
        get;
        set;
    }

    public void Create()
    {
        Icon = PlayersOverlay.Instance?.GetOrCreate(_player);
    }
}