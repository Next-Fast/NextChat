using System.Reflection;
using BepInEx;

namespace Next_Chat.Core;

public class ResourceLib(string name, bool WriteDir = false)
{
    private static readonly Assembly assembly = Assembly.GetExecutingAssembly();
    private static readonly string ResourcePath = $"{ResourceInfo.AssemblyName}.Resource.";
    public readonly string Name = GetName(name);
    public readonly bool WriteDir = WriteDir;

    public static string GetName(string path)
    {
        var paths = path.Split('.').ToList();
        paths.RemoveAt(0);
        return string.Join('.', paths);
    }
    public string GetPath()
    {
        if (assembly.GetManifestResourceNames().Contains(ResourcePath + name)) 
            return ResourcePath + name;

        return name;
    }

    public void Write()
    {
        if (!WriteDir) return;
        LogInfo($"Write Path Form Resources: {GetPath()} : {ResourcePath + name}");
        var path = Path.Combine(Paths.PluginPath, Name);
        if (File.Exists(path) && new FileInfo(path).Length != 0) return;
        using var _Stream = assembly.GetManifestResourceStream(GetPath());
        using var file = File.OpenWrite(path);
        _Stream?.CopyTo(file);
    }

    public Assembly? Load()
    {
        LogInfo($"Load Path Form Resources: {GetPath()} : {ResourcePath + name}");
        using var _Stream = assembly.GetManifestResourceStream(GetPath());
        
        if (_Stream != null) 
            return Assembly.Load(_Stream.ReadFully());
        
        LogWarning("Resource not found: " + GetPath());
        return null;
    }
}