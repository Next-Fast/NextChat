using NAudio.Wave;
using OpusDotNet;
using WebRtcVadSharp;

namespace Next_Chat.Core;

public class WaveTool : IDisposable
{
    private WaveInEvent? _waveIn;
    private WaveOutEvent? _waveOut;
    private OpusEncoder? _encoder;
    private OpusDecoder? _decoder;
    private WebRtcVad? _vad;
    
    public VoiceConfig? Config { get; set; }
    
    public bool BuildInEvent { get; set; }
    public bool BuildOutEvent { get; set; }
    public bool BuildEncoder { get; set; }
    public bool BuildDecoder { get; set; }
    public bool BuildVad { get; set; }

    public WaveInEvent? WaveIn
    {
        get
        {
            if (!BuildInEvent)
                return null;

            return _waveIn ??= Config?.BuildWaveIn();
        }
    }

    public WaveOutEvent? WaveOut
    {
        get
        {
            if (!BuildOutEvent)
                return null;

            return _waveOut ??= Config?.BuildWaveOut();
        }
    }

    public OpusEncoder? Encoder
    {
        get
        {
            if (!BuildEncoder)
                return null;

            return _encoder ??= Config?.BuildEncoder();
        }
    }

    public OpusDecoder? Decoder
    {
        get
        {
            if (!BuildDecoder)
                return null;

            return _decoder ??= Config?.BuildDecoder();
        }
    }

    public WebRtcVad? Vad
    {
        get
        {
            if (!BuildVad)
                return null;

            return _vad ??= Config?.BuildVad();
        }
    }
    public void Dispose()
    {
        Config = null;
        _waveIn?.Dispose();
        _waveOut?.Dispose();
        _encoder?.Dispose();
        _decoder?.Dispose();
        _vad?.Dispose();
    }

    public static WaveTool BuildFormConfig(
        VoiceConfig config,
        bool buildInEvent = true,
        bool buildOutEvent = true,
        bool buildEncoder = true,
        bool buildDecoder = true,
        bool buildVad = true)
    {
        return new WaveTool
        {
            Config = config,
            BuildInEvent = buildInEvent,
            BuildOutEvent = buildOutEvent,
            BuildEncoder = buildEncoder,
            BuildDecoder = buildDecoder,
            BuildVad = buildVad
        };
    }

    public WaveTool ReBuild(VoiceConfig? config = null, 
        bool buildInEvent = false,
        bool buildOutEvent = false,
        bool buildEncoder = false,
        bool buildDecoder = false,
        bool buildVad = false
        )
    {
        if (config != null)
            Config = config;
        
        BuildInEvent = buildInEvent;
        BuildOutEvent = buildOutEvent;
        BuildEncoder = buildEncoder;
        BuildDecoder = buildDecoder;
        BuildVad = buildVad;
        return this;
    }

    public WaveTool Check()
    {
        if (BuildInEvent && _waveIn == null)
            _waveIn = Config?.BuildWaveIn();

        if (BuildOutEvent && _waveOut == null)
            _waveOut = Config?.BuildWaveOut();

        if (BuildVad && _vad == null)
            _vad = Config?.BuildVad();

        if (BuildEncoder && _encoder == null)
            _encoder = Config?.BuildEncoder();

        if (BuildDecoder && _decoder == null)
            _decoder = Config?.BuildDecoder();

        if (!BuildDecoder && _decoder != null)
        {
            _decoder.Dispose();
            _decoder = null;
        }
        
        if (!BuildEncoder && _encoder != null)
        {
            _encoder.Dispose();
            _encoder = null;
        }

        if (!BuildVad && _vad != null)
        {
            _vad.Dispose();
            _vad = null;
        }

        if (!BuildInEvent && _waveIn != null)
        {
            _waveIn.Dispose(); 
            _waveIn = null;    
        }

        if (!BuildOutEvent && _waveOut != null)
        {
            _waveOut.Dispose();
            _waveOut = null;
        }

        return this;
    }
}