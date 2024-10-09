using Hazel;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Next_Chat.Patches;
using OpusDotNet;
using WebRtcVadSharp;

namespace Next_Chat.Core;

public class VoiceConfig : IRpcInfo
{
    public int SampleRateInt { get; set; }
    public int FrameLengthInt { get; set; }
    public int Channels { get; set; }
    public int Bits { get; set; }
    public int BufferOfNumber { get; set; }
    public int DesiredLatency { get; set; }
    public OperatingMode OperatingMode { get; set; }
    
    public bool ReadFully { get; set; }
    
    public bool convertTo16Bit { get; set; }

    public static implicit operator OperatingMode(VoiceConfig config) => config.OperatingMode;
    public static implicit operator FrameLength(VoiceConfig config) => config.GetFrameLengthType();
    public static implicit operator SampleRate(VoiceConfig config) => config.GetSampleRateType();

    static VoiceConfig()
    {
        RPCPatch.rpcHandlers.Add(RPCFlag.SyncConfig, reader =>
        {
            if (AmongUsClient.Instance.AmHost) return;
            var config = new VoiceConfig();
            config.RpcRead(reader);
            NextVoiceManager.Instance.ChangeConfig(config);
        });
    }
    
    public static VoiceConfig CreateDefault()
    {
        return new VoiceConfig
        {
            SampleRateInt = 48000,
            FrameLengthInt = 30,
            Channels = 1,
            Bits = 16,
            BufferOfNumber = 3,
            DesiredLatency = 300,
            OperatingMode = OperatingMode.HighQuality,
            ReadFully = true
        };
    }

    public MixingSampleProvider BuildMixingSampleProvider()
    {
        return new MixingSampleProvider(BuildWaveFormat())
        {
            ReadFully = ReadFully
        };
    }

    public BufferedWaveProvider BuildBufferedWaveProvider()
    {
        return new BufferedWaveProvider(BuildWaveFormat())
        {
            ReadFully = ReadFully
        };
    }
    
    public WaveFormat BuildWaveFormat()
    {
        return new WaveFormat(SampleRateInt, Bits, Channels);
    }

    public OpusEncoder BuildEncoder()
    {
        return new OpusEncoder(Application.VoIP);
    }

    public OpusDecoder BuildDecoder()
    {
        return new OpusDecoder(SampleRateInt, Channels);
    }

    public WebRtcVad BuildVad()
    {
        return new WebRtcVad
        {
            FrameLength = this,
            SampleRate = this,
            OperatingMode = this
        };
    }

    public WaveInEvent BuildWaveIn()
    {
        return new WaveInEvent
        {
            WaveFormat = new WaveFormat(SampleRateInt, Bits, Channels),
            BufferMilliseconds = FrameLengthInt,
            DeviceNumber = NextVoiceManager.Instance.CurrentMic?.Id ?? -1,
            NumberOfBuffers = BufferOfNumber
        };
    }

    public WaveOutEvent BuildWaveOut()
    {
        return new WaveOutEvent
        {
            DeviceNumber = NextVoiceManager.Instance.CurrentSpeaker?.Id ?? -1,
            NumberOfBuffers = BufferOfNumber,
            DesiredLatency = DesiredLatency,
            Volume = NextVoiceManager.Instance.Volume
        };
    }

    public FrameLength GetFrameLengthType()
    {
        while (true)
        {
            switch (FrameLengthInt)
            {
                case 10:
                    return FrameLength.Is10ms;
                case 20:
                    return FrameLength.Is20ms;
                case 30:
                    return FrameLength.Is30ms;
                default:
                    LogWarning("No Supped ReSet FrameLength");
                    SetFrameLength(FrameLengthInt);
                    continue;
            }
        }
    }

    public SampleRate GetSampleRateType()
    {
        while (true)
        {
            switch (SampleRateInt)
            {
                case 8000:
                    return SampleRate.Is8kHz;
                case 16000:
                    return SampleRate.Is16kHz;
                case 32000:
                    return SampleRate.Is32kHz;
                case 48000:
                    return SampleRate.Is48kHz;
                default:
                    LogWarning("No Supped ReSet SampleRate");
                    SetRate(SampleRateInt);
                    continue;
            }
        }
    }
    
    public void SetFrameLength(int frameLength)
    {
        List<int> rates = [10, 20 , 30];
        FrameLengthInt = rates.MinBy(n => Math.Abs(n - frameLength)); ;
    }

    public void SetRate(int rate)
    {
        List<int> rates = [8000, 16000, 32000, 48000];
        SampleRateInt = rates.MinBy(n => Math.Abs(n - rate));
    }

    public void RpcWrite(MessageWriter writer)
    {
        writer.Write(SampleRateInt);
        writer.Write(FrameLengthInt);
        writer.Write(Channels);
        writer.Write(Bits);
        writer.Write(BufferOfNumber);
        writer.Write(DesiredLatency);
        writer.Write((byte)OperatingMode);
        writer.Write(ReadFully);
        writer.Write(convertTo16Bit);
    }

    public void RpcRead(MessageReader reader)
    {
        SampleRateInt = reader.ReadInt32();
        FrameLengthInt = reader.ReadInt32();
        Channels = reader.ReadInt32();
        Bits = reader.ReadInt32();
        BufferOfNumber = reader.ReadInt32();
        DesiredLatency = reader.ReadInt32();
        OperatingMode = (OperatingMode)reader.ReadByte();
        ReadFully = reader.ReadBoolean();
        convertTo16Bit = reader.ReadBoolean();
    }
}