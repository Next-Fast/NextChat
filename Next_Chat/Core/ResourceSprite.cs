using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Next_Chat.Core;

public class ResourceSprite(
    string pathName = "",
    float pixel = 115f,
    int x = 0, 
    int y = 0,
    bool cache = true
    )
{
    
    private static readonly Assembly assembly = Assembly.GetExecutingAssembly();
    
    private static readonly string ResourcePath = $"{ResourceInfo.AssemblyName}.Resource.";

    public readonly bool _cache = cache;

    public readonly string _pathName = pathName;

    public float _pixel = pixel;
    private Sprite? _sprite;
    public Sprite[] Sprites = [];


    public string Path => GetPath();

    public static implicit operator Sprite(ResourceSprite rs)
    {
        return rs.GetSprite();
    }

    public Sprite GetSprite(int index)
    {
        if (x == 0 && y == 0) return GetSprite();
        var texture2D = UnityHelper.loadTextureFromResources(GetPath())!;
        if (!Sprites[index]) 
            Sprites[index] = UnityHelper.loadSprite(texture2D, _pixel, GetRect(texture2D, index), false)!;
        
        return Sprites[index];
    }

    public Sprite GetSprite()
    {
        
        if (_sprite != null && _sprite.pixelsPerUnit == _pixel)
            return _sprite;

        LogInfo($"Load Path Form Resources: {GetPath()} : {ResourcePath + _pathName}");
 
        _sprite = UnityHelper.loadSpriteFromResources(GetPath(), _pixel);
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
        Sprites.Do(UnityHelper.Destroy);
    }

    private Rect GetRect(Texture2D texture2D, int index)
    {
        var division = new Tuple<int, int>(x, y);
        var size = new Tuple<int, int>(texture2D.width / division.Item1, texture2D.height / division.Item2);
        Sprites = new Sprite[division.Item1 * division.Item2];
        var _x = index % division.Item1;
        var _y = index / division.Item1;
        return new Rect(_x * size.Item1, (division.Item2 - _y - 1) * size.Item2, size.Item1, size.Item2);
    }
}