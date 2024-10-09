using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Next_Chat.Core;

namespace Next_Chat.Default;

public class DefaultPlayer(PlayerControl player) : INextPlayer
{
    public PlayerControl player { get; set; } = player;
    public PlayerStates state { get; set; } = PlayerStates.Play;
    
    public bool IsSpeaking { get; set; }

    private readonly List<NextAudioData> AllKeepData = [];
    internal readonly List<NextAudioData> _allData = [];
    public int LastDataId { get; set; }
    public BufferedWaveProvider? BufferedProvider { get; set; }
    public Wave16ToFloatProvider? _FloatProvider { get; set; }
    public SampleProviderConverterBase? SampleProvider { get; set; }
    
    public VoiceConfig? Config { get; set; }
    

    public virtual void OnUpdate()
    {
        if (AllKeepData.Count != 0 && AllKeepData[0].dataId == LastDataId + 1)
            PushData(AllKeepData[0]);
        
        if (_allData.Count == 0) return;
        
        if (state.HasFlag(PlayerStates.Ban))
        {
            _allData[0].Dispose();
            _allData.RemoveAt(0);
            IsSpeaking = false;
            return;
        }
        
        var data = _allData[0];
        AddDataToProvider(data);
        IsSpeaking = true;
    }

    public virtual void AddDataToProvider(NextAudioData data)
    {
        if (BufferedProvider?.BufferedBytes == 0)
            BufferedProvider?.AddSamples(new byte[1024], 0, 1024);
        
        BufferedProvider?.AddSamples(data.DecodeBytes, 0, data.DecodeLength);
        data.Dispose();
    }

    public void AddData(NextAudioData data)
    {
        if (_allData.Exists(n => n.dataId == data.dataId)) return;

        if (data.dataId > LastDataId + 1 && data.dataId < LastDataId + 10)
            KeepData(data);
        else
            PushData(data);
    }

    public void PushData(NextAudioData data)
    {
        AllKeepData.RemoveAll(n => n.dataId == data.dataId);
        _allData.Add(data);
        LastDataId = data.dataId;
    }

    public void KeepData(NextAudioData data)
    {
        AllKeepData.Add(data);
        AllKeepData.Sort((x, y) => x.dataId.CompareTo(y.dataId));
    }

    public virtual void Dispose()
    {
        NextVoiceManager.Instance.Players.Remove(this);
        if (NextVoiceManager.Instance.MixingProvider != null)
            this.RemoveProvider(NextVoiceManager.Instance.MixingProvider);
    }

    public void Ban() => state |= PlayerStates.Ban;
    public void UnBan() => state &= ~PlayerStates.Ban;
    
    public void Silence() => state |= PlayerStates.Silence;
    public void UnSilence() => state &= ~PlayerStates.Silence;
}