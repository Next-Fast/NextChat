using UnityEngine;

namespace NextChat.Core;

public class VCFrame : MonoBehaviour
{
    public SpriteRenderer? Renderer;
    public INextPlayer? Player;
    public float alpha;
    public Color col;
    public void SetPlayer(PlayerVoteArea area)
    {
        col = Palette.PlayerColors[area.TargetPlayerId];
        if(Mathf.Max(col.r, col.g, col.b) < 100) 
            col = Color.Lerp(col, Color.white, 0.4f);
    }

    public void Update()
    {
        if (Renderer is null) return;
        if (Player is null) return;
        alpha = Player.IsSpeaking ? 
            Mathf.Clamp(alpha + Time.deltaTime * 4f, 0f, 1f) 
            : 
            Mathf.Clamp(alpha - Time.deltaTime * 4f, 0f, 1f);
        col.a = (byte)(alpha * 255f);
        Renderer.color = col;
    }
}