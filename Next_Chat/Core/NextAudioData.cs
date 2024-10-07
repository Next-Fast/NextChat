using Hazel;

namespace Next_Chat.Core;

public class NextAudioData : IDisposable, IRpcInfo
{
    public INextPlayer Player { get; set; } = null!;


    public void Dispose()
    {
    }

    public void RpcWrite(MessageWriter writer)
    {
    }

    public void RpcRead(MessageReader reader)
    {
    }
}