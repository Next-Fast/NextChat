using NAudio.Wave;
using Next_Chat.Core;
using WebRtcVadSharp;

namespace Next_Chat.Default;

public class LocalPlayer(PlayerControl player) : DefaultPlayer(player)
{
    public WaveInEvent waveEvent { get; } = new();
    public WebRtcVad Vad { get; } = new();
    public static bool MicEnabled { get; set; } = true;

    public static LocalPlayer? Instance =>
        (LocalPlayer)NextVoiceManager.Instance.Players.FirstOrDefault(n => n.player == PlayerControl.LocalPlayer)!;

    public void Update()
    {
    }
    
    public override void Dispose()
    {
        
        base.Dispose();
    }
    
    public void SetMicState(bool _state)
    {
        MicEnabled = _state;
        state = _state ? PlayerStates.Play : PlayerStates.Mute;
    }
}