using System.Reflection;

namespace Next_Chat.Core;

public class ResourceLib(string name)
{
    private static readonly Assembly assembly = Assembly.GetExecutingAssembly();
    private static readonly string ResourcePath = $"{assembly.GetName().Name}.Resources.";
    public string Name = name.Split(".")[^2];

    public string GetPath()
    {
        if (assembly.GetManifestResourceNames().Contains(ResourcePath + name)) 
            return ResourcePath + name;

        return name;
    }

    public Assembly? Load()
    {
        using var _Stream = assembly.GetManifestResourceStream(GetPath());
        
        if (_Stream != null) 
            return Assembly.Load(_Stream.ReadFully());
        
        LogWarning("Resource not found: " + GetPath());
        return null;


    }
}