using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;
using SStream = System.IO.Stream;

namespace NextChat.Core;

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

    public static T DontDestroyOnLoad<T>(this T obj) where T : Object
    {
        Object.DontDestroyOnLoad(obj);
        return obj;
    }
    
    public static Sprite? loadSpriteFromResources(string path, float pixelsPerUnit, Rect? _rect = null, bool cache = true)
    {
        try
        {
            var fileName = Path.GetFileName(path) + $"_{pixelsPerUnit}";
            if (cache && CacheSprite.Exists(n => n?.name == fileName)) 
                return CacheSprite.FirstOrDefault(n => n?.name == fileName);
            
            var texture = loadTextureFromResources(path);
            var sprite = Sprite.Create(texture, _rect ?? new Rect(0, 0, texture!.width, texture.height), new Vector2(0.5f, 0.5f),
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
    
    public static Sprite? loadSprite(Texture2D texture, float pixelsPerUnit, Rect? _rect = null, bool cache = true)
    {
        try
        {
            var fileName = texture.name + $"_{pixelsPerUnit}";
            var sprite = Sprite.Create(texture, _rect ?? new Rect(0, 0, texture!.width, texture.height), new Vector2(0.5f, 0.5f),
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
            LogError("texture: " + texture.name);
        }
        return null;
    }

    public static unsafe Texture2D? loadTextureFromResources(string path)
    {
        try
        {
            var texture = new Texture2D(2, 2, TextureFormat.ARGB32, true)
            {
                name = Path.GetFileName(path)
            };
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
    
    public static GameObject CreateObject(string objName, Transform? parent, Vector3 localPosition,int? layer = null)
    {
        var obj = new GameObject(objName);
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPosition;
        obj.transform.localScale = new Vector3(1f, 1f, 1f);
        if (layer.HasValue) obj.layer = layer.Value;
        else if (parent) obj.layer = parent!.gameObject.layer;
        return obj;
    }

    public static T CreateObject<T>(string objName, Transform? parent, Vector3 localPosition, int? layer = null)
        where T : Component
    {
        var obj = CreateObject(objName, parent, localPosition, layer);
        var component = obj.AddComponent<T>();

        return component;
    }


    public static List<T> ToSystemList<T>(this Il2CppSystem.Collections.Generic.List<T> list)
    {
        var newList = new List<T>();
        foreach (var value in list)
            newList.Add(value);
        return newList;
    }

    public static IEnumerable<T> GetFastEnumerator<T>(this Il2CppSystem.Collections.Generic.List<T> list) where T : Il2CppSystem.Object
        => new Il2CppListEnumerable<T>(list);
    
}

#pragma warning disable CS0169
#pragma warning disable CS0649
public unsafe class Il2CppListEnumerable<T> : IEnumerable<T>, IEnumerator<T> where T : Il2CppSystem.Object
{
    private struct Il2CppListStruct
    {
        private IntPtr _unusedPtr1;
        private IntPtr _unusedPtr2;
        public IntPtr _items;
        public int _size;
    }

    private static readonly int _elemSize;
    private static readonly int _offset;
    private static readonly Func<IntPtr, T> _objFactory;

    static Il2CppListEnumerable()
    {
        _elemSize = IntPtr.Size;
        _offset = 4 * IntPtr.Size;

        var constructor = typeof(T).GetConstructor([typeof(IntPtr)]);
        var ptr = Expression.Parameter(typeof(IntPtr));
        var create = Expression.New(constructor!, ptr);
        var lambda = Expression.Lambda<Func<IntPtr, T>>(create, ptr);
        _objFactory = lambda.Compile();
    }

    private readonly IntPtr _arrayPointer;
    private readonly int _count;
    private int _index = -1;

    public Il2CppListEnumerable(Il2CppSystem.Collections.Generic.List<T> list)
    {
        var listStruct = (Il2CppListStruct*)list.Pointer;
        _count = listStruct->_size;
        _arrayPointer = listStruct->_items;
    }

    object IEnumerator.Current => Current;
    public T Current { get; private set; } = null!;

    public bool MoveNext()
    {
        if (++_index >= _count) return false;
        var refPtr = *(IntPtr*)IntPtr.Add(IntPtr.Add(_arrayPointer, _offset), _index * _elemSize);
        Current = _objFactory(refPtr);
        return true;
    }

    public void Reset()
    {
        _index = -1;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return this;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this;
    }
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    } 
}