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

    public bool IsWasApi => Component is MMDevice;
    
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

    public static implicit operator MMDevice?(VoiceComponent component)
    { 
        if (!component.IsWasApi)
            return null; 
        return (MMDevice)component.Component;
    }
};

public class NextVoiceManager : InstanceClass<NextVoiceManager>
{
    public NextVoiceManager()
    {
        _Instance = this;
        ChangeEndpoint(ConnectionMode.Rpc);
        GetAllComponents();
        ChangeConfig(VoiceConfig.CreateDefault());
        SetDefault();
    }
    
    public List<INextPlayer> Players { get; set; } = [];
    public INetworkEndpoint? Endpoint { get; private set; }
    private List<VoiceComponent> _Components { get; set; } = [];
    public IReadOnlyList<VoiceComponent> Components => _Components.AsReadOnly();
    public VoiceComponent? CurrentMic { get; private set; }
    public VoiceComponent? CurrentSpeaker { get; private set; }
    public WaveTool? _WaveTool { get; private set; }
    public VoiceConfig? _Config { get; private set; }
    public int LastId { get; private set; }
    public MixingSampleProvider? MixingProvider { get; private set; }
    public IWavePlayer? CurrentOutput { get; private set; }

    public void ChangeConfig(VoiceConfig config)
    {
        _Config = config;
        if (LocalPlayer.Instance != null)
            LocalPlayer.Instance.Config = _Config;
        ReInitTool();
    }

    public NextAudioData? GenerateAudioData(byte[] data)
    {
        if (_WaveTool == null || _Config == null || _WaveTool.Encoder == null) return null;
        LastId++;
        return new NextAudioData
        {
            Player = LocalPlayer.Instance!,
            dataId = LastId,
        }.AddFormInfo(new EncodeInfo(_Config.BuffedLength, _WaveTool.Encoder, data));
    }

    public DecodeInfo? GetDecodeInfo(NextAudioData data)
    {
        if (_WaveTool == null || _Config == null || _WaveTool.Decoder == null) return null;
        data.GetDecodeByte(_WaveTool.Decoder, _Config.BuffedLength, out var length, out var dataBytes);
        return new DecodeInfo(length, dataBytes);
    }

    public void ReInitTool()
    {
        if (_Config == null)
        {
            LogWarning("Config is null, not ReInitTool");
            return;
        }

        _WaveTool?.Dispose();
        _WaveTool = WaveTool.BuildFormConfig(_Config, buildWasapiOut:true);
        _WaveTool.WaveIn!.DataAvailable += LocalPlayer.OnDataReceived;
        _WaveTool.WaveIn!.RecordingStopped += (sender, args) =>
        {
            waveInState = false;
            LocalPlayer.Instance!.IsSpeaking = false;
        };
        waveInState = false;
        MixingProvider = _Config.BuildMixingSampleProvider();
        foreach (var player in Players)
        {
            ((DefaultPlayer)player).AddProvider(_Config, MixingProvider);
        }

        CurrentOutput = _WaveTool.WasapiWaveOut;
        SetSpeakerPlay();
    }
    
    public bool State => CurrentOutput?.PlaybackState == PlaybackState.Playing;
    public DefaultPlayer GetPlayer(byte id) => (DefaultPlayer)Players.First(n => n.player.PlayerId == id);
    

    private bool waveInState;

    public void SetSpeakerPlay()
    {
        if (CurrentOutput == null) return;
        CurrentOutput.Init(MixingProvider);
        CurrentOutput.Play();
        
        LogInfo("Set Speaker Play");
    }

    public void UpdateToolState()
    {
        if (CurrentMic == null || CurrentSpeaker == null)
            SetDefault();
        
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
            if (CurrentOutput is WaveOutEvent outEvent)
                outEvent.DeviceNumber = component.Id;
            
            LogInfo($"SetSpeaker {component.Name}");
        }

        if (component.IsMic)
        {
            CurrentMic = component;
            if (_WaveTool?.WaveIn != null)
                _WaveTool.WaveIn.DeviceNumber = component.Id;
            
            LogInfo($"SetMic {component.Name}");
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
            LogInfo($"Add Mic {mic.ProductName} {index}");
            index++;
        }

        index = 0;
        foreach (var speaker in GetAllSpeaker())
        {
            _Components.Add(new VoiceComponent(speaker.ProductName, index, speaker)
            {
                Channels = speaker.Channels
            });
            LogInfo($"Add Speaker {speaker.ProductName} {index}");
            index++;
        }


        index = 0;
        foreach (var device in new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
        {
            _Components.Add(new VoiceComponent(device.DeviceFriendlyName, index, device));
            LogInfo($"Add Mic {device.DeviceFriendlyName} {index}");
            index++;
        }
    }

    public void SetDefault()
    {
        LogInfo("SetDefault Components");
        var mic = Components.FirstOrDefault(n => n.IsMic);
        var speaker = Components.FirstOrDefault(n => n.IsSpeaker);
        
        if (mic != null)
            ChangeComponent(mic);
        
        if (speaker != null)
            ChangeComponent(speaker);
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
        var NewPlayer = player == PlayerControl.LocalPlayer ? new LocalPlayer(player) { Config = _Config } : new DefaultPlayer(player);
        
        if (MixingProvider != null && _Config != null)
            NewPlayer.AddProvider(_Config, MixingProvider);
        
        NewPlayer.Create();
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
            ConnectionMode.Server => new ServerNetworkEndpoint(),
            _ => new RPCNetworkEndpoint()
        };
        
        Endpoint.Start();
    }
}
