using System;
using System.Collections.Generic;
using System.Linq;
using Next_Chat.Core;
using Next_Chat.Default;

namespace Next_Chat;

public class NextVoiceManager : InstanceClass<NextVoiceManager>
{
    public NextVoiceManager()
    {
        _Instance = this;
    }
    
    
    public List<INextPlayer> Players { get; set; } = [];
    public INetworkEndpoint? Endpoint { get; private set; }

    public INextPlayer CreatePlayer(PlayerControl player)
    {
        Players.FirstOrDefault(n => n.player == player)?.Dispose();
        var NewPlayer = player.AmOwner ? new LocalPlayer(player) : new DefaultPlayer(player);
        Players.Add(NewPlayer);
        return NewPlayer;
    }

    public void ChangeEndpoint(ConnectionMode connectionMode)
    {
        if (Endpoint?.Mode == connectionMode)
            return;
        Endpoint?.Stop();
        Endpoint = connectionMode switch
        {
            ConnectionMode.Rpc => new RPCNetworkEndpoint(),
            _ => throw new NotImplementedException($"Not implemented connection mode{connectionMode}")
        };
        
        Endpoint.Start();
    }
}