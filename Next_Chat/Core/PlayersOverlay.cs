using InnerNet;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Next_Chat.Core;


public class PlayersOverlay
{
    public static PlayersOverlay? Instance { get; private set; }
    public static PlayerIconInstance? IcoPrefab { get; private set; }
    public readonly List<PlayerIconInstance> HasPlayerIcons = [];
    public readonly List<PlayerIconInstance> _AllInstance = [];

    public void CreateIcon(PlayerControl player)
    {
        if (!IcoPrefab)
            IcoPrefab = CreatePrefab();
        var instance = _AllInstance.FirstOrDefault(n => !n.player);
        if (!instance)
        {
            var newInstance = Object.Instantiate(IcoPrefab, IconHolder.transform);
            newInstance.SetPlayer(player);
            newInstance.IsEnable = IsEnable;
            _AllInstance.Add(newInstance);
        }

        instance!.IsEnable = IsEnable;
        instance.SetPlayer(player);
    }

    private PlayerIconInstance CreatePrefab()
    {
        var obj = UnityHelper.CreateObject("Icon", IconHolder.transform, Vector3.zero);
        var instance = obj.AddComponent<PlayerIconInstance>();
        var back = UnityHelper.CreateObject<SpriteRenderer>("Back", obj.transform, new Vector3(0, 0, 0.1f));
        back.sprite = Sprites.OverlayIcon.GetSprite(0);
        back.color = new Color(0.45f, 0.45f, 0.45f);
        
        var front = UnityHelper.CreateObject<SpriteRenderer>("Front", obj.transform, new Vector3(0, 0, 0.05f));
        front.sprite = Sprites.OverlayIcon.GetSprite(1);
        front.color = new Color(0.23f, 0.23f, 0.23f);
        
        var spriteMask = UnityHelper.CreateObject<SpriteMask>("Mask", obj.transform, Vector3.zero);
        spriteMask.sprite = Sprites.OverlayIcon.GetSprite(1);

        var player = GetPlayerIcon(PlayerControl.LocalPlayer.CurrentOutfit, 
            spriteMask.transform, new Vector3(0, -0.3f, -1f),
            Vector3.one);
        player.TogglePet(false);
        player.cosmetics.SetMaskType(PlayerMaterial.MaskType.ComplexUI);
        player.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f);
        obj.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        
        AddSortingGroup(
            player.cosmetics.skin.layer.gameObject,
            player.cosmetics.hat.FrontLayer.gameObject,
            player.cosmetics.visor.Image.gameObject,
            player.cosmetics.currentBodySprite.BodySprite.gameObject
        );
        instance.Poolable = player;
        return instance;
    }

    public PlayersOverlay(Func<PlayerIconInstance, bool> isEnable)
    {
        IsEnable = isEnable;
        Instance = this;
    }
    
    private readonly GameObject IconHolder = UnityHelper.CreateObject("IconHolder", HudManager.Instance.transform, new Vector3(0, 2.7f, -120f));
    public Func<PlayerIconInstance, bool> IsEnable { get; set; }


    // ReSharper disable Unity.PerformanceAnalysis
    internal void OnUpdate()
    {
        if((MeetingHud.Instance && !ExileController.Instance) || PlayerCustomizationMenu.Instance || GameSettingMenu.Instance)
        {
            IconHolder.gameObject.SetActive(false);
            return;
        }
        IconHolder.gameObject.SetActive(true);

        if (
            AmongUsClient.Instance.GameState < InnerNetClient.GameStates.Started
            &&
            PlayerControl.AllPlayerControls.Count != HasPlayerIcons.Count
            )
        {
            var noPlayer = PlayerControl.AllPlayerControls
                .ToSystemList()
                .First(p => HasPlayerIcons.All(i => i.player != p));

            CreateIcon(noPlayer);
        }
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
    

    private static void AddSortingGroup(params GameObject[] objects)
    {
        foreach (var obj in objects)
        {
            if (!obj.TryGetComponent<ZOrderedSortingGroup>(out _))
                obj.AddComponent<ZOrderedSortingGroup>();
        }
    }
}


public class PlayerIconInstance : MonoBehaviour
{
    public PlayerControl? player { get; set; }
    public PoolablePlayer Poolable { get; set; }
    public Func<PlayerIconInstance, bool>? IsEnable { get; set; }
    public bool HasPlayer => player;

    public void LateUpdate()
    {
        if (PlayersOverlay.Instance == null) return;
        gameObject.SetActive(player && (IsEnable?.Invoke(this) ?? false));

        if (HasPlayer) return;
        if (PlayersOverlay.Instance.HasPlayerIcons.Contains(this))
            PlayersOverlay.Instance.HasPlayerIcons.Remove(this);
    }

    public void UpdateIcon()
    {
        if (!player) return;
        Poolable.UpdateFromPlayerOutfit(player!.CurrentOutfit, PlayerMaterial.MaskType.ComplexUI, false, false);
    }

    public void SetPlayer(PlayerControl _player)
    {
        player = _player;
        Poolable.name = player.CurrentOutfit.PlayerName;
        UpdateIcon();
        if (!PlayersOverlay.Instance!.HasPlayerIcons.Contains(this))
            PlayersOverlay.Instance.HasPlayerIcons.Add(this);
    }
}


public class ZOrderedSortingGroup : MonoBehaviour
{
    private SortingGroup? group;
    private Renderer? renderer;
    public int ConsiderParents;
    public void SetConsiderParentsTo(Transform parent)
    {
        var num = 0;
        var t = transform;
        while(!(t == parent || t == null))
        {
            num++;
            t = t.parent;
        }
        ConsiderParents = num;
    }
    public void Start()
    {
        if(!gameObject.TryGetComponent<Renderer>(out renderer)) 
            group = gameObject.AddComponent<SortingGroup>();
    }

    private const float rate = 20000f;
    private const int baseValue = 5;

    public void Update()
    {
        var z = transform.localPosition.z;
        var t = transform;
        for (var i = 0; i < ConsiderParents; i++)
        {
            t = t.parent;
            z += t.localPosition.z;
        }
        var layer = baseValue - (int)(rate * z);
        if (group is not null)group.sortingOrder = layer;
        if(renderer is not null) renderer.sortingOrder = layer;
    }
}

