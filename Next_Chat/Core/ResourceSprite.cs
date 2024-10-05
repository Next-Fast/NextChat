using System.Reflection;
using UnityEngine;

namespace Next_Chat.Core;

public class ResourceSprite(
    string pathName = "",
    float pixel = 115f,
    bool cache = true
    )
{


    private static readonly Assembly assembly = Assembly.GetExecutingAssembly();
    
    private static readonly string ResourcePath = $"{assembly.GetName().Name}.Resources.";

    public readonly bool _cache = cache;

    public readonly string _pathName = pathName;

    public float _pixel = pixel;
    private Sprite? _sprite;


    public string Path => GetPath();

    public static implicit operator Sprite(ResourceSprite rs)
    {
        return rs.GetSprite();
    }

    public Sprite GetSprite()
    {
        
        if (_sprite != null && _sprite.pixelsPerUnit == _pixel)
            return _sprite;

        _sprite = UnityHelper.loadSpriteFromResources(GetPath(), _pixel, _cache);
        return _sprite!;
    }

    private string GetPath()
    {
        if (assembly.GetManifestResourceNames().Contains(ResourcePath + _pathName)) 
            return ResourcePath + _pathName;

        return _pathName;
    }

    internal void Destroy()
    {
        _sprite?.Destroy();
    }
}