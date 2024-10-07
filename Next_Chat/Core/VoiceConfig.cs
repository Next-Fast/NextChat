using Hazel;
using NAudio.Wave;
using OpusDotNet;
using WebRtcVadSharp;

namespace Next_Chat.Core;

public class VoiceConfig : IRpcInfo
{
    public int SampleRateInt { get; set; }
    public long FrameLengthLong { get; set; }
    public int Channels { get; set; }

    
    public OperatingMode OperatingMode { get; set; }

    public static implicit operator OperatingMode(VoiceConfig config) => config.OperatingMode;
    public static implicit operator FrameLength(VoiceConfig config) => config.GetFrameLengthType();
    public static implicit operator SampleRate(VoiceConfig config) => config.GetSampleRateType();

    public static VoiceConfig CreateDefault()
    {
        return new VoiceConfig();
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
        return new WaveInEvent();
    }

    public WaveOutEvent BuildWaveOut()
    {
        return new WaveOutEvent();
    }

    public FrameLength GetFrameLengthType()
    {
        while (true)
        {
            switch (FrameLengthLong)
            {
                case 10:
                    return FrameLength.Is10ms;
                case 20:
                    return FrameLength.Is20ms;
                case 30:
                    return FrameLength.Is30ms;
                default:
                    LogWarning("No Supped ReSet FrameLength");
                    SetFrameLength(FrameLengthLong);
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
    
    public void SetFrameLength(long frameLength)
    {
        List<long> rates = [10, 20 , 30];
        FrameLengthLong = rates.MinBy(n => Math.Abs(n - frameLength)); ;
    }

    public void SetRate(int rate)
    {
        List<int> rates = [8000, 16000, 32000, 48000];
        SampleRateInt = rates.MinBy(n => Math.Abs(n - rate));
    }

    public void RpcWrite(MessageWriter writer)
    {
    }

    public void RpcRead(MessageReader reader)
    {
    }
}