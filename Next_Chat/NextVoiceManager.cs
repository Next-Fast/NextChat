using System.Runtime.InteropServices;
using NAudio;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Next_Chat.Core;
using Next_Chat.Default;
using Next_Chat.Patches;
using OpusDotNet;
using UnityEngine;

namespace Next_Chat;

public record VoiceComponent(string Name, int Id, object Component)
{
    public int Channels { get; set; }
    
    public bool IsMic => Component is WaveInCapabilities;
    public bool IsSpeaker => Component is WaveOutCapabilities;
    
    public static implicit operator WaveInCapabilities?(VoiceComponent component)
    {
        if (!component.IsMic)
            return null;
        return (WaveInCapabilities)component.Component;
    }
    
    public static implicit operator WaveOutCapabilities?(VoiceComponent component)
    {
        if (!component.IsSpeaker)
            return null;
        return (WaveOutCapabilities)component.Component;
    }
};

public class NextVoiceManager : InstanceClass<NextVoiceManager>
{
    public NextVoiceManager()
    {
        _Instance = this;
        InputKeyBindUpdate.Register("VoiceMute", LocalPlayer.Instance.SetMicState, KeyCode.M);
        ChangeEndpoint(ConnectionMode.Rpc);
        SetSpeakerPlay();
        ChangeConfig(VoiceConfig.CreateDefault());
    }
    
    public List<INextPlayer> Players { get; set; } = [];
    public INetworkEndpoint? Endpoint { get; private set; }
    private List<VoiceComponent> _Components { get; set; } = [];
    public IReadOnlyList<VoiceComponent> Components => _Components.AsReadOnly();
    public VoiceComponent? CurrentMic { get; private set; }
    public VoiceComponent? CurrentSpeaker { get; private set; }
    public float Volume { get; private set; }
    public WaveTool? _WaveTool { get; private set; }
    public VoiceConfig? _Config { get; private set; }
    public int LastId { get; private set; }
    public MixingSampleProvider? MixingProvider { get; private set; }
    

    public void ChangeConfig(VoiceConfig config)
    {
        _Config = config;
        ReInitTool();
    }

    public NextAudioData GenerateAudioData(byte[] data, int byteNumber)
    {
        LastId++;
        return new NextAudioData
        {
            DataBytes = data,
            Player = LocalPlayer.Instance,
            dataId = LastId,
            pcmLength = byteNumber
        };
    }

    public void applyData(NextAudioData data)
    {
        data.Player.AddData(data);
        data.Player.IsSpeaking = true;
    }

    public void ReInitTool()
    {
        if (_Config == null)
        {
            LogWarning("Config is null, not ReInitTool");
            return;
        }

        _WaveTool?.Dispose();
        _WaveTool = WaveTool.BuildFormConfig(_Config);
        _WaveTool.WaveIn!.DataAvailable += LocalPlayer.OnDataReceived;
        _WaveTool.WaveIn!.RecordingStopped += (sender, args) => waveInState = false;
        waveInState = false;
        MixingProvider = _Config.BuildMixingSampleProvider();
        foreach (var player in Players)
        {
            ((DefaultPlayer)player).AddProvider(_Config, MixingProvider);
        }
    }
    
    public bool WaveOutState => _WaveTool?.WaveOut?.PlaybackState == PlaybackState.Playing;
    public INextPlayer GetPlayer(byte id) => Players.First(n => n.player.PlayerId == id);

    public void SetVolume(float volume)
    {
        Volume = volume;
        if (_WaveTool?.WaveOut != null)
            _WaveTool.WaveOut.Volume = volume;
    }

    private bool waveInState = false;

    public void SetSpeakerPlay()
    {
        if (!_WaveTool?.BuildOutEvent ?? false)
            ReInitTool();

        var WaveOut = _WaveTool?.WaveOut;
        if (WaveOut == null) return;
        WaveOut.Init(MixingProvider, _Config!.convertTo16Bit);
        WaveOut.Play();
    }

    public void UpdateToolState()
    {
        if (_WaveTool == null)
            ReInitTool();

        if (!_WaveTool!.BuildInEvent)
            return;
        
        
        switch (LocalPlayer.MicEnabled)
        {
            case true when !waveInState:
                _WaveTool?.WaveIn?.StartRecording();
                waveInState = true;
                break;
            case false when waveInState:
                _WaveTool?.WaveIn?.StopRecording();
                waveInState = false;
                break;
        }
    }

    public void ChangeComponent(VoiceComponent component)
    {
        if (component.IsSpeaker)
        {
            CurrentSpeaker = component;
            if (_WaveTool?.WaveOut != null)
                _WaveTool.WaveOut.DeviceNumber = component.Id;
        }

        if (component.IsMic)
        {
            CurrentMic = component;
            if (_WaveTool?.WaveIn != null)
                _WaveTool.WaveIn.DeviceNumber = component.Id;
        }

        if (component is { IsMic: false, IsSpeaker: false })
        {
            LogWarning($"Unknown component name:{component.Name} Id:{component.Id} Type:{component.Component.GetType().Name}");
        }
    }

    public void GetAllComponents()
    {
        _Components.Clear();
        
        var index = 0;
        foreach (var mic in GetAllMic())
        {
            _Components.Add(new VoiceComponent(mic.ProductName, index, mic)
            {
                Channels = mic.Channels
            });
            index++;
        }

        index = 0;
        foreach (var speaker in GetAllSpeaker())
        {
            _Components.Add(new VoiceComponent(speaker.ProductName, index, speaker)
            {
                Channels = speaker.Channels
            });
            index++;
        }
    }

    public void SetDefault()
    {
        CurrentMic ??= Components.FirstOrDefault(n => n.IsMic);
        CurrentSpeaker ??= Components.FirstOrDefault(n => n.IsSpeaker);
    }
    
    
    public static IEnumerable<WaveInCapabilities> GetAllMic()
    {
        for (var i = 0; i < WaveInEvent.DeviceCount; i++)
        {
            yield return WaveInEvent.GetCapabilities(i);
        }
    }
    
    public static IEnumerable<WaveOutCapabilities> GetAllSpeaker()
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
        var NewPlayer = player.AmOwner ? new LocalPlayer(player) : new DefaultPlayer(player);
        
        if (MixingProvider != null && _Config != null)
            NewPlayer.AddProvider(_Config, MixingProvider);
        
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
            _ => throw new Exception($"Not implemented connection mode{connectionMode}")
        };
        
        Endpoint.Start();
    }
}
