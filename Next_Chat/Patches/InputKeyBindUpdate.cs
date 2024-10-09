using System.Diagnostics.CodeAnalysis;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace Next_Chat.Patches;

[SuppressMessage("Performance", "CA1822:将成员标记为 static")]
public sealed class InputKeyBindUpdate : MonoBehaviour
{
    public record KeyBind(string name, Action OnInput, params KeyCode[] KeyCodes)
    {
        public bool IsPressed => KeyCodes.All(Input.GetKey);
    }

    private static readonly List<KeyBind> AllBind = [];

    public static void Register(string name, Action onInput, params KeyCode[] keyCodes) => Register(new KeyBind(name, onInput, keyCodes));
    public static void Register(string name, Action onInput, KeyCode key) => Register(name, onInput, [key]);
    public static void Register(KeyBind bind)
    {
        if (bind.KeyCodes.Length == 0)return;
        if (AllBind.Any(n => n.name == bind.name)) return;
        AllBind.Add(bind);
    }

    static InputKeyBindUpdate()
    {
        ClassInjector.RegisterTypeInIl2Cpp<InputKeyBindUpdate>();
    }

    public void Update()
    {
        foreach (var bing in AllBind.Where(n => n.IsPressed))
        {
            bing.OnInput();
        }
    }

    public void FixedUpdate()
    {
        foreach (var player in NextVoiceManager.Instance.Players)
        {
            player.OnUpdate();
        }
    }
}

