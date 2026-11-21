
#if UNITY_EDITOR
using UnityEditor;
using static ObjectDrawing.DrawingSystem;
#endif

using Cysharp.Threading.Tasks;

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using Utility;
using UtilityEditor;
using UtilityEnum;
using UtilityExtensions;


namespace ObjectDrawing
{
    public enum DrawingGraphicsFormat
    {
        R8 = GraphicsFormat.R8_UNorm,
        R8G8 = GraphicsFormat.R8G8_UNorm,
        R8G8B8A8 = GraphicsFormat.R8G8B8A8_UNorm,
        R16 = GraphicsFormat.R16_UNorm,
        R16G16 = GraphicsFormat.R16G16_UNorm,
        R16G16B16A16 = GraphicsFormat.R16G16B16A16_UNorm,
    }

    public enum DrawingDepthFormat
    {
        D16 = GraphicsFormat.D16_UNorm,
        D24 = GraphicsFormat.D24_UNorm,
        D32 = GraphicsFormat.D32_SFloat,
    }


    /// <summary>
    /// <br/>   A singleton system for drawing onto each tracked <see cref="DrawableObject"/>.
    /// <br/>   Drawable objects are tracked in a list of <see cref="DrawingSlice"/>.
    /// <br/>   Communicates with <see cref="ObjectDrawingFeature"/> to draw onto objects.
    /// <br/>   Drawing ends up on a global texture: "_DrawingArrayTexture".
    /// </summary>
    /// <remarks>
    /// <br/>   <see cref="ScriptableObjectSingleton{TDerived}"/> initialization args:
    /// <br/>   • <see cref="int"/> maxColors
    /// </remarks>
    [CreateAssetMenu(fileName = "DrawingSystem", menuName = "ObjectDrawing/DrawingSystem")]
    public class DrawingSystem : ScriptableObjectSingleton<DrawingSystem>
    {
#if UNITY_EDITOR
        public static string AssetPath { get; private set; } = "";
        public static string ScriptPath { get; private set; } = "";

        public static DrawingSystem Instance
        {
            get
            {
                if (instance == null)
                {
                    string[] guids = AssetDatabase.FindAssets($"t:{typeof(DrawingSystem).Name}");

                    foreach (string guid in guids)
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        instance = AssetDatabase.LoadAssetAtPath<DrawingSystem>(assetPath);
                        if (instance == null) { continue; }

                        AssetPath = System.IO.Path.GetDirectoryName(assetPath);
                        var script = MonoScript.FromScriptableObject(instance);
                        string scriptPath = AssetDatabase.GetAssetPath(script);
                        ScriptPath = System.IO.Path.GetDirectoryName(scriptPath);
                        break;
                    }
                }

                return instance;
            }
        }
#else
        public static DrawingSystem Instance => instance;
#endif

        private static DrawingSystem instance = null;


        public const DrawingDepthFormat DEPTH_FORMAT = DrawingDepthFormat.D32;
        public const int DEPTH_BITS = 32;

        public const DrawingGraphicsFormat DRAWING_ARRAY_FORMAT = DrawingGraphicsFormat.R8;
        public const DrawingGraphicsFormat OPERATION_DATA_FORMAT = DrawingGraphicsFormat.R16G16B16A16;

#if !DRAWING_QUALITY_OPTION
        public const bool USE_8BIT_FORMAT = true;

        // Drawing Array: R8 - 8
        public const int DRAWING_ARRAY_BITS = 8;

        // Operation Data: R16G16B16A16 - 64
        public const int OPERATION_DATA_BITS = 64;
        public const string OPERATION_DATA_BIT_USAGE =
            "R: [16: UV horizontal]\n" +
            "G: [16: UV vertical]\n" +
            "B: [8: Nothing] + [8: Brush info]\n" +
            "A: [10: Buffer index] + [6: Slice index]";
        public const int BRUSH_INFO_BITS = 8;
        public const int BUFFER_INDEX_BITS = 11;
        public const int SLICE_INDEX_BITS = 5;
#else
        public const bool USE_8BIT_FORMAT = false;

        // Drawing Array: R16 - 16
        public const int DRAWING_ARRAY_BITS = 16;
        
        // Operation Data: R16G16B16A16 - 64
        public const int OPERATION_DATA_BITS = 64;
        public const string OPERATION_DATA_BIT_USAGE =
            "R: [16: UV horizontal]\n" +
            "G: [16: UV vertical]\n" +
            "B: [16: Brush info]\n" +
            "A: [10: Buffer index] + [6: Slice index]";
        public const int BRUSH_INFO_BITS = 16;
        public const int BUFFER_INDEX_BITS = 10;
        public const int SLICE_INDEX_BITS = 8;
#endif

        public const int MAX_CONCURRENT_OBJECTS = 1 << BUFFER_INDEX_BITS;

        public const int MIN_ALLOWED_TEXTURE_SIZE = 16;
        public const int MAX_ALLOWED_TEXTURE_SIZE = 4096;

        public int MaxConcurrentDraws => maxConcurrentDraws;
        public int SliceCount => sliceCount;
        public int MaxTextureSize => maxTextureSize;
        public int MinTextureSize => minTextureSize;
        public int BrushSize => brushSize;

        public int MaxColors { get; private set; } = 0;
        
        internal Camera BrushCamera { get; private set; } = null;

        internal ComputeShader ObjectDrawingCompute => objectDrawingCompute;
        internal ComputeShader ObjectCleaningCompute => objectCleaningCompute;
        internal ComputeShader TileGenerationCompute => tileGenerationCompute;
        internal ComputeShader ColorCalculatingCompute => colorCalculatingCompute;

        /// <summary> Material that draws UV, buffer index and slice index per drawing operation. </summary>
        internal Material OperationDataMaterial => operationDataMaterial;

        /// <summary> Same as <see cref="OperationDataMaterial"/> but only sees previous paint. </summary>
        internal Material PaintOnlyOperationDataMaterial => paintOnlyOperationDataMaterial;

        internal Material BaseBrushMaterial => brushBaseMaterial;

        internal RTHandle DepthRTH => depthRTH;
        internal RTHandle OperationDataRTH => operationDataRTH;
        internal RTHandle DrawingArrayRTH => drawingArrayRTH;



        protected override bool RequiresPersistentContainer => true;

        [SerializeField] private UniversalRendererData objectDrawingRenderer = null;

        [Header("Drawing Array:")]
        [SerializeField] private ComputeShader objectDrawingCompute = null;
        [SerializeField] private ComputeShader objectCleaningCompute = null;
        [SerializeField] private ComputeShader tileGenerationCompute = null;
        [SerializeField] private ComputeShader colorCalculatingCompute = null;
        [SerializeField, POT(MIN_ALLOWED_TEXTURE_SIZE, MAX_ALLOWED_TEXTURE_SIZE)] private int maxTextureSize = MAX_ALLOWED_TEXTURE_SIZE;
        [SerializeField, POT(MIN_ALLOWED_TEXTURE_SIZE, MAX_ALLOWED_TEXTURE_SIZE)] private int minTextureSize = MIN_ALLOWED_TEXTURE_SIZE;
        [SerializeField, Range(1, 1 << SLICE_INDEX_BITS)] private int sliceCount = 1;
        [SerializeField, Range(1, 100)] private int maxConcurrentDraws = 1;

        [Header("Brush:")]
        [Tooltip("Material that contains brush data written into blue channel.")]
        [SerializeField] private Material brushBaseMaterial = null;
        [SerializeField] private Material operationDataMaterial = null;
        [SerializeField] private Material paintOnlyOperationDataMaterial = null;
        [SerializeField, POT(1, 4096)] private int brushSize = 2048;

        private ObjectDrawingFeature objectDrawingFeature = null;
        private ObjectDrawingPass objectDrawingPass = null;

        private RTHandle dummyRTH = null;
        private RTHandle depthRTH = null;
        private RTHandle operationDataRTH = null;
        private RTHandle drawingArrayRTH = null;

        private Stack<int> freeSliceIndices = new();

        private Queue<DrawOperation> drawOperations = new();
        private HashSet<DrawableObject> objectsToRegister = new();
        private HashSet<DrawableObject> objectsToCleanUp = new();
        private HashSet<DrawingSlice> slicesToCleanUp = new();


        #region Lifecycle ============================================================================================= Lifecycle

        protected override TypeAndName[] ExpectedArgs { get; } = new TypeAndName[]
        {
            TypeAndName.For<int>("MaxColors"),
        };

        protected override async UniTask<DrawingSystem> InitializeAsync(params object[] args)
        {
            if (objectDrawingRenderer == null)
            { throw new ControlledException("Object drawing renderer not set!"); }

            if (!objectDrawingRenderer.rendererFeatures.TryGetFirstOfType(out objectDrawingFeature))
            { throw new ControlledException("Object drawing feature missing!"); }

            if (objectDrawingCompute == null)
            { throw new ControlledException("Object drawing compute shader not set!"); }

            if (objectCleaningCompute == null)
            { throw new ControlledException("Object cleaning compute shader not set!"); }

            if (operationDataMaterial == null || paintOnlyOperationDataMaterial == null)
            { throw new ControlledException("Data materials couldn't be found!"); }

            instance = this;

            MaxColors = (int)args[0];

            freeSliceIndices = new(Enumerable.Range(0, sliceCount).Reverse());

            CreateDrawingArrayTexture();

            CreateBrushCamera();
            CreateBrushCameraTextures();

            await UniTask.WaitUntil(() => objectDrawingFeature.ObjectDrawingPass != null);
            objectDrawingPass = objectDrawingFeature.ObjectDrawingPass;
            objectDrawingPass.Initialize();

            return this;
        }

        protected override void OnInitializationFailed() => FullCleanUp();

        private void OnDestroy() => FullCleanUp();

        #endregion Lifecycle


        #region Drawing Methods ======================================================================================= Drawing Methods

        internal Queue<DrawOperation> GetDrawOperations()
        {
            Queue<DrawOperation> copy = new(drawOperations);
            drawOperations.Clear();
            return copy;
        }

        internal void OnDrawOperationsFinished()
        {
            drawOperations.Clear();
        }

        public static void Draw(DrawOperation drawOperation)
        {
            if (Instance == null || drawOperation.Brush == null) { return; }

            if (Instance.drawOperations.Count >= Instance.maxConcurrentDraws)
            { Util.LogError<DrawingSystem>("Max draw operations reached!"); return; }

            Instance.drawOperations.Enqueue(drawOperation);
        }

        public static void DrawCursor(Brush brush, Camera referenceCamera, Vector2 screenPosition, float height, float rotation,
            float zNear = 0.1f, float zFar = 1000f)
        {
            if (brush == null || referenceCamera == null) { return; }

            referenceCamera.transform.GetPositionAndRotation(out Vector3 camPosition, out Quaternion camRotation);
            DrawCursor(brush, referenceCamera, camPosition, camRotation, screenPosition, height, rotation, zNear, zFar);
        }

        public static void DrawCursor(Brush brush, Camera referenceCamera, Vector3 camPosition, Quaternion camRotation, Vector2 screenPosition, float height, float rotation,
            float zNear = 0.1f, float zFar = 1000f)
        {
            if (brush == null || referenceCamera == null) { return; }

            Vector2 center = new(screenPosition.x * 2f - 1f, screenPosition.y * 2f - 1f);

            Matrix4x4 T1 = Matrix4x4.Translate(new Vector3(-center.x, -center.y, 0));
            Matrix4x4 S = Matrix4x4.Scale(new Vector3(1f / height, 1f / height, 1f));
            Matrix4x4 R = Matrix4x4.Rotate(Quaternion.Euler(0, 0, rotation));
            Matrix4x4 CropMatrix = R * S * T1;

            if (referenceCamera.orthographic)
            {
                float aspect = referenceCamera.aspect;
                float size = referenceCamera.orthographicSize;
                Matrix4x4 projectionMatrix = CropMatrix * Matrix4x4.Ortho(-size * aspect, size * aspect, -size, size, zNear, zFar);
                Draw(new(brush, camPosition, camRotation, projectionMatrix, isOrthographic: true, zNear, zFar));
            }
            else
            {
                float fov = referenceCamera.fieldOfView;
                float aspect = referenceCamera.aspect;
                Matrix4x4 projectionMatrix = CropMatrix * Matrix4x4.Perspective(fov, aspect, zNear, zFar);
                Draw(new(brush, camPosition, camRotation, projectionMatrix, isOrthographic: false, zNear, zFar));
            }
        }

        public static void DrawOrtho(Brush brush, Vector3 position, Quaternion rotation, float size, float zNear = 0.1f, float zFar = 1000f)
        { Draw(DrawOperation.Orthographic(brush, position, rotation, size, zNear, zFar)); }

        public static void DrawPerspective(Brush brush, Vector3 position, Quaternion rotation, float fieldOfView, float zNear = 0.1f, float zFar = 1000f)
        { Draw(DrawOperation.Perspective(brush, position, rotation, fieldOfView, zNear, zFar)); }

        public static void DrawDecal(Brush brush, Vector3 position, Quaternion rotation, float startScale, float endScale, float distance)
        {
            // TODO: Implement decal drawing
        }

        #endregion Drawing Methods


        #region Slice Management Methods ============================================================================== Slice Management Methods

        internal bool TryReserveSlice(out int sliceIndex)
        {
            sliceIndex = -1;

            if (freeSliceIndices.Count == 0) { return false; }

            sliceIndex = freeSliceIndices.Pop();

            return true;
        }

        internal List<DrawableObject> GetObjectsToRegister() => new(objectsToRegister);

        internal List<DrawableObject> GetObjectsToCleanUp() => new(objectsToCleanUp);

        internal List<DrawingSlice> GetSlicesToCleanUp() => new(slicesToCleanUp);

        internal void OnCleanUpsAndRegistrationsFinished()
        {
            objectsToRegister.Clear();
            objectsToCleanUp.Clear();

            foreach (DrawingSlice drawingSlice in slicesToCleanUp)
            { freeSliceIndices.Push(drawingSlice.Index); }

            slicesToCleanUp.Clear();
        }

        internal void AddObjectToBeRegistered(DrawableObject drawableObject)
        { objectsToRegister.Add(drawableObject); }

        internal void AddObjectToBeCleanedUp(DrawableObject drawableObject)
        { objectsToCleanUp.Add(drawableObject); }

        internal void AddSliceToBeCleanedUp(DrawingSlice drawingSlice)
        { slicesToCleanUp.Add(drawingSlice); }

        internal void FreeUpSlice(DrawingSlice drawingSlice)
        { freeSliceIndices.Push(drawingSlice.Index); }

        #endregion Slice Management Methods


        #region Public Methods ======================================================================================== Public Methods

        public bool TryGetColorAmountsFor(DrawableObject drawableObject, uint[] colorAmounts)
        { return objectDrawingPass?.TryGetColorAmounts(drawableObject, colorAmounts) ?? false; }

        #endregion Public Methods


        #region Internal Methods ====================================================================================== Internal Methods

        internal static (int oneChannel, int total) GetFormatBitCount(DrawingGraphicsFormat format)
        {
            return format switch
            {
                DrawingGraphicsFormat.R8 => (8, 8),
                DrawingGraphicsFormat.R8G8 => (8, 16),
                DrawingGraphicsFormat.R8G8B8A8 => (8, 32),
                DrawingGraphicsFormat.R16 => (16, 16),
                DrawingGraphicsFormat.R16G16 => (16, 32),
                DrawingGraphicsFormat.R16G16B16A16 => (16, 64),
                _ => (0, 0),
            };
        }

        #endregion Internal Methods


        #region Private Methods ======================================================================================= Private Methods

        private void CreateDrawingArrayTexture()
        {
            CleanUpRenderTextureHandle(ref drawingArrayRTH);

            drawingArrayRTH = RTHandles.Alloc(maxTextureSize, maxTextureSize,
                name: "DrawingArrayTexture", colorFormat: (GraphicsFormat)DRAWING_ARRAY_FORMAT,
                enableRandomWrite: true, dimension: TextureDimension.Tex2DArray, slices: sliceCount,
                autoGenerateMips: false, useMipMap: true);

            Shader.SetGlobalFloat("_G_MaxSliceSize", maxTextureSize);
            Shader.SetGlobalTexture("_G_DrawingArrayTexture", drawingArrayRTH);
        }

        private void CreateBrushCamera()
        {
            // Used as an output texture for brush camera to prevent it from rendering normally
            dummyRTH = RTHandles.Alloc(1, 1, name: "DummyTexture", colorFormat: GraphicsFormat.D16_UNorm);

            GameObject brushCameraObject = new("BrushCamera");
            BrushCamera = brushCameraObject.AddComponent<Camera>();
            BrushCamera.cullingMask = LayerMask.GetMask("DrawableObjects");
            BrushCamera.targetTexture = dummyRTH;
            BrushCamera.TrySetRenderer(objectDrawingRenderer);
            brushCameraObject.transform.parent = persistentContainer.transform;

            // Consider removing dummy RT and rendering manually with:
            // BrushCamera.enabled = false;
            // BrushCamera.Render();
            // RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            //void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
            //{
            //    if (camera == BrushCamera)
            //    {
            //        var universalData = camera.GetUniversalAdditionalCameraData();
            //        universalData.scriptableRenderer.EnqueuePass(objectDrawingPass);
            //    }
            //}
        }

        private void CreateBrushCameraTextures()
        {
            CleanUpRenderTextureHandle(ref depthRTH);
            CleanUpRenderTextureHandle(ref operationDataRTH);

            depthRTH = RTHandles.Alloc(brushSize, brushSize,
                name: "DepthBuffer", colorFormat: (GraphicsFormat)DEPTH_FORMAT,
                autoGenerateMips: false, useMipMap: false);

            operationDataRTH = RTHandles.Alloc(brushSize, brushSize,
                name: "OperationDataTexture", colorFormat: (GraphicsFormat)OPERATION_DATA_FORMAT,
                autoGenerateMips: false, useMipMap: false);
        }

        private void FullCleanUp()
        {
            Destroy(BrushCamera);
            CleanUpRenderTextureHandle(ref dummyRTH);
            CleanUpRenderTextureHandle(ref depthRTH);
            CleanUpRenderTextureHandle(ref operationDataRTH);
            CleanUpRenderTextureHandle(ref drawingArrayRTH);
        }

        private void CleanUpRenderTextureHandle(ref RTHandle textureHandle)
        { if (textureHandle != null) { textureHandle.Release(); textureHandle = null; } }

        #endregion Private Methods
    }


#if UNITY_EDITOR

    [CustomEditor(typeof(DrawingSystem))]
    internal class DrawingSystemEditor : AdvancedEditor
    {
        private DrawingSystem drawingSystem = null;

        private SerializedProperty objectDrawingRendererProperty = null;
        private SerializedProperty maxSliceSizeProperty = null;
        private SerializedProperty minSliceSizeProperty = null;
        private SerializedProperty sliceCountProperty = null;
        private SerializedProperty brushSizeProperty = null;


        protected override void OnEnableInternal()
        {
            drawingSystem = (DrawingSystem)target;
            objectDrawingRendererProperty = serializedObject.FindProperty("objectDrawingRenderer");
            maxSliceSizeProperty = serializedObject.FindProperty("maxTextureSize");
            minSliceSizeProperty = serializedObject.FindProperty("minTextureSize");
            sliceCountProperty = serializedObject.FindProperty("sliceCount");
            brushSizeProperty = serializedObject.FindProperty("brushSize");
        }

        protected override void OnPropertyFieldEnd(ModifiedProperty modifiedProperty)
        {
            if (modifiedProperty.Property.propertyPath == objectDrawingRendererProperty.propertyPath &&
                objectDrawingRendererProperty.objectReferenceValue == null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Assign renderer before proceeding!", centeredLabel);
                StopDrawing();
            }
            else if (modifiedProperty.Property.propertyPath == minSliceSizeProperty.propertyPath
                && maxSliceSizeProperty.intValue < minSliceSizeProperty.intValue)
            {
                EditorGUILayout.LabelField("Min slice size has to be smaller than max!", centeredLabel);
                EditorGUILayout.Space();
            }
        }

        protected override void OnInspectorGUIEnd()
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Information:", EditorStyles.boldLabel);

            double drawingArrayMemory = GetDrawingArrayMemory();
            double brushTexturesMemory = GetBrushTexturesMemory();
            double totalMemory = drawingArrayMemory + brushTexturesMemory;

            EditorGUILayout.LabelField("Drawing array:\t" + GetFormated(drawingArrayMemory));
            EditorGUILayout.LabelField("Brush textures:\t" + GetFormated(brushTexturesMemory));
            EditorGUILayout.LabelField("Total:\t\t\t" + GetFormated(totalMemory));
        }

        private double GetDrawingArrayMemory()
        {
            double size = maxSliceSizeProperty.intValue;
            double sliceCount = sliceCountProperty.intValue;
            double memory = 0;

            memory += (DRAWING_ARRAY_BITS * size * size * sliceCount) / 8.0;

            // Account for mips
            memory *= 1.0 + (1.0 / 3.0);

            // Account for read / write overhead
            memory *= 1.15;

            return memory;
        }

        private double GetBrushTexturesMemory()
        {
            int size = brushSizeProperty.intValue;
            double memory = 0;

            memory += (DEPTH_BITS * size * size) / 8.0;
            memory += (OPERATION_DATA_BITS * size * size) / 8.0;

            return memory;
        }

        private string GetFormated(double memory) => memory.SIFormat("B", baseUnitHasDecimals: false);
    }

#endif
}
