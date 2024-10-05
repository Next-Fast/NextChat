using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;
using SStream = System.IO.Stream;

namespace Next_Chat.Core;

public static class UnityHelper
{
    public static IRegionInfo CurrentServer => DestroyableSingleton<ServerManager>.Instance.CurrentRegion;
    public static bool IsCustomServer => CurrentServer.TranslateName 
        is StringNames.NoTranslation || (CurrentServer.TranslateName != StringNames.ServerAS && CurrentServer.TranslateName != StringNames.ServerEU && CurrentServer.TranslateName != StringNames.ServerNA);

    
    public static readonly List<Sprite?> CacheSprite = [];

    public static T Dont<T>(this T obj) where T : Object
    {
        obj.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
        return obj;
    }
    
    public static Sprite? loadSpriteFromResources(string path, float pixelsPerUnit, bool cache = true)
    {
        try
        {
            var fileName = Path.GetFileName(path) + $"_{pixelsPerUnit}";
            if (cache && CacheSprite.Exists(n => n?.name == fileName)) 
                return CacheSprite.FirstOrDefault(n => n?.name == fileName);
            
            var texture = loadTextureFromResources(path);
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture!.width, texture.height), new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
            sprite.name = fileName;
            switch (cache)
            {
                case true:
                    sprite.Dont();
                    break;
                case false:
                    return sprite;
            }

            CacheSprite.Add(sprite);
            return sprite;
        }
        catch
        {
            LogError("loading sprite from path: " + path);
        }

        return null;
    }

    public static unsafe Texture2D? loadTextureFromResources(string path)
    {
        try
        {
            var texture = new Texture2D(2, 2, TextureFormat.ARGB32, true);
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream(path);
            var length = stream!.Length;
            var byteTexture = new Il2CppStructArray<byte>(length);
            _ = stream.Read(new Span<byte>(IntPtr.Add(byteTexture.Pointer, IntPtr.Size * 4).ToPointer(), (int)length));
            texture.LoadImage(byteTexture, false);
            return texture;
        }
        catch
        {
            LogError("loading texture from resources: " + path);
        }

        return null;
    }
    
    public static Sprite LoadSprite(this SStream stream, bool DontUnload , Vector2 pivot, float pixelsPerUnit)
    {
        var texture = LoadTexture(stream, DontUnload);
        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, pixelsPerUnit);
        if (DontUnload)
            sprite.Dont();
        return sprite;
    }

    public static Texture2D LoadTexture(this SStream stream, bool DontUnload)
    {
        var texture = new Texture2D(2, 2, TextureFormat.ARGB32, true);
        var byteTexture = stream.ReadFully();
        texture.LoadImage(byteTexture, false);
        if (DontUnload)
            texture.Dont();
        return texture;
    }
    
    public static byte[] ReadFully(this SStream? input)
    {
        using var ms = new MemoryStream();
        input?.CopyTo(ms);
        return ms.ToArray();
    }
    
    public static void Destroy(this Object obj)
    {
        Object.Destroy(obj);
    }
    
    public static void AddListener(this UnityEvent @event, Action action)
    {
        @event.AddListener(action);
    }
    
    public static void AddListener<T>(this UnityEvent<T> @event, Action<T> action)
    {
        @event.AddListener(action);
    }

    public static GameObject DestroyAllChildren<T>(this GameObject obj) where T : MonoBehaviour
    {
        var list = obj.GetComponentsInChildren<T>();
        list.Do(Object.Destroy);
        return obj;
    }

    public static IRegionInfo CreateHttpRegion(string name, string ip, ushort port)
    {
        return new StaticHttpRegionInfo(name,
                StringNames.NoTranslation,
                ip,
                new Il2CppReferenceArray<ServerInfo>(
                [
                    new ServerInfo(name, ip, port, false)
                ])
            )
            .Cast<IRegionInfo>();
    }

    public static bool TryGet<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate, [MaybeNullWhen(false)] out T item) where T : class
    {
        item = null;
        var get = enumerable.ToList().FirstOrDefault(predicate);
        return get != null;
    }
    
}