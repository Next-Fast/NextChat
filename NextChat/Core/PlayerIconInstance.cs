using UnityEngine;

namespace NextChat.Core;

public class PlayerIconInstance : MonoBehaviour
{
    public PlayerControl? player { get; set; }
    public PoolablePlayer? Poolable { get; set; }

    public SpriteRenderer? Back;
    public SpriteRenderer? Front;
    public SpriteMask? SpriteMask;
    public Func<PlayerIconInstance, bool> IsEnable = null!;

    public void Start()
    {
        Back ??= UnityHelper.CreateObject<SpriteRenderer>("Back", transform, new Vector3(0, 0, 0.1f));
        Back.sprite = Sprites.OverlayIcon.GetSprite(0);
        Back.color = new Color(0.45f, 0.45f, 0.45f);
        
        Front ??= UnityHelper.CreateObject<SpriteRenderer>("Front", transform, new Vector3(0, 0, 0.05f));
        Front.sprite = Sprites.OverlayIcon.GetSprite(1);
        Front.color = new Color(0.23f, 0.23f, 0.23f);
        
        SpriteMask ??= UnityHelper.CreateObject<SpriteMask>("Mask", transform, Vector3.zero);
        SpriteMask.sprite = Sprites.OverlayIcon.GetSprite(1);
        
        Poolable ??=PlayersOverlay.GetPlayerIcon(PlayerControl.LocalPlayer.CurrentOutfit, 
            SpriteMask.transform, new Vector3(0, -0.3f, -1f),
            Vector3.one);
        Poolable.TogglePet(false);
        Poolable.cosmetics.SetMaskType(PlayerMaterial.MaskType.ComplexUI);
        Poolable.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f);
        transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        
        PlayersOverlay.AddSortingGroup(
            Poolable.cosmetics.skin.layer.gameObject,
            Poolable.cosmetics.hat.FrontLayer.gameObject,
            Poolable.cosmetics.visor.Image.gameObject,
            Poolable.cosmetics.currentBodySprite.BodySprite.gameObject
        );
    }

    public void LateUpdate()
    {
        if (PlayersOverlay.Instance == null || player is null) return;
        gameObject.SetActive(IsEnable.Invoke(this));
    }

    public void UpdateIcon()
    {
        if (player is null) return;
        Poolable?.UpdateFromPlayerOutfit(player.CurrentOutfit, PlayerMaterial.MaskType.ComplexUI, false, false);
    }

    public void SetPlayer(PlayerControl _player)
    {
        player = _player;
        Poolable!.name = player.CurrentOutfit.PlayerName;
        UpdateIcon();
    }
}