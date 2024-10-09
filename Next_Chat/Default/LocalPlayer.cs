using NAudio.Wave;
using Next_Chat.Core;
using WebRtcVadSharp;

namespace Next_Chat.Default;

public class LocalPlayer(PlayerControl player) : DefaultPlayer(player)
{
    public static bool MicEnabled { get; set; } = true;
    public static bool SpeakerEnabled { get; set; } = true;

    public static LocalPlayer Instance =>
        (LocalPlayer)NextVoiceManager.Instance.Players.FirstOrDefault(n => n.player == PlayerControl.LocalPlayer)!;

    private static WaveTool? Tool => NextVoiceManager.Instance._WaveTool;
    
    
    public override void Dispose()
    {
        if (Tool is { WaveIn: not null })
            Tool.WaveIn.DataAvailable -= OnDataReceived;
        
        base.Dispose();
    }


    public void SetMicState() => SetMicState(!MicEnabled);
    public void SetMicState(bool _state)
    {
        MicEnabled = _state;
        if (_state)
        {
            state |= PlayerStates.Play;
            state &= ~PlayerStates.Mute;
        }
        else
        {
            state &= PlayerStates.Play;
            state |= ~PlayerStates.Mute;
        }
        
        NextVoiceManager.Instance.UpdateToolState();
    }

    public void SetSpeakerState(bool _state)
    {
        SpeakerEnabled = _state;
        NextVoiceManager.Instance.UpdateToolState();
    }

    public static void OnDataReceived(object? sender, WaveInEventArgs e)
    {
        if (Tool == null) return;
        if (!Tool.BuildVad) return;
        if (!Tool.Vad!.HasSpeech(e.Buffer)) return;
        var data = NextVoiceManager.Instance.GenerateAudioData(e.Buffer, e.BytesRecorded);
        NextVoiceManager.Instance.Endpoint?.Send(data);
    }
}