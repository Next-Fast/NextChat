using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NextChat.Default;
using UnityEngine;

namespace NextChat.Core;

public static class Extension
{
    public static void AddProvider(this DefaultPlayer player, VoiceConfig config, MixingSampleProvider provider)
    {
        if (player.VolumeProvider != null)
            provider.RemoveMixerInput(player.VolumeProvider);
        
        player.BufferedProvider = null;
        player._FloatProvider = null;
        player.SampleProvider = null;
        player.VolumeProvider = null;
        player.CreateProvider(config);
        provider.AddMixerInput(player.VolumeProvider);
    }
    

    public static DefaultPlayer CreateProvider(this DefaultPlayer player, VoiceConfig config)
    {
        player.BufferedProvider = config.BuildBufferedWaveProvider();
        player._FloatProvider = config.Build16ToFloatProvider(player.BufferedProvider);
        player.SampleProvider = player._FloatProvider.GetConverter();
        player.VolumeProvider = new VolumeSampleProvider(player.SampleProvider);
        return player;
    }

    public static SampleProviderConverterBase GetConverter(this IWaveProvider provider)
    {
        var format = provider.WaveFormat;
        if (format.Encoding == WaveFormatEncoding.Pcm)
        {
            return format.BitsPerSample switch
            {
                8 => new Pcm8BitToSampleProvider(provider),
                16 => new Pcm16BitToSampleProvider(provider),
                24 => new Pcm24BitToSampleProvider(provider),
                32 => new Pcm32BitToSampleProvider(provider),
                _ => throw new Exception("No PCM bit SampleProviderConverterBase converter")
            };
        }

        if (format.Encoding != WaveFormatEncoding.IeeeFloat) 
            throw new Exception("provider No PCM and IEEE Float");
        
        if (format.BitsPerSample == 64)
            return new WaveToSampleProvider64(provider);
            
        return new WaveToSampleProvider(provider);

    }

    public static T AddComponent<T>(this BasePlugin plugin) where T : MonoBehaviour
    {
        if (!ClassInjector.IsTypeRegisteredInIl2Cpp(typeof(T))) 
            ClassInjector.RegisterTypeInIl2Cpp<T>();
        return plugin.AddComponent<T>().Dont();
    }
    
}