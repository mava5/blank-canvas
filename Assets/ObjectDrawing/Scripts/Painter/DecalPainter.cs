
#if UNITY_EDITOR
using UnityEditor;
#endif

using ObjectDrawing;

using UnityEngine;

using UtilityEditor;
using UtilityEnum;


public class DecalPainter : QuickPainter
{
    private const float MAX_SIZE_MULTIPLIER = 100f;
    private const float MIN_SIZE = 0.01f;
    private const float MAX_SIZE = 5f;
    private const float MAX_LENGTH_MULTIPLIER = 100f;
    private const float MIN_LENGTH = 0.01f;
    private const float MAX_LENGTH = 5f;

    [SerializeField, Range(1, 10)] private int drawCount = 1;

    [Header("Size:")]
    [SerializeField, Range(1f, MAX_SIZE_MULTIPLIER)] private float sizeMultiplier = 1f;
    [SerializeField, Range(MIN_SIZE, MAX_SIZE)] private float startSize = 2f;
    [SerializeField, Range(MIN_SIZE, MAX_SIZE)] private float endSize = 2f;

    [Header("Length:")]
    [SerializeField, Range(1f, MAX_LENGTH_MULTIPLIER)] private float lengthMultiplier = 1f;
    [SerializeField, Range(MIN_LENGTH, MAX_LENGTH)] private float length = 2f;

    [Header("Brush:")]
    [SerializeField] private bool isErasing = false;
    [SerializeField] private int colorIndex = 0;
    [SerializeField] private float intensity = 1f;
    [SerializeField] private int strokeNum = 0;
    [Space]
    [SerializeField] private bool useMaterial = false;
    [EnableIfNot("useMaterial")]
    [SerializeField] private Texture texture = null;
    [EnableIfNot("useMaterial")]
    [SerializeField] private ColorChannel targetChannel = ColorChannel.r;
    [EnableIf("useMaterial")]
    [SerializeField] private Material material = null;

    private ColoredBrush brush = null;


    protected override void Awake()
    {
        base.Awake();

        if (useMaterial)
        {
            brush = new MaterialBrush(material)
            {
                IsJittered = true,
                IsErasing = isErasing,
                ColorIndex = colorIndex,
                Intensity = intensity,
                StrokeNum = strokeNum,
            };
        }
        else
        {
            brush = new TextureBrush(texture, targetChannel)
            {
                IsJittered = true,
                IsErasing = isErasing,
                ColorIndex = colorIndex,
                Intensity = intensity,
                StrokeNum = strokeNum,
            };
        }
    }


    public void Paint()
    {
        if (!CanPaint) { return; }

        OnPaint();
    }

    protected override void OnPaint()
    {
        for (int i = 0; i < drawCount; i++)
        { PaintDecal(this, transform.position, transform.rotation); }
    }

    protected static void PaintDecal(DecalPainter decalPainter, Vector3 position, Quaternion rotation)
    {
        float sizeMultiplier = Mathf.Clamp(decalPainter.sizeMultiplier, 1f, MAX_SIZE_MULTIPLIER);
        float nearSize = Mathf.Max(decalPainter.startSize, MIN_SIZE) * sizeMultiplier;
        float farSize = Mathf.Clamp(decalPainter.endSize, nearSize, MAX_SIZE) * sizeMultiplier;

        float lengthMultiplier = Mathf.Clamp(decalPainter.lengthMultiplier, 1f, MAX_LENGTH_MULTIPLIER);
        float length = Mathf.Clamp(decalPainter.length, MIN_LENGTH, MAX_LENGTH) * lengthMultiplier;

        if (nearSize == farSize)
        {
            float nearDistance = 0.1f;
            float farDistance = length + nearDistance;

            position -= (rotation * Vector3.forward) * nearDistance;

            DrawingSystem.DrawOrtho(decalPainter.brush, position, rotation, nearSize, nearDistance, farDistance);
        }
        else
        {
            float fov = 2f * Mathf.Atan((farSize - nearSize) / length) * Mathf.Rad2Deg;

            float nearDistance = nearSize / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            float farDistance = nearDistance + length;

            position -= rotation * Vector3.forward * nearDistance;

            DrawingSystem.DrawPerspective(decalPainter.brush, position, rotation, fov, nearDistance, farDistance);
        }
    }


#if UNITY_EDITOR

    protected virtual void OnDrawGizmosSelected()
    { DrawDecalGizmo(startSize * sizeMultiplier, endSize * sizeMultiplier, length * lengthMultiplier, transform.position, transform.rotation, true); }

    protected virtual void OnDrawGizmos()
    { DrawDecalGizmo(startSize * sizeMultiplier, endSize * sizeMultiplier, length * lengthMultiplier, transform.position, transform.rotation, false); }

    protected static void DrawDecalGizmo(float nearSize, float farSize, float length, Vector3 position, Quaternion rotation, bool selected)
    {
        nearSize = Mathf.Clamp(nearSize, MIN_SIZE, MAX_SIZE * MAX_SIZE_MULTIPLIER);
        farSize = Mathf.Clamp(farSize, nearSize, MAX_SIZE * MAX_SIZE_MULTIPLIER);
        length = Mathf.Clamp(length, 0f, MAX_LENGTH * MAX_LENGTH_MULTIPLIER);

        float nearHalfSize = nearSize * 0.5f;
        float farHalfSize = farSize * 0.5f;

        Vector3 forward = rotation * Vector3.forward;
        Vector3 right = rotation * Vector3.right;
        Vector3 up = rotation * Vector3.up;
        Vector3 farCenter = position + forward * length;

        Vector3 nearTL = position + (-right + up) * nearHalfSize;
        Vector3 nearTR = position + (right + up) * nearHalfSize;
        Vector3 nearBR = position + (right - up) * nearHalfSize;
        Vector3 nearBL = position + (-right - up) * nearHalfSize;
        Vector3 farTL = farCenter + (-right + up) * farHalfSize;
        Vector3 farTR = farCenter + (right + up) * farHalfSize;
        Vector3 farBR = farCenter + (right - up) * farHalfSize;
        Vector3 farBL = farCenter + (-right - up) * farHalfSize;
        
        Gizmos.color = Color.clear;
        Matrix4x4 originalMatrix = Gizmos.matrix;
        Matrix4x4 decalMatrix = Matrix4x4.TRS(position, rotation, new(nearSize, nearSize, 0f));
        Gizmos.matrix = decalMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = originalMatrix;

        Color nearColor = selected ? new(0.5f, 0.5f, 1f, 1f) : new(1f, 1f, 1f, 0.5f);
        Color farColor = new(nearColor.r, nearColor.g, nearColor.b, nearColor.a * 0.8f);
        Color directionColor = selected ? new(1f, 1f, 1f, 0.8f) : new(1f, 1f, 1f, 0.5f);
        Color edgeColor = selected ? new(0f, 0f, 0f, 0.5f) : new(0f, 0f, 0f, 0.2f);

        Gizmos.color = nearColor;
        Gizmos.DrawLineStrip(new Vector3[] { nearTL, nearTR, nearBR, nearBL }, true);
        Gizmos.color = directionColor;
        Gizmos.DrawLine(position, position + (right * 0.8f) * nearHalfSize);
        Gizmos.DrawLine(position, position + (up * 0.8f) * nearHalfSize);
        Gizmos.color = edgeColor;
        Gizmos.DrawLine(nearTL, farTL);
        Gizmos.DrawLine(nearTR, farTR);
        Gizmos.DrawLine(nearBR, farBR);
        Gizmos.DrawLine(nearBL, farBL);
        Gizmos.color = farColor;
        Gizmos.DrawLineStrip(new Vector3[] { farTL, farTR, farBR, farBL }, true);
        Gizmos.color = Color.white;
    }

#endif
}


#if UNITY_EDITOR

[CustomEditor(typeof(DecalPainter)), CanEditMultipleObjects]
internal class DecalPainterEditor : AdvancedEditor { }

#endif
