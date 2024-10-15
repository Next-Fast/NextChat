using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Next_Chat.Core;
using Next_Chat.Patches;

namespace Next_Chat.Default;

public class DefaultPlayer(PlayerControl player) : PlayerVCInstance, INextPlayer
{
    public PlayerControl player { get; set; } = player;
    public PlayerStates state { get; set; } = PlayerStates.Play;

    public bool IsSpeaking { get; set; }
    public bool HasInstall { get; set; }

    private readonly List<NextAudioData> AllKeepData = [];
    internal readonly List<NextAudioData> _allData = [];
    public int LastDataId { get; set; }
    public BufferedWaveProvider? BufferedProvider { get; set; }
    public Wave16ToFloatProvider? _FloatProvider { get; set; }
    public SampleProviderConverterBase? SampleProvider { get; set; }
    public VolumeSampleProvider? VolumeProvider { get; set; }

    public VoiceConfig? Config { get; set; }
    
    public float Volume { get; private set; }

    public void AddLocalData(byte[] bytes, int length)
    {
        BufferedProvider?.AddSamples(bytes, 0, length);
        IsSpeaking = true;
        LogInfo($"BufferedProvider AddSamples {length}");
    }

    
    public virtual void OnUpdate()
    {
        if (NextVoiceManager.Instance._WaveTool == null || NextVoiceManager.Instance._WaveTool.Decoder == null) return;
        
        if (AllKeepData.Count != 0 && AllKeepData[0].dataId == LastDataId + 1)
            PushData(AllKeepData[0]);
        
        if (_allData.Count == 0)
        {
            IsSpeaking = BufferedProvider?.BufferedBytes / 2 != 0;
            return;
        }
        
        if (state.HasFlag(PlayerStates.Ban))
        {
            _allData[0].Dispose();
            _allData.RemoveAt(0);
            IsSpeaking = false;
            return;
        }
        
        var data = _allData[0];
        AddDataToProvider(data);
        data.Dispose();
        _allData.RemoveAt(0);
        IsSpeaking = BufferedProvider?.BufferedBytes / 2 != 0;
    }

    public virtual void AddDataToProvider(NextAudioData data)
    {
        var info = NextVoiceManager.Instance.GetDecodeInfo(data);
        if (info == null) return;
        BufferedProvider?.AddSamples(info.Data, 0, info.buffedLength);
        LogInfo($"AddData dataId:{data.dataId} length:{info.buffedLength}");
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
        LogInfo($"push data {data.dataId}");
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
            NextVoiceManager.Instance.MixingProvider.RemoveMixerInput(VolumeProvider);
    }

    public void Ban() => state |= PlayerStates.Ban;
    public void UnBan() => state &= ~PlayerStates.Ban;
    
    public void Silence() => state |= PlayerStates.Silence;
    public void UnSilence() => state &= ~PlayerStates.Silence;

    static DefaultPlayer()
    {
        RPCPatch.rpcHandlers.Add(RPCFlag.HasInstall, reader =>
        {
            var player = NextVoiceManager.Instance.GetPlayer(reader.ReadByte());
            player.HasInstall = true;
        });
    }

    protected override INextPlayer _player => this;
}