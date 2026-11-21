
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

using System.Linq;
using System.Runtime.InteropServices;

using UnityEngine;

using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

using Utility;


namespace ObjectDrawing
{
    public class ObjectDrawingFeature : ScriptableRendererFeature
    {
        internal ObjectDrawingPass ObjectDrawingPass { get; private set; } = null;

        public override void Create() => ObjectDrawingPass = new();

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!Application.isPlaying) { return; }

            DrawingSystem drawingSystem = ObjectDrawingPass.DrawingSystem;

            if (drawingSystem == null || drawingSystem.BrushCamera == null ||
                renderingData.cameraData.camera != drawingSystem.BrushCamera) { return; }

            renderer.EnqueuePass(ObjectDrawingPass);
        }

        protected override void Dispose(bool disposing) // TODO: [IMPORTANT_CHANGE]
        {
            ObjectDrawingPass?.CleanUp();
            base.Dispose(disposing);
        }
    }


    public class ObjectDrawingPass : ScriptableRenderPass
    {
        private class EmptyPassData { }

        private class CleanUpPassData
        {
            public TextureHandle DrawingArrayTextureH = default;
            public BufferHandle FullCleanBufferH = default;
            public BufferHandle MacroCleanBufferH = default;
            public BufferHandle MicroCleanBufferH = default;
            public int FullCount = 0;
            public int MacroCount = 0;
            public int MicroCount = 0;
        }

        private class OperationProcessingPassData
        {
            public DrawOperation Operation = default;
            public RendererListHandle RendererList = default;
        }

        private class BrushDrawingPassData
        {
            public DrawOperation Operation = default;
            public TextureHandle DepthTextureH = default;
        }

        private class ObjectDrawingPassData
        {
            public BufferHandle DirtyObjectBufferH = default;
            public TextureHandle OperationDataTextureH = default;
            public TextureHandle DrawingArrayTextureH = default;
        }

        private class DirtyTileGeneratingPassData
        {
            public BufferHandle DirtyObjectBufferH = default;
            public BufferHandle DirtyTileBufferH = default;
            public BufferHandle ColorAmountBufferH = default;
            public BufferHandle ObjectDataBufferH = default;
        }

        private class ColorAmountCalculatingData
        {
            public BufferHandle CalculationArgsBufferH = default;
            public BufferHandle DirtyTileBufferH = default;
            public BufferHandle ColorAmountBufferH = default;
            public TextureHandle DrawingArrayTextureH = default;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ObjectData
        {
            public int SliceIndex;
            public int TextureIndex;
            public int TextureSize;
            public int XMin;
            public int YMin;

            public static ObjectData Default => new() { SliceIndex = -1, TextureIndex = -1 };
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CleanTileData
        {
            public int SliceIndex;
            public int XMin;
            public int YMin;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ObjectTileData
        {
            public int ObjectIndex;
            public int SliceIndex;
            public int XMin;
            public int YMin;
        }


        internal DrawingSystem DrawingSystem { get; private set; } = null;

        internal bool DrawingArrayTextureChanged { get; private set; } = false;


        private readonly int brushDepthTextureID = Shader.PropertyToID("_Brush_DepthTexture"); // TODO: [IMPORTANT_CHANGE]
        private readonly int brushIsOrthographicID = Shader.PropertyToID("_Brush_IsOrthographic");
        private readonly int brushZNearID = Shader.PropertyToID("_Brush_ZNear");
        private readonly int brushZFarID = Shader.PropertyToID("_Brush_ZFar");
        private readonly int brushCameraPositionID = Shader.PropertyToID("_Brush_CameraPosition");
        private readonly int brushInverseViewProjID = Shader.PropertyToID("_Brush_InverseViewProj");

        private readonly int fullCleanBufferID = Shader.PropertyToID("FullCleanBuffer");
        private readonly int macroCleanBufferID = Shader.PropertyToID("MacroCleanBuffer");
        private readonly int microCleanBufferID = Shader.PropertyToID("MicroCleanBuffer");

        private readonly int dirtyObjectBufferID = Shader.PropertyToID("DirtyObjectBuffer");
        private readonly int dirtyTileBufferID = Shader.PropertyToID("DirtyTileBuffer");

        private readonly int maxObjectsID = Shader.PropertyToID("MaxObjects");
        private readonly int maxColorsID = Shader.PropertyToID("MaxColors");
        private readonly int microTileSizeID = Shader.PropertyToID("MicroTileSize");
        private readonly int sliceCountID = Shader.PropertyToID("SliceCount");

        private readonly int colorAmountBufferID = Shader.PropertyToID("ColorAmountBuffer");
        private readonly int objectDataBufferID = Shader.PropertyToID("ObjectDataBuffer");
        private readonly int operationDataTextureID = Shader.PropertyToID("OperationDataTexture");
        private readonly int drawingArrayTextureID = Shader.PropertyToID("DrawingArrayTexture");

        private Material operationDataMaterial = null;
        private Material paintOnlyOperationDataMaterial = null;

        private ComputeShader objectCleaningCompute = null;
        private int fullCleanKernel = 0;
        private int macroCleanKernel = 0;
        private int microCleanKernel = 0;
        
        private ComputeShader objectDrawingCompute = null;
        private int drawBrushKernel = 0;

        private ComputeShader tileGenerationCompute = null;
        private int generateTilesKernel = 0;

        private ComputeShader colorCalculatingCompute = null;
        private int calculateColorsKernel = 0;

        private int maxColors = 0;
        private int maxObjects = 0;
        private int maxTextureSize = 0;
        private int minTextureSize = 0;
        private int sliceCount = 0;
        private int brushSize = 0;
        private int macroTileSize = 0;
        private const int microTileSize = 16;
        
        private Stack<int> freeObjectDataBufferIndices = new();

        private GraphicsBuffer fullCleanBuffer = null;
        private GraphicsBuffer macroCleanBuffer = null;
        private GraphicsBuffer microCleanBuffer = null;
        private List<int> fullCleanList = new();
        private List<CleanTileData> macroCleanList = new();
        private List<CleanTileData> microCleanList = new();

        private GraphicsBuffer calculationArgsBuffer = null;
        private GraphicsBuffer dirtyObjectBuffer = null;
        private GraphicsBuffer dirtyTileBuffer = null;

        private GraphicsBuffer colorAmountBuffer = null;
        private GraphicsBuffer objectDataBuffer = null;
        private uint[] colorAmountArray = null;
        private ObjectData[] objectDataArray = null;

        private volatile bool colorsChanged = false;


        #region Lifecycle =================================================================================== Lifecycle

        internal ObjectDrawingPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
        }

        internal void Initialize()
        {
            CleanUp();
            
            DrawingSystem = DrawingSystem.Instance;

            maxColors = DrawingSystem.MaxColors;
            maxObjects = DrawingSystem.MAX_CONCURRENT_OBJECTS;

            maxTextureSize = DrawingSystem.MaxTextureSize;
            minTextureSize = DrawingSystem.MinTextureSize;
            brushSize = DrawingSystem.BrushSize;
            sliceCount = DrawingSystem.SliceCount;

            operationDataMaterial = DrawingSystem.OperationDataMaterial;
            paintOnlyOperationDataMaterial = DrawingSystem.PaintOnlyOperationDataMaterial;

            objectCleaningCompute = DrawingSystem.ObjectCleaningCompute;
            fullCleanKernel = objectCleaningCompute.FindKernel("CleanFull");
            macroCleanKernel = objectCleaningCompute.FindKernel("CleanMacro");
            microCleanKernel = objectCleaningCompute.FindKernel("CleanMicro");

            objectDrawingCompute = DrawingSystem.ObjectDrawingCompute;
            drawBrushKernel = objectDrawingCompute.FindKernel("DrawBrush");

            objectDrawingCompute = DrawingSystem.ObjectDrawingCompute;
            drawBrushKernel = objectDrawingCompute.FindKernel("DrawBrush");

            tileGenerationCompute = DrawingSystem.TileGenerationCompute;
            generateTilesKernel = tileGenerationCompute.FindKernel("GenerateTiles");

            colorCalculatingCompute = DrawingSystem.ColorCalculatingCompute;
            calculateColorsKernel = colorCalculatingCompute.FindKernel("CalculateColors");

            freeObjectDataBufferIndices = new(Enumerable.Range(0, maxObjects).Reverse());

            int maxAllowedSize = DrawingSystem.MAX_ALLOWED_TEXTURE_SIZE;
            int minAllowedSize = DrawingSystem.MIN_ALLOWED_TEXTURE_SIZE;
            int maxMicroTilesPerSlice = (maxTextureSize * maxTextureSize) / (microTileSize * microTileSize);

            macroTileSize = 1 << Mathf.CeilToInt((Mathf.Log(maxAllowedSize, 2) + Mathf.Log(minAllowedSize, 2)) / 2f);
            macroTileSize = Mathf.Max(macroTileSize, microTileSize);

            // Note: The min and max allowed texture sizes here are assumed to be 16 and 4096 respectively

            // 1. Buffer: Full slice cleaning
            // Contains indices for each slice to be cleaned up
            fullCleanBuffer = new(GraphicsBuffer.Target.Structured, DrawingSystem.SliceCount, sizeof(uint));
            fullCleanList = new(DrawingSystem.SliceCount);

            // 2. Buffer: Macro tile cleaning
            // Macro tiles [256 x 256] are used to clear larger regions: 2048, 1024, 512, 256
            // Note: The max texture size isn't accounted for here as the full slice clean up handles it
            int maxTileableSize = Mathf.Max(maxTextureSize / 2, minAllowedSize);
            if (maxTileableSize >= macroTileSize)
            {
                int maxMacroTiles = maxTileableSize / macroTileSize;
                maxMacroTiles *= maxMacroTiles;
                macroCleanBuffer = new(GraphicsBuffer.Target.Structured, maxObjects * maxMacroTiles, Marshal.SizeOf<CleanTileData>());
                macroCleanList = new(maxObjects * maxMacroTiles);
            }

            // 3. Buffer: Micro tile cleaning
            // Micro tiles [16 x 16] are used to clear smaller regions: 128, 64, 32, 16
            if (minTextureSize < macroTileSize)
            {
                int maxMicroTiles = (macroTileSize / 2) / microTileSize;
                maxMicroTiles *= maxMicroTiles;
                microCleanBuffer = new(GraphicsBuffer.Target.Structured, maxObjects * maxMicroTiles, Marshal.SizeOf<CleanTileData>());
                microCleanList = new(maxObjects * maxMicroTiles);
            }

            // 4. - 6. Buffers: Color amount calculation
            // New color amounts are calculated based on dirtied objects and the micro tiles they're split into
            calculationArgsBuffer = new(GraphicsBuffer.Target.IndirectArguments, 1, sizeof(uint) * 3);
            dirtyObjectBuffer = new(GraphicsBuffer.Target.Structured, maxObjects, sizeof(uint));
            dirtyTileBuffer = new(GraphicsBuffer.Target.Append, maxMicroTilesPerSlice * sliceCount, Marshal.SizeOf<ObjectTileData>());
            
            // Initialize args to prevent garbage data from causing GPU to hang
            calculationArgsBuffer.SetData(new uint[] { 0, 1, 1 }); // TODO: [IMPORTANT_CHANGE]

            // 7. Buffer: Color amount
            // Contains color information per drawable object
            colorAmountBuffer = new(GraphicsBuffer.Target.Structured, maxObjects * maxColors, sizeof(uint));
            colorAmountArray = new uint[maxObjects * maxColors];

            // 8. Buffer: Object data
            // Contains data per drawable object
            objectDataBuffer = new(GraphicsBuffer.Target.Structured, maxObjects, Marshal.SizeOf<ObjectData>());
            objectDataArray = Enumerable.Repeat(ObjectData.Default, maxObjects).ToArray();

            RenderPipelineManager.endContextRendering += OnEndContextRendering;
        }

        private bool bufferReadNeeded = false;
        private bool readingBuffer = false;

        private void OnEndContextRendering(ScriptableRenderContext ctx, List<Camera> cameras)
        {
            if (colorsChanged)
            {
                bufferReadNeeded = true;
                colorsChanged = false;
            }

            if (!readingBuffer && bufferReadNeeded)
            {
                bufferReadNeeded = false;
                ReadBufferAsync().Forget();
            }
        }

        private async UniTask ReadBufferAsync()
        {
            if (colorAmountBuffer == null) { return; }

            try
            {
                readingBuffer = true;
                var request = await AsyncGPUReadback.RequestAsync(colorAmountBuffer);

                if (!request.hasError)
                { request.GetData<uint>().CopyTo(colorAmountArray); }
            }
            finally
            {
                readingBuffer = false;
            }
        }

        internal void CleanUp()
        {
            if (fullCleanBuffer?.IsValid() ?? false) { fullCleanBuffer.Release(); fullCleanBuffer = null; }
            if (macroCleanBuffer?.IsValid() ?? false) { macroCleanBuffer.Release(); macroCleanBuffer = null; }
            if (microCleanBuffer?.IsValid() ?? false) { microCleanBuffer.Release(); microCleanBuffer = null; }

            if (calculationArgsBuffer?.IsValid() ?? false) { calculationArgsBuffer.Release(); calculationArgsBuffer = null; }
            if (dirtyObjectBuffer?.IsValid() ?? false) { dirtyObjectBuffer.Release(); dirtyObjectBuffer = null; }
            if (dirtyTileBuffer?.IsValid() ?? false) { dirtyTileBuffer.Release(); dirtyTileBuffer = null; }

            if (colorAmountBuffer?.IsValid() ?? false) { colorAmountBuffer.Release(); colorAmountBuffer = null; }
            if (objectDataBuffer?.IsValid() ?? false) { objectDataBuffer.Release(); objectDataBuffer = null; }
        }

        #endregion Lifecycle


        #region Pass Logic ================================================================================== Pass Logic

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (DrawingSystem == null || !Application.isPlaying) { return; }

            TextureHandle drawingArrayTextureH = default;

            Queue<DrawOperation> drawOperations = DrawingSystem.GetDrawOperations();
            List<DrawableObject> objectsToRegister = DrawingSystem.GetObjectsToRegister();
            List<DrawableObject> objectsToCleanUp = DrawingSystem.GetObjectsToCleanUp();
            List<DrawingSlice> slicesToCleanUp = DrawingSystem.GetSlicesToCleanUp();

            bool objectsRegistered = RegisterObjects(objectsToRegister);
            bool objectsUnregistered = UnregisterSlicesAndObjects(slicesToCleanUp, objectsToCleanUp);
            DrawingSystem.OnCleanUpsAndRegistrationsFinished();

            if (objectsRegistered || objectsUnregistered)
            {
                colorAmountBuffer.SetData(colorAmountArray);
                objectDataBuffer.SetData(objectDataArray);

                if (objectsUnregistered)
                {
                    drawingArrayTextureH = renderGraph.ImportTexture(DrawingSystem.DrawingArrayRTH);

                    colorsChanged = true;

                    // 1. Pass: Cleaning up
                    // Cleans up any full slices or other DrawableObjects marked for clean up
                    AddCleanUpPass(renderGraph, drawingArrayTextureH);
                }
            }

            // TODO: Add slice downscaling and merging

            if (drawOperations.Count > 0)
            {
                colorsChanged = true;

                BufferHandle calculationArgsBufferH = renderGraph.ImportBuffer(calculationArgsBuffer);
                BufferHandle dirtyObjectBufferH = renderGraph.ImportBuffer(dirtyObjectBuffer);
                BufferHandle dirtyTileBufferH = renderGraph.ImportBuffer(dirtyTileBuffer);

                BufferHandle colorAmountBufferH = renderGraph.ImportBuffer(colorAmountBuffer);
                BufferHandle objectDataBufferH = renderGraph.ImportBuffer(objectDataBuffer);

                TextureHandle depthTextureH = renderGraph.ImportTexture(DrawingSystem.DepthRTH);
                TextureHandle operationDataTextureH = renderGraph.ImportTexture(DrawingSystem.OperationDataRTH);

                if (!drawingArrayTextureH.IsValid()) { drawingArrayTextureH = renderGraph.ImportTexture(DrawingSystem.DrawingArrayRTH); }

                while (drawOperations.Count > 0)
                {
                    DrawOperation drawOperation = drawOperations.Dequeue();

                    if (drawOperation.Brush.FinalMaterial == null || drawOperation.Brush.MPB == null) { continue; }

                    if (drawOperation.Brush.IsJittered)
                    { ApplyJitter(ref drawOperation); }

                    // 2. Pass: Operation processing
                    // Renders brush info, scene UVs and slice index from brush camera's perspective
                    AddOperationProcessingPass(renderGraph, frameData, drawOperation, depthTextureH, operationDataTextureH);

                    // 3. Pass: Brush drawing
                    // Renders a fullscreen quad using the brush's final material
                    AddBrushDrawingPass(renderGraph, drawOperation, depthTextureH, operationDataTextureH);

                    // 4. Pass: Object drawing
                    // Samples UV and brush data and draws into the global drawing texture array
                    AddObjectDrawingPass(renderGraph, dirtyObjectBufferH, operationDataTextureH, drawingArrayTextureH);
                }

                // 5. Pass: Dirty tile generating
                // Splits dirtied objects into micro tiles for color calculating
                AddDirtyTileGeneratingPass(renderGraph, dirtyObjectBufferH, dirtyTileBufferH, colorAmountBufferH, objectDataBufferH);

                // 6. Pass: Color amount calculating
                // Updates color amounts for dirty objects from their micro tiles
                AddColorAmountCalculatingPass(renderGraph, calculationArgsBufferH, dirtyTileBufferH, colorAmountBufferH, drawingArrayTextureH);
            }
        }

        #endregion Pass Logic
        

        #region Passes ====================================================================================== Passes

        private void AddCleanUpPass(RenderGraph renderGraph, TextureHandle drawingArrayTextureH)
        {
            if (fullCleanList.Count == 0 && macroCleanList.Count == 0 && microCleanList.Count == 0) { return; }

            int fullCount = fullCleanList.Count;
            int macroCount = macroCleanList.Count;
            int microCount = microCleanList.Count;

            BufferHandle fullCleanBufferH = default;
            BufferHandle macroCleanBufferH = default;
            BufferHandle microCleanBufferH = default;

            if (fullCount > 0)
            {
                fullCleanBuffer.SetData(fullCleanList);
                fullCleanBufferH = renderGraph.ImportBuffer(fullCleanBuffer);
            }
            if (macroCount > 0)
            {
                macroCleanBuffer.SetData(macroCleanList);
                macroCleanBufferH = renderGraph.ImportBuffer(macroCleanBuffer);
            }
            if (microCount > 0)
            {
                microCleanBuffer.SetData(microCleanList);
                microCleanBufferH = renderGraph.ImportBuffer(microCleanBuffer);
            }

            using (var builder = renderGraph.AddComputePass<CleanUpPassData>("CleanUpPass", out var builtData))
            {
                builder.UseTexture(drawingArrayTextureH, AccessFlags.Write);

                if (fullCount > 0) { builder.UseBuffer(fullCleanBufferH, AccessFlags.Read); }
                if (macroCount > 0) { builder.UseBuffer(macroCleanBufferH, AccessFlags.Read); }
                if (microCount > 0) { builder.UseBuffer(microCleanBufferH, AccessFlags.Read); }

                builtData.DrawingArrayTextureH = drawingArrayTextureH;
                builtData.FullCleanBufferH = fullCleanBufferH;
                builtData.MacroCleanBufferH = macroCleanBufferH;
                builtData.MicroCleanBufferH = microCleanBufferH;
                builtData.FullCount = fullCount;
                builtData.MacroCount = macroCount;
                builtData.MicroCount = microCount;

                builder.SetRenderFunc((CleanUpPassData passData, ComputeGraphContext ctx) =>
                {
                    if (passData.FullCount > 0)
                    {
                        ctx.cmd.SetComputeTextureParam(objectCleaningCompute, fullCleanKernel, drawingArrayTextureID, passData.DrawingArrayTextureH);
                        ctx.cmd.SetComputeBufferParam(objectCleaningCompute, fullCleanKernel, fullCleanBufferID, passData.FullCleanBufferH);
                        ctx.cmd.DispatchCompute(objectCleaningCompute, fullCleanKernel, maxTextureSize / 16, maxTextureSize / 16, passData.FullCount);
                    }
                    if (passData.MacroCount > 0)
                    {
                        ctx.cmd.SetComputeTextureParam(objectCleaningCompute, macroCleanKernel, drawingArrayTextureID, passData.DrawingArrayTextureH);
                        ctx.cmd.SetComputeBufferParam(objectCleaningCompute, macroCleanKernel, macroCleanBufferID, passData.MacroCleanBufferH);
                        ctx.cmd.DispatchCompute(objectCleaningCompute, macroCleanKernel, macroTileSize / microTileSize, macroTileSize / microTileSize, passData.MacroCount);
                    }
                    if (passData.MicroCount > 0)
                    {
                        ctx.cmd.SetComputeTextureParam(objectCleaningCompute, microCleanKernel, drawingArrayTextureID, passData.DrawingArrayTextureH);
                        ctx.cmd.SetComputeBufferParam(objectCleaningCompute, microCleanKernel, microCleanBufferID, passData.MicroCleanBufferH);
                        ctx.cmd.DispatchCompute(objectCleaningCompute, microCleanKernel, 1, 1, passData.MicroCount);
                    }
                });
            }

            fullCleanList.Clear();
            macroCleanList.Clear();
            microCleanList.Clear();
        }

        private void AddOperationProcessingPass(RenderGraph renderGraph, ContextContainer frameData,
            DrawOperation drawOperation, TextureHandle depthTextureH, TextureHandle operationDataTextureH)
        {
            if (!TryCreateBrushRendererList(renderGraph, frameData, drawOperation.Brush.FinalAppliesToPaintOnly ? paintOnlyOperationDataMaterial : operationDataMaterial,
                drawOperation.ViewMatrix, drawOperation.ProjectionMatrix, out var rendererList)) { return; }

            using var builder = renderGraph.AddRasterRenderPass<OperationProcessingPassData>("OperationProcessingPass", out var builtData);

            builder.SetRenderAttachment(operationDataTextureH, 0, AccessFlags.Write);
            builder.SetRenderAttachmentDepth(depthTextureH, AccessFlags.Write);
            builder.UseRendererList(rendererList);

            builtData.Operation = drawOperation;
            builtData.RendererList = rendererList;

            builder.SetRenderFunc((OperationProcessingPassData passData, RasterGraphContext ctx) =>
            {
                DrawOperation operation = passData.Operation;
                Brush brush = operation.Brush;

                ctx.cmd.SetViewProjectionMatrices(operation.ViewMatrix, operation.ProjectionMatrix);
                ctx.cmd.ClearRenderTarget(true, true, Color.black);
                ctx.cmd.DrawRendererList(passData.RendererList);
            });
        }

        private void AddBrushDrawingPass(RenderGraph renderGraph,
            DrawOperation drawOperation, TextureHandle depthTextureH, TextureHandle operationDataTextureH)
        {
            using var builder = renderGraph.AddRasterRenderPass<BrushDrawingPassData>("BrushDrawingPass", out var builtData);

            builder.SetRenderAttachment(operationDataTextureH, 0, AccessFlags.Write);
            builder.UseTexture(depthTextureH, AccessFlags.Read);

            builtData.Operation = drawOperation;
            builtData.DepthTextureH = depthTextureH;

            builder.SetRenderFunc((BrushDrawingPassData passData, RasterGraphContext ctx) =>
            {
                Brush brush = passData.Operation.Brush;
                brush.MPB.SetTexture(brushDepthTextureID, passData.DepthTextureH);
                brush.MPB.SetFloat(brushIsOrthographicID, passData.Operation.IsOrthographic ? 1f : 0f);
                brush.MPB.SetFloat(brushZNearID, passData.Operation.ZNear);
                brush.MPB.SetFloat(brushZFarID, passData.Operation.ZFar);
                brush.MPB.SetVector(brushCameraPositionID, passData.Operation.Position);
                brush.MPB.SetMatrix(brushInverseViewProjID, (passData.Operation.ProjectionMatrix * passData.Operation.ViewMatrix).inverse);
                ctx.cmd.DrawProcedural(Matrix4x4.identity, brush.FinalMaterial, 0, MeshTopology.Triangles, 3, 1, brush.MPB);
            });
        }

        private void AddObjectDrawingPass(RenderGraph renderGraph,
            BufferHandle dirtyObjectBufferH, TextureHandle operationDataTextureH, TextureHandle drawingArrayTextureH)
        {
            using var builder = renderGraph.AddComputePass<ObjectDrawingPassData>("ObjectDrawingPass", out var builtData);

            builder.UseBuffer(dirtyObjectBufferH, AccessFlags.Write);
            builder.UseTexture(operationDataTextureH, AccessFlags.Read);
            builder.UseTexture(drawingArrayTextureH, AccessFlags.Write);

            builtData.DirtyObjectBufferH = dirtyObjectBufferH;
            builtData.OperationDataTextureH = operationDataTextureH;
            builtData.DrawingArrayTextureH = drawingArrayTextureH;

            builder.SetRenderFunc((ObjectDrawingPassData passData, ComputeGraphContext ctx) =>
            {
                ctx.cmd.SetComputeIntParam(objectDrawingCompute, maxColorsID, maxColors);
                ctx.cmd.SetComputeBufferParam(objectDrawingCompute, drawBrushKernel, dirtyObjectBufferID, passData.DirtyObjectBufferH);
                ctx.cmd.SetComputeTextureParam(objectDrawingCompute, drawBrushKernel, operationDataTextureID, passData.OperationDataTextureH);
                ctx.cmd.SetComputeTextureParam(objectDrawingCompute, drawBrushKernel, drawingArrayTextureID, passData.DrawingArrayTextureH);
                ctx.cmd.DispatchCompute(objectDrawingCompute, drawBrushKernel, brushSize / 8, brushSize / 8, 1);
            });
        }

        private void AddDirtyTileGeneratingPass(RenderGraph renderGraph,
            BufferHandle dirtyObjectBufferH, BufferHandle dirtyTileBufferH, BufferHandle colorAmountBufferH, BufferHandle objectDataBufferH)
        {
            using var builder = renderGraph.AddComputePass<DirtyTileGeneratingPassData>("DirtyTileGeneratingPass", out var builtData);

            builder.UseBuffer(colorAmountBufferH, AccessFlags.Write);
            builder.UseBuffer(objectDataBufferH, AccessFlags.Read);
            builder.UseBuffer(dirtyObjectBufferH, AccessFlags.ReadWrite);
            builder.UseBuffer(dirtyTileBufferH, AccessFlags.Write);

            builtData.DirtyObjectBufferH = dirtyObjectBufferH;
            builtData.DirtyTileBufferH = dirtyTileBufferH;
            builtData.ColorAmountBufferH = colorAmountBufferH;
            builtData.ObjectDataBufferH = objectDataBufferH;

            builder.SetRenderFunc((DirtyTileGeneratingPassData passData, ComputeGraphContext ctx) =>
            {
                ctx.cmd.SetBufferCounterValue(passData.DirtyTileBufferH, 0);
                ctx.cmd.SetComputeIntParam(tileGenerationCompute, maxColorsID, maxColors);
                ctx.cmd.SetComputeIntParam(tileGenerationCompute, maxObjectsID, maxObjects);
                ctx.cmd.SetComputeIntParam(tileGenerationCompute, microTileSizeID, microTileSize);
                ctx.cmd.SetComputeBufferParam(tileGenerationCompute, generateTilesKernel, dirtyObjectBufferID, passData.DirtyObjectBufferH);
                ctx.cmd.SetComputeBufferParam(tileGenerationCompute, generateTilesKernel, dirtyTileBufferID, passData.DirtyTileBufferH);
                ctx.cmd.SetComputeBufferParam(tileGenerationCompute, generateTilesKernel, colorAmountBufferID, passData.ColorAmountBufferH);
                ctx.cmd.SetComputeBufferParam(tileGenerationCompute, generateTilesKernel, objectDataBufferID, passData.ObjectDataBufferH);
                ctx.cmd.DispatchCompute(tileGenerationCompute, generateTilesKernel, Mathf.CeilToInt(maxObjects / 64f), 1, 1);
            });
        }

        private void AddColorAmountCalculatingPass(RenderGraph renderGraph,
            BufferHandle calculationArgsBufferH, BufferHandle dirtyTileBufferH, BufferHandle colorAmountBufferH, TextureHandle drawingArrayTextureH)
        {
            using var builder = renderGraph.AddComputePass<ColorAmountCalculatingData>("ColorAmountCalculatingPass", out var builtData);

            builder.UseBuffer(calculationArgsBufferH, AccessFlags.ReadWrite);
            builder.UseBuffer(dirtyTileBufferH, AccessFlags.Read);
            builder.UseBuffer(colorAmountBufferH, AccessFlags.ReadWrite);
            builder.UseTexture(drawingArrayTextureH, AccessFlags.Read);

            builtData.CalculationArgsBufferH = calculationArgsBufferH;
            builtData.DirtyTileBufferH = dirtyTileBufferH;
            builtData.ColorAmountBufferH = colorAmountBufferH;
            builtData.DrawingArrayTextureH = drawingArrayTextureH;

            builder.SetRenderFunc((ColorAmountCalculatingData passData, ComputeGraphContext ctx) =>
            {
                ctx.cmd.CopyCounterValue(passData.DirtyTileBufferH, passData.CalculationArgsBufferH, 0);
                ctx.cmd.SetComputeIntParam(colorCalculatingCompute, maxColorsID, maxColors);
                ctx.cmd.SetComputeIntParam(colorCalculatingCompute, maxObjectsID, maxObjects);
                ctx.cmd.SetComputeBufferParam(colorCalculatingCompute, calculateColorsKernel, dirtyTileBufferID, passData.DirtyTileBufferH);
                ctx.cmd.SetComputeBufferParam(colorCalculatingCompute, calculateColorsKernel, colorAmountBufferID, passData.ColorAmountBufferH);
                ctx.cmd.SetComputeTextureParam(colorCalculatingCompute, calculateColorsKernel, drawingArrayTextureID, passData.DrawingArrayTextureH);
                ctx.cmd.DispatchCompute(colorCalculatingCompute, calculateColorsKernel, passData.CalculationArgsBufferH, 0);
            });
        }

        #endregion Passes


        #region Internal Methods ============================================================================ Internal Methods

        internal bool TryGetColorAmounts(DrawableObject drawableObject, uint[] colorAmounts)
        {
            if (!drawableObject.ObjectIndex.HasValue) { return false; }

            int bufferIndex = drawableObject.ObjectIndex.Value;

            for (int i = 0; i < maxColors; i++)
            { colorAmounts[i] = colorAmountArray[(bufferIndex * maxColors) + i]; }

            return true;
        }

        #endregion Internal Methods


        #region Private Methods ============================================================================= Private Methods

        /// <returns> <see cref="bool"/>: Tells if changes occured </returns>
        private bool RegisterObjects(List<DrawableObject> registeredObjects)
        {
            bool changed = false;

            foreach (DrawableObject drawableObject in registeredObjects)
            {
                if (drawableObject.ObjectIndex.HasValue) { continue; }

                if (!freeObjectDataBufferIndices.TryPop(out int bufferIndex))
                { Util.LogError<ObjectDrawingPass>("Max drawable objects reached!"); return changed; }

                drawableObject.SetObjectIndex(bufferIndex);
                
                objectDataArray[bufferIndex] = new()
                {
                    SliceIndex = drawableObject.SliceIndex,
                    TextureIndex = drawableObject.TextureIndex,
                    TextureSize = drawableObject.TextureSize,
                    XMin = drawableObject.XMin,
                    YMin = drawableObject.YMin,
                };

                for (int i = 0; i < maxColors; i++)
                { colorAmountArray[(bufferIndex * maxColors) + i] = 0; }

                changed = true;
            }

            return changed;
        }

        /// <returns> <see cref="bool"/>: Tells if changes occured </returns>
        private bool UnregisterSlicesAndObjects(List<DrawingSlice> drawingSlices, List<DrawableObject> drawableObjects)
        {
            bool changed = false;

            Dictionary<int, DrawingSlice> indexToSlice = drawingSlices.ToDictionary(s => s.Index, s => s);

            foreach (DrawableObject drawableObject in drawableObjects)
            {
                if (indexToSlice.ContainsKey(drawableObject.SliceIndex)) { continue; }
                changed |= UnregisterDrawableObject(drawableObject, addForCleanUp: true);
            }

            foreach (DrawingSlice drawingSlice in drawingSlices)
            {
                foreach (DrawableObject drawableObject in drawingSlice.GetObjects())
                { UnregisterDrawableObject(drawableObject, addForCleanUp: false); }
                fullCleanList.Add(drawingSlice.Index);
                changed = true;
            }

            return changed;
        }

        /// <returns> <see cref="bool"/>: Tells if changes occured </returns>
        private bool UnregisterDrawableObject(DrawableObject drawableObject, bool addForCleanUp)
        {
            if (!drawableObject.ObjectIndex.HasValue) { return false; }

            int bufferIndex = drawableObject.ObjectIndex.Value;
            freeObjectDataBufferIndices.Push(bufferIndex);
            Debug.Log(freeObjectDataBufferIndices.Count);
            drawableObject.SetObjectIndex(null);
            
            if (addForCleanUp)
            { AddObjectDataToCleanUp(objectDataArray[bufferIndex]); }
            
            objectDataArray[bufferIndex] = ObjectData.Default;

            for (int i = 0; i < maxColors; i++)
            { colorAmountArray[(bufferIndex * maxColors) + i] = 0; }

            return true;
        }

        private void AddObjectDataToCleanUp(ObjectData objectData)
        {
            int sliceIndex = objectData.SliceIndex;
            int textureSize = objectData.TextureSize;
            int xMin = objectData.XMin;
            int yMin = objectData.YMin;

            if (objectData.TextureSize >= macroTileSize)
            {
                for (int x = xMin; x < xMin + textureSize; x += macroTileSize)
                {
                    for (int y = yMin; y < yMin + textureSize; y += macroTileSize)
                    { macroCleanList.Add(new() { SliceIndex = sliceIndex, XMin = x, YMin = y }); }
                }
            }
            else
            {
                for (int x = xMin; x < xMin + textureSize; x += microTileSize)
                {
                    for (int y = yMin; y < yMin + textureSize; y += microTileSize)
                    { microCleanList.Add(new() { SliceIndex = sliceIndex, XMin = x, YMin = y }); }
                }
            }
        }


        private void ApplyJitter(ref DrawOperation drawOperation)
        {
            Matrix4x4 viewMatrix = drawOperation.ViewMatrix;

            Vector3 offset = Random.onUnitSphere / 100f;
            Vector4 column = viewMatrix.GetColumn(3);

            column.x += offset.x;
            column.y += offset.y;
            column.z += offset.z;

            viewMatrix.SetColumn(3, column);

            drawOperation.ViewMatrix = viewMatrix;
        }

        private bool TryCreateBrushRendererList(RenderGraph graph, ContextContainer frameData,
            Material overrideMaterial, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix, out RendererListHandle rendererList)
        {
            rendererList = default;

            var renderingData = frameData.Get<UniversalRenderingData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var lightData = frameData.Get<UniversalLightData>();
            var cullContext = frameData.Get<CullContextData>();


            // Culling Results ------------------------------------------------------------------------ Culling Results

            if (!cameraData.camera.TryGetCullingParameters(out var cullingParams)) { return false; }

            cullingParams.cullingMatrix = projectionMatrix * viewMatrix;

            Plane[] frustumPlanes = new Plane[6];
            GeometryUtility.CalculateFrustumPlanes(cullingParams.cullingMatrix, frustumPlanes);

            for (int i = 0; i < 6; i++)
            { cullingParams.SetCullingPlane(i, frustumPlanes[i]); }

            CullingResults brushCullResults = cullContext.Cull(ref cullingParams);


            // Drawing Settings ----------------------------------------------------------------------- Drawing Settings

            SortingCriteria sorting = cameraData.defaultOpaqueSortFlags;
            ShaderTagId shaderTagID = new("SRPDefaultUnlit"); // <- Important (originally only included "Lit" meshes)
            DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(
                shaderTagID, renderingData, cameraData, lightData, sorting);

            drawSettings.overrideMaterial = overrideMaterial;
            drawSettings.overrideMaterialPassIndex = 0;


            RendererListParams listParams = new(brushCullResults, drawSettings, new FilteringSettings(RenderQueueRange.opaque));
            rendererList = graph.CreateRendererList(listParams);

            return rendererList.IsValid();
        }

        #endregion Private Methods
    }
}
