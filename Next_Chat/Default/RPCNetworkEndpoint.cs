using Hazel;
using Next_Chat.Core;
using Next_Chat.Patches;

namespace Next_Chat.Default;

internal class RPCNetworkEndpoint : INetworkEndpoint
{
    public ConnectionMode Mode { get; init; } = ConnectionMode.Rpc;
    public bool Starting = false;

    public void Start()
    {
        if (Starting)
            return;
        RPCPatch.rpcHandlers.Add(RPCFlag.Send, reader =>
        {
            var audio = new NextAudioData();
            audio.RpcRead(reader);
            Receive(audio);
        });
        
        Starting = true;
    }

    public void Send(NextAudioData data)
    {
        RPCFlag.Send.SendRpcToPlayer(SendOption.None, data.RpcWrite, data.Player.player.OwnerId);
    }

    public void Receive(NextAudioData data)
    {
    }

    public void Stop()
    {
        RPCPatch.rpcHandlers.Remove(RPCFlag.Send);
        Starting = false;
    }
}