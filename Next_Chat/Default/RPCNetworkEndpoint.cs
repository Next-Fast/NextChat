using Next_Chat.Core;

namespace Next_Chat.Default;

internal class RPCNetworkEndpoint : INetworkEndpoint
{
    public ConnectionMode Mode { get; init; } = ConnectionMode.Rpc;

    public void Start()
    {
    }

    public void Send()
    {
    }

    public void Receive()
    {
    }

    public void Stop()
    {
    }
    
}