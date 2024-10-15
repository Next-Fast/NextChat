using NAudio.Wave;
using Next_Chat.Core;
using Next_Chat.Patches;
using WebRtcVadSharp;

namespace Next_Chat.Default;

public class LocalPlayer(PlayerControl player) : DefaultPlayer(player)
{
    public static bool MicEnabled { get; set; } = true;
    public static bool SpeakerEnabled { get; set; } = true;

    public static LocalPlayer? Instance {
        get
        {
            if (PlayerControl.LocalPlayer == null) return null;
            
            var player = NextVoiceManager.Instance.Players.FirstOrDefault(n => n.player == PlayerControl.LocalPlayer);
            if (player != null)
                return player as LocalPlayer;
            
            return NextVoiceManager.Instance.CreatePlayer(PlayerControl.LocalPlayer) as LocalPlayer;
        }
    }

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
        
        LogInfo("mic: " + MicEnabled);
        VoicePatch.UpdateSprite();
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
        if (NextVoiceManager.Instance.Players.Count <= 1)
        {
            Instance?.AddLocalData(e.Buffer, e.BytesRecorded);
            LogInfo($"Add LocalDataLength {e.BytesRecorded}");
        }
        else
        {
            var data = NextVoiceManager.Instance.GenerateAudioData(e.Buffer);
            if (data == null) return;
            LogInfo($"GenerateAudioData: {data.dataId} : {e.BytesRecorded} : {data.Length}");
            NextVoiceManager.Instance.Endpoint?.Send(data);
        }
    }
}