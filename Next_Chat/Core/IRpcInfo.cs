using Hazel;

namespace Next_Chat.Core;

public interface IRpcInfo
{
    public void RpcWrite(MessageWriter writer);
    public void RpcRead(MessageReader reader);
}