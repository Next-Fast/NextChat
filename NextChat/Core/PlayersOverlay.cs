using HarmonyLib;
using NextChat.Patches;
using Rewired.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NextChat.Core;


public class PlayersOverlay 
{
    public static PlayersOverlay? Instance { get; private set; }
    public static PlayerIconInstance IcoPrefab { get; private set; } = null!;
    public readonly List<PlayerIconInstance> _AllInstance = [];
    
    public static PlayerIconInstance CreateIcon(PlayerControl _player)
    {
        LogInfo($"CreateIcon {_player.PlayerId} {_player.Data.PlayerName}");
        var instance = Object.Instantiate(IcoPrefab);
        instance.gameObject.name = $"Icon:{_player.PlayerId}{_player.Data.PlayerName}";
        instance.id = _player.PlayerId;
        instance.UpdateIcon(_player);
        return instance;
    }

    private PlayerIconInstance CreatePrefab()
    {
        var instance = UnityHelper.CreateObject<PlayerIconInstance>("IconPrefab", IconHolder?.transform, Vector3.zero);
        instance.Back = UnityHelper.CreateObject<SpriteRenderer>("Back", instance.transform, new Vector3(0, 0, 0.1f));
        instance.Back.sprite = Sprites.OverlayIcon.GetSprite(0);
        instance.Back.color = new Color(0.45f, 0.45f, 0.45f);
        
        instance.Front = UnityHelper.CreateObject<SpriteRenderer>("Front", instance.transform, new Vector3(0, 0, 0.05f));
        instance.Front.sprite = Sprites.OverlayIcon.GetSprite(1);
        instance.Front.color = new Color(0.23f, 0.23f, 0.23f);
        
        instance.SpriteMask = UnityHelper.CreateObject<SpriteMask>("Mask", instance.transform, Vector3.zero);
        instance.SpriteMask.sprite = Sprites.OverlayIcon.GetSprite(1);

        instance.Poolable = Object.Instantiate(HudManager.Instance.IntroPrefab.PlayerPrefab, instance.SpriteMask.transform, true);
        instance.Poolable.transform.localPosition = new Vector3(0, -0.3f, -1f);
        instance.Poolable.TogglePet(false);
        instance.Poolable.cosmetics.SetMaskType(PlayerMaterial.MaskType.ComplexUI);
        instance.Poolable.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f);
        instance.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        
        instance.gameObject.SetActive(false);
        return instance.DontDestroyOnLoad();
    }

    public PlayersOverlay(Func<PlayerIconInstance, bool> isEnable)
    {
        IsEnable = isEnable;
        Instance = this;
    }

    public GameObject? IconHolder { get; private set; }
    public Func<PlayerIconInstance, bool> IsEnable { get; set; }
    

    // ReSharper disable Unity.PerformanceAnalysis
    internal void OnUpdate()
    {

        if (!IconHolder)
        {
            IconHolder = UnityHelper.CreateObject("IconHolder", HudManager.Instance.transform, new Vector3(0, 2.7f, -120f)).Dont();
            return;
        }

        if((MeetingHud.Instance && !ExileController.Instance) || PlayerCustomizationMenu.Instance || GameSettingMenu.Instance)
        {
            IconHolder?.gameObject.SetActive(false);
            return;
        }
        
        IconHolder?.gameObject.SetActive(true);

        if (!IcoPrefab && HudManager.Instance.IntroPrefab.PlayerPrefab)
        {
            IcoPrefab = CreatePrefab();
        }

        if (!IcoPrefab)
            return;

        if (_AllInstance.Count != NextVoiceManager.Instance.Players.Count)
        {
            _AllInstance.Clear();
            foreach (var player in NextVoiceManager.Instance.Players)
            {
                var icon = CreateIcon(player.player);
                icon.UpdateIcon(player.player);
                _AllInstance.Add(icon);
            }
            
            LogInfo($"Instance Count:{_AllInstance.Count}");
        }
        
        _AllInstance.RemoveAll(n => n.IsNullOrDestroyed());
        
        var num = 0;
        foreach (var i in _AllInstance)
        {
            i.gameObject.SetActive(true);
            if (!i.gameObject.active) continue;
            i.gameObject.transform.localPosition = new Vector3(0.45f * num, 0f) - new Vector3(0.225f * (num - 1), 0f, 0f);
            num++;
        }
    }
}

public class OverlyManager : MonoBehaviour
{
    public static OverlyManager? Instance { get; private set; }
    public static PlayersOverlay? CurrentOverlay { get; private set; }

    public OverlyManager()
    {
        Instance = this;
    }

    public void OnEnable()
    {
        if (CurrentOverlay == null)
            CreateOverlay();
    }

    public void LateUpdate()
    {
        if (!PlayerPatch.HasHudManager)
            return;

        CurrentOverlay?.OnUpdate();
    }

    public static void CreateOverlay()
    {
        CurrentOverlay = new PlayersOverlay(n => true);
    }
}