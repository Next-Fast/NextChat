using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NAudio;
using NAudio.Wave;
using Next_Chat.Core;
using Next_Chat.Default;
using OpusDotNet;

namespace Next_Chat;

public class NextVoiceManager : InstanceClass<NextVoiceManager>
{
    public NextVoiceManager()
    {
        _Instance = this;
    }
    
    public List<INextPlayer> Players { get; set; } = [];
    public INetworkEndpoint? Endpoint { get; private set; }
    public OpusEncoder? Encoder { get; set; }

    public IEnumerable<WaveInCapabilities> GetAllMic()
    {
        for (var i = 0; i < WaveInEvent.DeviceCount; i++)
        {
            yield return WaveInEvent.GetCapabilities(i);
        }
    }
    
    public IEnumerable<WaveOutCapabilities> GetAllSpeaker()
    {
        for (var i = 0; i < WaveInterop.waveOutGetNumDevs(); i++)
        {
            yield return GetCapabilities(i);
        }

        yield break;
        WaveOutCapabilities GetCapabilities(int i)
        {
            var caps = new WaveOutCapabilities();
            var structSize = Marshal.SizeOf(caps);
            MmException.Try(WaveInterop.waveOutGetDevCaps((IntPtr)i, out caps, structSize), "WaveOutGetDevCaps");
            return caps;
        }
    }

    public INextPlayer CreatePlayer(PlayerControl player)
    {
        Players.FirstOrDefault(n => n.player == player)?.Dispose();
        var NewPlayer = player.AmOwner ? 
            new LocalPlayer(player) : 
            new DefaultPlayer(player);
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