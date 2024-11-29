using UnityEngine;
using Object = UnityEngine.Object;

namespace NextChat.Core;


public class PlayersOverlay
{
    public static PlayersOverlay? Instance { get; private set; }
    public static PlayerIconInstance? IcoPrefab { get; private set; }
    public readonly List<PlayerIconInstance> _AllInstance = [];

    public PlayerIconInstance GetOrCreate(INextPlayer _player)
    {
        var instance = _AllInstance.FirstOrDefault(n => n.player == _player.player) 
                       ?? 
                       CreateIcon(_player);
        return instance;
    }
    
    public PlayerIconInstance CreateIcon(INextPlayer _player)
    {
        IcoPrefab ??= CreatePrefab();
        var instance = _AllInstance.FirstOrDefault(n => n.player is null);
        if (instance is null)
        {
            var newInstance = Object.Instantiate(IcoPrefab, IconHolder.transform, true);
            newInstance.name = $"Icon:{_player.player.name}";
            newInstance.SetPlayer(_player.player);
            newInstance.IsEnable = IsEnable;
            _AllInstance.Add(newInstance);
            return newInstance;
        }

        instance.transform.SetParent(IconHolder.transform);
        instance.gameObject.name = $"Icon:{_player.player.name}";
        instance.IsEnable = IsEnable;
        instance.SetPlayer(_player.player);
        return instance;
    }

    private static PlayerIconInstance CreatePrefab()
    {
        var instance = UnityHelper.CreateObject<PlayerIconInstance>("IconPrefab", null, Vector3.zero);
        instance.Back = UnityHelper.CreateObject<SpriteRenderer>("Back", instance.transform, new Vector3(0, 0, 0.1f));
        instance.Back.sprite = Sprites.OverlayIcon.GetSprite(0);
        instance.Back.color = new Color(0.45f, 0.45f, 0.45f);
        
        instance.Front = UnityHelper.CreateObject<SpriteRenderer>("Front", instance.transform, new Vector3(0, 0, 0.05f));
        instance.Front.sprite = Sprites.OverlayIcon.GetSprite(1);
        instance.Front.color = new Color(0.23f, 0.23f, 0.23f);
        
        instance.SpriteMask = UnityHelper.CreateObject<SpriteMask>("Mask", instance.transform, Vector3.zero);
        instance.SpriteMask.sprite = Sprites.OverlayIcon.GetSprite(1);
        
        instance.Poolable = GetPlayerIcon(PlayerControl.LocalPlayer.CurrentOutfit, 
            instance.SpriteMask.transform, new Vector3(0, -0.3f, -1f),
            Vector3.one);
        instance.Poolable.TogglePet(false);
        instance.Poolable.cosmetics.SetMaskType(PlayerMaterial.MaskType.ComplexUI);
        instance.Poolable.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f);
        instance.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        
        AddSortingGroup(
            instance.Poolable.cosmetics.skin.layer.gameObject,
            instance.Poolable.cosmetics.hat.FrontLayer.gameObject,
            instance.Poolable.cosmetics.visor.Image.gameObject,
            instance.Poolable.cosmetics.currentBodySprite.BodySprite.gameObject
        );
        instance.gameObject.SetActive(false);
        return instance.DontDestroyOnLoad();
    }

    public PlayersOverlay(Func<PlayerIconInstance, bool> isEnable)
    {
        IsEnable = isEnable;
        Instance = this;
        IconHolder = UnityHelper.CreateObject("IconHolder", HudManager.Instance.transform, new Vector3(0, 2.7f, -120f)).Dont();
    }
    
    private readonly GameObject IconHolder;
    public Func<PlayerIconInstance, bool> IsEnable { get; set; }
    

    internal void OnUpdate()
    {
        if (!IconHolder) return;
        
        if((MeetingHud.Instance && !ExileController.Instance) || PlayerCustomizationMenu.Instance || GameSettingMenu.Instance)
        {
            IconHolder.gameObject.SetActive(false);
            return;
        }
        IconHolder.gameObject.SetActive(true);
        
        var num = 0;
        foreach (var i in _AllInstance.Where(i => i.gameObject.active))
        {
            i.gameObject.transform.localPosition = new Vector3(0.45f * num, 0f) - new Vector3(0.225f * (num - 1), 0f, 0f);
            num++;
        }
    }
    
    
    public static PoolablePlayer Prefab => HudManager.Instance.IntroPrefab.PlayerPrefab;
    public static PoolablePlayer GetPlayerIcon(NetworkedPlayerInfo.PlayerOutfit outfit, Transform? parent,Vector3 position,Vector3 scale,bool flip = false, bool includePet = true)
    {
        var player = Object.Instantiate(Prefab);
        if(parent)
            player.transform.SetParent(parent);
        player.name = outfit.PlayerName;
        player.SetFlipX(flip);
        player.transform.localPosition = position;
        player.transform.localScale = scale;
        player.UpdateFromPlayerOutfit(outfit, PlayerMaterial.MaskType.None, false, includePet);
        player.ToggleName(false);
        player.SetNameColor(Color.white);
        return player;
    }


    internal static void AddSortingGroup(params GameObject[] objects)
    {
        foreach (var obj in objects)
        {
            if (!obj.TryGetComponent<ZOrderedSortingGroup>(out _))
                obj.AddComponent<ZOrderedSortingGroup>();
        }
    }
}