using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Next_Chat.Default;

namespace Next_Chat.Core;

public static class Extension
{
    public static DefaultPlayer AddProvider(this DefaultPlayer player, VoiceConfig config, MixingSampleProvider provider)
    {
        player.RemoveProvider(provider);
        player.BufferedProvider = null;
        player.SampleProvider = null;
        player.CreateProvider(config);
        provider.AddMixerInput(player.SampleProvider);
        return player;
    }

    public static DefaultPlayer RemoveProvider(this DefaultPlayer player, MixingSampleProvider provider)
    {
        provider.RemoveMixerInput(player.SampleProvider);
        return player;
    }

    public static DefaultPlayer CreateProvider(this DefaultPlayer player, VoiceConfig config)
    {
        player.BufferedProvider = config.BuildBufferedWaveProvider();
        player.SampleProvider = player.BufferedProvider.GetConverter();
        return player;
    }

    public static SampleProviderConverterBase GetConverter(this IWaveProvider provider)
    {
        var format = provider.WaveFormat;
        if (format.Encoding != WaveFormatEncoding.Pcm) 
            throw new Exception("provider No PCM");
        switch (format.BitsPerSample)
        {
            case 8:
                return new Pcm8BitToSampleProvider(provider);
            case 16:
                return new Pcm16BitToSampleProvider(provider);
            case 24:
                return new Pcm24BitToSampleProvider(provider);
            case 32:
                return new Pcm32BitToSampleProvider(provider);
            default:
                throw new Exception("No if (provider is SampleProviderConverterBase converter)");
        }
    }
    
}