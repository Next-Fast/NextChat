using System.Diagnostics.CodeAnalysis;
using NextChat.Core;
using UnityEngine;

namespace NextChat.Patches;

[SuppressMessage("Performance", "CA1822:将成员标记为 static")]
public sealed class InputKeyBindUpdate : MonoBehaviour
{
    public record KeyBind(string name, Action OnInput, params KeyCode[] KeyCodes)
    {
        public bool IsPressed() => KeyCodes.All(Input.GetKeyDown);
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
    

    public void Update()
    {
        foreach (var bing in AllBind.Where(n => n.IsPressed()))
        {
            LogInfo($"Input {bing.name} {string.Join(',', bing.KeyCodes.Select(n => n.ToString()))}");
            bing.OnInput();
        }
    }
    
    
    
    public void FixedUpdate()
    {
        foreach (var player in NextVoiceManager.Instance.Players)
        {
            player.OnUpdate();
        }
        PlayersOverlay.Instance?.OnUpdate();
    }
}

