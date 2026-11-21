
using ObjectDrawing;

using UnityEngine;


public abstract class ColoredBrush : Brush
{
    protected abstract class ColoredBrush_ShaderIDs
    {
        public static readonly int IsErasingID = Shader.PropertyToID("_IsErasing");
        public static readonly int ColorIndexID = Shader.PropertyToID("_ColorIndex");
        public static readonly int IntensityID = Shader.PropertyToID("_Intensity");
        public static readonly int StrokeNumID = Shader.PropertyToID("_StrokeNum");
    }

    public const int COLOR_BITS = 3;
    public const int AMOUNT_BITS = DrawingSystem.BRUSH_INFO_BITS - COLOR_BITS;
    public const int MAX_COLORS = (1 << COLOR_BITS) + 1;

    public bool IsErasing
    {
        get => isErasing;
        set { isErasing = value; MPB.SetInt(ColoredBrush_ShaderIDs.IsErasingID, isErasing ? 1 : 0); }
    }
    public int ColorIndex
    {
        get => colorIndex;
        set { colorIndex = value; MPB.SetFloat(ColoredBrush_ShaderIDs.ColorIndexID, colorIndex); }
    }
    public float Intensity
    {
        get => intensity;
        set { intensity = value; MPB.SetFloat(ColoredBrush_ShaderIDs.IntensityID, intensity); }
    }
    public int StrokeNum
    {
        get => strokeNum;
        set { strokeNum = value; MPB.SetInt(ColoredBrush_ShaderIDs.StrokeNumID, strokeNum); }
    }


    protected internal override bool FinalAppliesToPaintOnly => false;

    private bool isErasing = false;
    private int colorIndex = 0;
    private float intensity = 1f;
    private int strokeNum = 0;


    protected ColoredBrush() => SetInitial();

    public void RandomizeStrokeNum() => strokeNum = Random.Range(-5000, 5000);

    protected virtual void SetInitial()
    {
        IsErasing = isErasing;
        ColorIndex = colorIndex;
        Intensity = intensity;
        StrokeNum = strokeNum;
    }
}