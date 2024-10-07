using System.Diagnostics;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Next_Chat.Core;

namespace Next_Chat;

[BepInAutoPlugin("Next.Voice", "Next.Voice", "1.0.0")]
[UsedImplicitly]
public sealed partial class Main : BasePlugin
{
    public static readonly Harmony _Harmony = new(Id);
    public static ManualLogSource LogSource { get; private set; } = null!;
    public static ConfigFile ConfigFile { get; private set; } = null!;
    
    public static IOptionCreator? _OptionCreator { get; set; }

    public Main()
    {
        LogSource = Log;
        ConfigFile = Config;
    }
    
    public override void Load()
    {
        System.Console.OutputEncoding = Encoding.UTF8;
        AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
        Process.GetCurrentProcess().Exited += (sender, args) => Unload();

        foreach (var lib in Libs.ResourceLibs)
            lib.Write();
        
        
        _Harmony.PatchAll();
    }
    

    public override bool Unload()
    {
        AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;
        return base.Unload();
    }

    private static Assembly? AssemblyResolve(object? sender, ResolveEventArgs args)
    {
        var name = new AssemblyName(args.Name);
        return Libs.ResourceLibs.TryGet(n => n.Name == name.Name, out var lib) ? lib.Load() : null;
    }
}