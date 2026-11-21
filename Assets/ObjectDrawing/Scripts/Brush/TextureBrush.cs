
using UnityEngine;

using UtilityEnum;


public class TextureBrush : ColoredBrush
{
    protected abstract class TextureBrush_ShaderIDs : ColoredBrush_ShaderIDs
    {
        public static readonly int BrushTextureID = Shader.PropertyToID("_BrushTexture");
        public static readonly int TargetChannelID = Shader.PropertyToID("_TargetChannel");
    }

    public Texture Texture
    {
        get => texture;
        set { texture = value; if (texture != null) { MPB.SetTexture(TextureBrush_ShaderIDs.BrushTextureID, texture); } }
    }
    public ColorChannel TargetChannel
    {
        get => targetChannel;
        set { targetChannel = value; MPB.SetInt(TextureBrush_ShaderIDs.TargetChannelID, (int)targetChannel); }
    }


    private Texture texture = null;
    private ColorChannel targetChannel = ColorChannel.r;


    public TextureBrush(Texture texture, ColorChannel targetChannel = ColorChannel.r)
    {
        Texture = texture;
    }

    protected override void SetInitial()
    {
        base.SetInitial();

        Texture = texture;
        TargetChannel = targetChannel;
    }
}
