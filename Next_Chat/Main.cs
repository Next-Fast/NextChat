using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Next_Chat.Core;
using Next_Chat.Default;
using Next_Chat.Patches;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

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
        if (ConsoleManager.ConsoleActive)
            System.Console.OutputEncoding = Encoding.UTF8;
        
        AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
        Process.GetCurrentProcess().Exited += (sender, args) => Unload();
        
        SetModStamp();
        SetWrite();
        RegisterIL2cpp();
        SetKeyBind();

        _Harmony.PatchAll();
    }

    private void RegisterIL2cpp()
    {
        ClassInjector.RegisterTypeInIl2Cpp<InputKeyBindUpdate>();
        ClassInjector.RegisterTypeInIl2Cpp<ZOrderedSortingGroup>();
        ClassInjector.RegisterTypeInIl2Cpp<PlayerIconInstance>();
        ClassInjector.RegisterTypeInIl2Cpp<VCFrame>();
    }

    private static void SetModStamp()
    {
        SceneManager.add_sceneLoaded((UnityAction<Scene, LoadSceneMode>)(Action<Scene, LoadSceneMode>)((scene, mode) => 
        { 
            if (!ModManager.InstanceExists) return;
                ModManager.Instance.ShowModStamp();
        }));
    }

    private void SetKeyBind()
    {
        Extension.AddComponent<InputKeyBindUpdate>(this);
        InputKeyBindUpdate.Register("VoiceMute", () =>
        {
            LocalPlayer.Instance?.SetMicState();
        }, KeyCode.M);
    }
    
    private static void SetWrite()
    {
        LibDir = Path.Combine(Paths.GameRootPath, "Library");
        if (!Directory.Exists(LibDir))
            Directory.CreateDirectory(LibDir);

        foreach (var lib in Libs.ResourceLibs)
        {
            lib.Write(LibDir);
        }
        
        NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), Resolver);
        return;

        IntPtr Resolver(string library, Assembly assembly, DllImportSearchPath? search)
        {
            var path = Path.Combine(LibDir, $"{library}.dll");
            LogInfo($"Resolver: {library} {assembly.GetName()} " + path);
            if (File.Exists(path) && NativeLibrary.TryLoad(path, out var handle))
                return handle;
            
            return IntPtr.Zero;
        }
    }
    

    public override bool Unload()
    {
        AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;
        return base.Unload();
    }

    public static string LibDir = Paths.PluginPath;
    private static Assembly? AssemblyResolve(object? sender, ResolveEventArgs args)
    {
        var name = new AssemblyName(args.Name);
        var path = Path.Combine(LibDir, $"{name.Name}.dll");
        return File.Exists(path) ? Assembly.LoadFile(path) : null;
    }
}