using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using NAudio.Wave;

namespace Next_Chat;

[BepInAutoPlugin("Next.Voice", "Next.Voice", "1.0.0")]
public sealed partial class Main : BasePlugin
{
    public static readonly Harmony _Harmony = new(Id);
    public static ManualLogSource LogSource { get; private set; } = null!;
    public static ConfigFile ConfigFile { get; private set; } = null!;

    public Main()
    {
        LogSource = Log;
        ConfigFile = Config;
    }
    
    public override void Load()
    {
        var @event = new WaveInEvent();
        @event.StartRecording();
        @event.DataAvailable += (sender, args) =>
        {
            
        };
        _Harmony.PatchAll();
    }
}