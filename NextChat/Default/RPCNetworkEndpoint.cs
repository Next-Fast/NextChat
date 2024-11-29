using Hazel;
using NextChat.Core;
using NextChat.Patches;

namespace NextChat.Default;

internal class RPCNetworkEndpoint : INetworkEndpoint
{
    public ConnectionMode Mode { get; init; } = ConnectionMode.Rpc;
    public bool Starting;

    public void Start()
    {
        if (Starting)
            return;
        RPCPatch.rpcHandlers.Add(RPCFlag.Send, reader =>
        {
            var audio = new NextAudioData();
            audio.RpcRead(reader);
            LogInfo($"receive AudioDar Id:{audio.dataId} Length:{audio.Length} sender:{audio.Player.player.name}");
            audio.Player.AddData(audio);
        });
        
        Starting = true;
    }

    public void Send(NextAudioData data)
    {
        if (!Starting) return; 
        RPCFlag.Send.SendRpcToAll(SendOption.None, data.RpcWrite);
        LogInfo($"send AudioDar Id:{data.dataId} Length:{data.Length}");
    }


    public void Stop()
    {
        RPCPatch.rpcHandlers.Remove(RPCFlag.Send);
        Starting = false;
    }
}