using Hazel;

namespace NextChat.Core;

public interface IRpcInfo
{
    public void RpcWrite(MessageWriter writer);
    public void RpcRead(MessageReader reader);
}