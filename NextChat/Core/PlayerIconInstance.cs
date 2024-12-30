using Rewired.Utils;
using UnityEngine;

namespace NextChat.Core;

public class PlayerIconInstance : MonoBehaviour
{
    public byte id = byte.MaxValue;
    
    public PoolablePlayer? Poolable;
    public SpriteRenderer? Back;
    public SpriteRenderer? Front;
    public SpriteMask? SpriteMask;

    public void UpdateIcon(PlayerControl player)
    {
        Poolable?.UpdateFromPlayerOutfit(player.CurrentOutfit, PlayerMaterial.MaskType.ComplexUI, false, false);
    }
}