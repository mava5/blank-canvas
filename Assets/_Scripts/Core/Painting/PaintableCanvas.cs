
using Cysharp.Threading.Tasks;

using DG.Tweening;

using ObjectDrawing;

using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using UtilityEditor;
using UtilityEnum;
using UtilityExtensions;


public class PaintableCanvas : MonoBehaviour//, ICarriable, IControllable
{
    private class BrushStroke
    {
        private static BrushStroke previousStroke;

        private readonly bool isErasing;
        private readonly int colorIndex;
        private readonly int strokeNum;
        private readonly int drawsGoal;
        private int drawsDone;

        private readonly PaintableCanvas canvas;
        private readonly Camera camera;
        private readonly ColoredBrush brush;

        private readonly Vector3 camPosition;
        private readonly Quaternion camRotation;

        private Vector2 start;
        private Vector2 end;
        private readonly Vector2 normalDir;
        private readonly float noiseOffset;

        private float targetAlpha;


        public BrushStroke(bool isErasing, int colorIndex, int drawsGoal, bool goRight,
            PaintableCanvas canvas, Camera camera, ColoredBrush brush)
        {
            this.isErasing = isErasing;
            this.colorIndex = colorIndex;
            strokeNum = Random.Range(-1000, 1000);
            this.drawsGoal = Mathf.Max(drawsGoal - 1, 0);
            drawsDone = 0;

            this.canvas = canvas;
            this.camera = camera;
            this.brush = brush;

            camPosition = camera.transform.position;
            camRotation = camera.transform.rotation;

            float angle = (goRight ? 180f : 0f) - Random.Range(-canvas.maxBrushRotation, canvas.maxBrushRotation);
            float angleRad = Mathf.Deg2Rad * angle;
            float angleEndRad = angleRad + Mathf.PI;
            start = new(Mathf.Cos(angleRad) * 0.5f, Mathf.Sin(angleRad) * 0.5f);
            end = new(Mathf.Cos(angleEndRad) * 0.5f, Mathf.Sin(angleEndRad) * 0.5f);
            start = (start * 0.9f) + (Vector2.one * 0.5f);
            end = (end * 0.9f) + (Vector2.one * 0.5f);

            start.x *= canvas.aspectRatio;
            end.x *= canvas.aspectRatio;

            Vector2 dir = (end - start).normalized;
            normalDir = new(dir.y, -dir.x);

            noiseOffset = Random.Range(-1000f, 1000f);

            targetAlpha = 0f;
        }

        public void SetTargetAlpha(float value) => targetAlpha = Mathf.Clamp01(value);

        public bool TryDrawNext()
        {
            if (drawsDone > drawsGoal) { return false; }

            if (this != previousStroke)
            {
                brush.IsErasing = isErasing;
                brush.ColorIndex = colorIndex;
                brush.StrokeNum = strokeNum;
            }
            previousStroke = this;

            if (drawsGoal == 0)
            {
                Vector2 position = start;
                position.x /= canvas.aspectRatio;
                DrawingSystem.DrawCursor(brush, camera, camPosition, camRotation, position, canvas.brushRadius, 0f);
                return false;
            }

            float alpha = drawsDone / (float)drawsGoal;
            float alphaNext = Mathf.Clamp01((drawsDone + 1) / (float)drawsGoal);
            float alpha_A = 1f - Mathf.Clamp01(Mathf.Abs((alpha * 2f) - 1f)); 

            (Vector2 position0, float rotation0) = GetPosRot(alpha);
            (Vector2 position1, float rotation1) = GetPosRot((alpha + alphaNext) / 2f);
            
            position0.x /= canvas.aspectRatio;
            position1.x /= canvas.aspectRatio;

            float zNear = canvas.brushZNear;
            float zFar = isErasing ? 1000f : canvas.brushZFar;

            brush.Intensity = alpha_A;
            DrawingSystem.DrawCursor(brush, camera, camPosition, camRotation, position0, alpha_A * canvas.brushRadius, rotation0, zNear, zFar);

            brush.Intensity = alpha_A * 0.5f;
            DrawingSystem.DrawCursor(brush, camera, camPosition, camRotation, position1, alpha_A * canvas.brushRadius, rotation1, zNear, zFar);

            drawsDone++;

            return true;
        }

        public bool TryDrawUpToTargetAlpha(int maxDraws) => TryDrawUpToAlpha(targetAlpha, maxDraws);

        public bool TryDrawUpToAlpha(float inputAlpha, int maxDraws)
        {
            if (drawsDone > drawsGoal) { return false; }

            float alpha = Mathf.Clamp01(inputAlpha);
            int drawsTarget = Mathf.FloorToInt(drawsGoal * alpha) + 1;
            int drawsToDo = Mathf.Clamp(drawsTarget - drawsDone, 0, maxDraws);

            for (int i = 0; i < drawsToDo; i++)
            { if (!TryDrawNext()) { return false; } }

            return true;
        }


        private (Vector2 position, float rotation) GetPosRot(float alpha)
        {
            Vector2 position0 = GetPos(alpha);
            Vector2 position1 = GetPos(alpha + 0.01f);
            Vector2 offset = position1 - position0;
            float rotation = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;

            return (position0, -(rotation - 90f));
        }

        private Vector2 GetPos(float alpha)
        {
            float perlin = (Mathf.PerlinNoise1D(noiseOffset + (alpha / canvas.noiseSize)) - 0.5f) * canvas.noiseAmplitude;
            float alpha_v = Mathf.Clamp01(Mathf.Abs((alpha * 2f) - 1f));
            perlin *= Mathf.Lerp(canvas.noiseAmplitudeEndMultiplier, 1f, alpha_v);

            return Vector2.Lerp(start, end, alpha) + normalDir * perlin;
        }
    }


    public bool IsLoaded => State == LoadingState.Loaded;
    public Painting Painting => painting;

    public LoadingState State { get; private set; } = LoadingState.Unloaded;

    internal PaintingManager PaintingManager => paintingManager;


    [SerializeField] private Painting painting = null;
    [SerializeField] private MeshRenderer canvasRenderer = null;
    [SerializeField] private Axis3D surfaceAxis = Axis3D.x;
    [SerializeField] private float aspectRatio = 1f;
    [SerializeField] private UniversalRendererData paintingRenderer = null;

    [Header("Brush:")]
    [SerializeField, Range(0f, 2f)] private float brushRadius = 0.1f;
    [SerializeField, Range(0f, 90f)] private float maxBrushRotation = 10f;
    [SerializeField, Range(0f, 1f)] private float noiseAmplitude = 0.1f;
    [SerializeField, Range(0f, 1f)] private float noiseAmplitudeEndMultiplier = 1f;
    [SerializeField, Range(0.01f, 2f)] private float noiseSize = 1f;
    [SerializeField, Range(0.001f, 100f)] private float brushZNear = 0.1f;
    [SerializeField, Range(0.001f, 100f)] private float brushZFar = 10f;

    [Header("Strokes:")]
    [SerializeField, Range(1, 10)] private int maxSimulatenousStrokes = 5;
    [SerializeField, Range(0f, 1f)] private float strokeDuration = 0.1f;
    [SerializeField, Range(1, 40)] private int maxDrawsPerStroke = 10;
    [SerializeField, Range(1, 10)] private int maxDrawsPerFrame = 2;


    new private Camera camera = null;
    private Plane surfacePlane = default;

    private PaintingManager paintingManager = null;
    private Camera paintingCamera = null;
    private RenderTexture paintingRT = null;


    [SerializeField] private Material testBrushMaterial = null;
    [SerializeField] private Texture test_TExture = null;
    private TextureBrush testBrush = null;


    [SerializeField] private List<HubObject> colorObjects = new();
    [SerializeField] private List<GameObject> selectionObjects = new();

    [SerializeField] private AudioSource audioSource = null;
    [SerializeField] private List<AudioClip> brushAudioClips = new();
    [SerializeField] private List<AudioClip> paintAudioClips = new();

    private RTHandle drawingColorsRTH = null;


    // DEBUG
    private float mouseTraveledDistance = 0f;

    private float lastDrawTime = float.MinValue;

    private bool directionToggle = false;

    private LinkedList<(Tween tween, BrushStroke brushStroke)> brushStrokes = new();

    private bool isActive = false;
    public void SetActive(bool value)
    {
        isActive = value;

        if (paintingManager != null)
        { paintingManager.Player.TEMP_PlayerMove.SetActive(isActive); }
    }

    private bool isErasing = false;
    private int colorIndex = 0;

    private List<Color> currentColors = new();


    #region Lifecycle ============================================================================= Lifecycle

    private void Awake()
    {
        SetActive(false);

        camera = Camera.main;

        testBrush = new(test_TExture)
        {
            IsJittered = true,
            IsErasing = isErasing,
            ColorIndex = colorIndex
        };
    }

    private void Start()
    {
        UpdateDrawingColorsTexture();

        for (int i = 0; i < painting.Colors.Count; i++)
        {
            Color color = painting.Colors[i];

            colorObjects[i].SetTint(color);
            selectionObjects[i].SetActive(i == colorIndex);
        }
    }

    private void OnDestroy()
    {
        drawingColorsRTH?.Release();
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (currentColors.Count != painting.Colors.Count)
        {
            UpdateDrawingColorsTexture();
        }
        else
        {
            for (int i = 0; i < painting.Colors.Count; i++)
            {
                if (currentColors[i] != painting.Colors[i])
                {
                    UpdateDrawingColorsTexture();
                    break;
                }
            }
        }
#endif

        if (Game.Instance.IsPaused) { return; }

        if (State != LoadingState.Loaded) { return; }

        if (paintingManager == null || paintingCamera == null) { return; }

        paintingCamera.Render();
        
        if (!isActive) { return; }
        
        UpdatePlane();

        void UpdateSelectionVisual()
        {
            if (isErasing)
            {
                for (int i = 0; i < selectionObjects.Count - 1; i++)
                { selectionObjects[i].SetActive(false); }

                selectionObjects[^1].SetActive(true);
            }
            else
            {
                for (int i = 0; i < selectionObjects.Count; i++)
                { selectionObjects[i].SetActive(i == colorIndex); }
            }
        }

        void OnColorIndexChanged()
        {
            isErasing = colorIndex == 4;
            UpdateSelectionVisual();
        }

        var node = brushStrokes.First;
        while (node != null)
        {
            var nextNode = node.Next;
            var (tween, brushStroke) = node.Value;

            if (!brushStroke.TryDrawUpToTargetAlpha(maxDrawsPerFrame))
            {
                if (tween != null && tween.IsActive()) { tween.Kill(); }
                brushStrokes.Remove(node);
            }

            node = nextNode;
        }

        if (!TryGetNormalizedSurfacePosition(out Vector2 position)) { return; }

        int previousColorIndex = colorIndex;

        if (Input.mouseScrollDelta.y < 0f) { colorIndex = Mathf.Clamp(colorIndex + 1, 0, 4); OnColorIndexChanged(); }
        if (Input.mouseScrollDelta.y > 0f) { colorIndex = Mathf.Clamp(colorIndex - 1, 0, 4); OnColorIndexChanged(); }
        else if (Input.GetKeyDown(KeyCode.Alpha1)) { colorIndex = 0; OnColorIndexChanged(); }
        else if (Input.GetKeyDown(KeyCode.Alpha2)) { colorIndex = 1; OnColorIndexChanged(); }
        else if (Input.GetKeyDown(KeyCode.Alpha3)) { colorIndex = 2; OnColorIndexChanged(); }
        else if (Input.GetKeyDown(KeyCode.Alpha4)) { colorIndex = 3; OnColorIndexChanged(); }
        else if (Input.GetKeyDown(KeyCode.Alpha5)) { colorIndex = 4; OnColorIndexChanged(); }

        if (colorIndex != previousColorIndex && !isErasing)
        { audioSource.PlayOneShot(paintAudioClips[Random.Range(0, paintAudioClips.Count)], Random.Range(0.95f, 1.05f)); }

        if (Input.GetMouseButtonDown(0) || (Input.GetMouseButton(0) && Time.time > (lastDrawTime + (strokeDuration * 0.9f))))
        {
            audioSource.PlayOneShot(brushAudioClips[Random.Range(0, brushAudioClips.Count)], Random.Range(0.95f, 1.05f));

            if (brushStrokes.Count > maxSimulatenousStrokes)
            {
                (Tween oldTween, _) = brushStrokes.Last();
                if (oldTween != null && oldTween.IsActive()) { oldTween.Kill(); }
                brushStrokes.RemoveLast();
            }

            directionToggle = !directionToggle;
            BrushStroke brushStroke = new(isErasing, colorIndex, maxDrawsPerStroke, directionToggle, this, paintingCamera, testBrush);

            Tween tween = DOVirtual.Float(0f, 1f, strokeDuration, alpha => { brushStroke.SetTargetAlpha(alpha); })
                .OnComplete(() => brushStroke.SetTargetAlpha(1f));

            brushStrokes.AddFirst((tween, brushStroke));

            lastDrawTime = Time.time;
        }
    }

    #endregion Lifecycle


    #region Loading / Unloading =================================================================== Loading / Unloading

    public async UniTask<bool> TryLoadAsync()
    {
        if (State != LoadingState.Unloaded) { return false; }

        State = LoadingState.Loading;

        TryResult<PaintingManager> managerLoading = await PaintingManager.TryLoadForAsync(this);
        
        if (managerLoading.Success && managerLoading.Value.Player != null)
        {
            paintingManager = managerLoading.Value;

            // DEBUG
            paintingManager.Player.TEMP_PlayerMove.SetActive(false);

            paintingCamera = paintingManager.Player.TEMP_PlayerMove.Camera;
            paintingCamera.enabled = false;
            paintingCamera.clearFlags = CameraClearFlags.SolidColor;
            paintingCamera.backgroundColor = new(1f, 1f, 0f);
            paintingCamera.cullingMask = LayerMask.GetMask("DrawableObjects");
            paintingCamera.TrySetRenderer(paintingRenderer);

            Shader.SetGlobalTexture("_G_DrawingColors", drawingColorsRTH);

            UpdatePaintingRenderTexture();
        }
        else
        {
            State = LoadingState.Unloaded;
            return false;
        }

        State = LoadingState.Loaded;

        return managerLoading.Success;
    }

    public async UniTask<bool> TryUnloadAsync()
    {
        if (State != LoadingState.Loaded) { return false; }

        State = LoadingState.Unloading;
        await paintingManager.UnloadAsync();
        paintingManager = null;
        State = LoadingState.Unloaded;

        return true;
    }

    #endregion Loading / Unloading


    #region Interaction =========================================================================== Interaction



    #endregion Interaction


    #region Private Methods ======================================================================= Private Methods

    private void CleanUpRenderTexture(ref RenderTexture texture)
    { if (texture != null) { texture.Release(); texture = null; } }

    private void UpdatePaintingRenderTexture()
    {
        if (paintingManager == null || paintingCamera == null) { return; }

        CleanUpRenderTexture(ref paintingRT);

        paintingRT = new(Mathf.CeilToInt(Screen.height * aspectRatio), Screen.height, 16, RenderTextureFormat.ARGB32)
        { filterMode = FilterMode.Point, antiAliasing = 1 };
        paintingRT.Create();

        paintingCamera.targetTexture = paintingRT;
        canvasRenderer.sharedMaterial.SetTexture("_PaintingTexture", paintingRT);
    }

    private bool TryGetNormalizedSurfacePosition(out Vector2 position, bool withinBounds = true)
    {
        position = Vector2.zero;
        Ray mouseRay = camera.ScreenPointToRay(Input.mousePosition);

        if (!surfacePlane.Raycast(mouseRay, out float distance)) { return false; }

        Vector3 localPosition = transform.InverseTransformPoint(mouseRay.GetPoint(distance));

        position = surfaceAxis switch
        {
            Axis3D.x => new(localPosition.z + 0.5f, localPosition.y + 0.5f),
            Axis3D.y => new(localPosition.z + 0.5f, 0.5f - localPosition.x),
            Axis3D.z => new(localPosition.x + 0.5f, localPosition.y + 0.5f),
            _ => Vector3.zero,
        };

        if (withinBounds && (position.x < 0f || position.x > 1f || position.y < 0f || position.y > 1f)) { return false; }

        return true;
    }

    private (Vector3 normal, Vector3 center) GetSurfacePlaneInformation()
    {
        Vector3 normal = surfaceAxis switch
        {
            Axis3D.x => transform.right,
            Axis3D.y => transform.up,
            Axis3D.z => transform.forward,
            _ => Vector3.one,
        };

        Vector3 center = surfaceAxis switch
        {
            Axis3D.x => transform.TransformPoint(Vector3.right / 2f),
            Axis3D.y => transform.TransformPoint(Vector3.up / 2f),
            Axis3D.z => transform.TransformPoint(Vector3.forward / 2f),
            _ => Vector3.zero,
        };

        return (normal, center);
    }

    private void UpdatePlane()
    {
        var (normal, center) = GetSurfacePlaneInformation();
        surfacePlane = new(normal, center);
    }

    private void UpdateDrawingColorsTexture()
    {
        drawingColorsRTH?.Release();

        drawingColorsRTH = RTHandles.Alloc(DrawingSystem.Instance.MaxColors * 2, 2,
            name: "DrawingColorsTexture", colorFormat: GraphicsFormat.R8G8B8A8_UNorm,
            autoGenerateMips: false, useMipMap: false);

        Texture2D tempTexture = new(DrawingSystem.Instance.MaxColors * 2, 2);
        
        for (int i = 0; i < DrawingSystem.Instance.MaxColors; i++)
        {
            Color color = i < painting.Colors.Count ? painting.Colors[i] : Color.clear;

            for (int xOff = 0; xOff < 2; xOff++)
            {
                for (int yOff = 0; yOff < 2; yOff++)
                { tempTexture.SetPixel(i * 2 + xOff, yOff, color); }
            }
        }

        currentColors.Clear();
        for (int i = 0; i < painting.Colors.Count; i++)
        { currentColors.Add(painting.Colors[i]); }

        tempTexture.Apply();
        Graphics.Blit(tempTexture, drawingColorsRTH);
        Destroy(tempTexture);
    }

    #endregion Private Methods


#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        var (_, center) = GetSurfacePlaneInformation();

        Vector3 scale = transform.lossyScale;
        Vector3 size = surfaceAxis switch
        {
            Axis3D.x => new(0, scale.y, scale.z),
            Axis3D.y => new(scale.x, 0, scale.z),
            Axis3D.z => new(scale.x, scale.y, 0),
            _ => Vector3.zero,
        };

        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, size);
        Gizmos.matrix = oldMatrix;
        Gizmos.DrawLine(center, center + (center - transform.position).normalized * 0.05f);
    }

#endif
}


#if UNITY_EDITOR

[UnityEditor.CustomEditor(typeof(PaintableCanvas))]
public class PaintableCanvasEditor : AdvancedEditor { }

#endif