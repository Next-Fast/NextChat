using System.Reflection;
using BepInEx;

namespace Next_Chat.Core;

public class ResourceLib(string name)
{
    private static readonly Assembly assembly = Assembly.GetExecutingAssembly();
    private static readonly string ResourcePath = $"{ResourceInfo.AssemblyName}.Resource.";
    public readonly string Name = GetName(name);

    public static string GetName(string path)
    {
        path = path[(path.IndexOf('.') + 1)..];
        path = path[..path.LastIndexOf('.')];
        return path;
    }
    public string GetPath()
    {
        if (assembly.GetManifestResourceNames().Contains(ResourcePath + name)) 
            return ResourcePath + name;

        return name;
    }

    public void Write(string dir)
    {
        LogInfo($"Write Path Form Resources: {GetPath()} : {ResourcePath + name}");
        var path = Path.Combine(dir, Name + ".dll" );
        if (File.Exists(path) && new FileInfo(path).Length != 0) return;
        using var _Stream = assembly.GetManifestResourceStream(GetPath());
        File.WriteAllBytes(path, _Stream.ReadFully());
    }
    
}