using NAudio.Wave;

namespace Next_Chat.Default;

public class LocalPlayer(PlayerControl player) : DefaultPlayer(player)
{
    public WaveInEvent waveEvent { get; } = new();
    
    public override void Dispose()
    {
        
        base.Dispose();
    }
}