
using Cysharp.Threading.Tasks;

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;


public class CanvasRayRendererFeature : ScriptableRendererFeature
{
    public static CanvasRayRendererFeature Instance { get; private set; } = null;

    public Vector2? CanvasRayUV { get; private set; } = null;

    [SerializeField] private Material uvMaterial = null;

    private MenuBook menuBook = null;
    private CanvasRayRenderPass canvasRayPass = null;


    public override void Create()
    {
        Instance = this;
        canvasRayPass = new(this);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (menuBook == null || menuBook.Renderer == null || !menuBook.IsOpen ||
            renderingData.cameraData.camera != menuBook.PageCanvasRayCamera) { return; }

        renderer.EnqueuePass(canvasRayPass);
    }

    public void SetMenuBook(MenuBook value)
    {
        menuBook = value;
        canvasRayPass.SetCanvasRayActive(menuBook != null);
    }


    private class CanvasRayRenderPass : ScriptableRenderPass
    {
        private class PassData
        {
            public Matrix4x4 ViewMatrix;
            public Matrix4x4 ProjectionMatrix;
            public SkinnedMeshRenderer BookRenderer;
            public Material UVMaterial;
        }

        private struct RG32Pixel
        {
            public ushort R;
            public ushort G;
        }

        private readonly CanvasRayRendererFeature feature = null;

        private bool canvasRayActive = false;
        private bool readingCanvasRayRT = false;

        internal CanvasRayRenderPass(CanvasRayRendererFeature feature)
        {
            this.feature = feature;
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Color);

            RenderPipelineManager.endContextRendering += OnEndContextRendering;
        }

        public void SetCanvasRayActive(bool value)
        {
            canvasRayActive = value;

            if (!canvasRayActive)
            { feature.CanvasRayUV = null; }
        }

        private void OnEndContextRendering(ScriptableRenderContext ctx, List<Camera> cameras)
        {
            if (feature.menuBook == null || !feature.menuBook.IsOpen) { return; }

            if (!readingCanvasRayRT && canvasRayActive)
            { ReadCanvasRayRTAsync().Forget(); }
        }

        private async UniTask ReadCanvasRayRTAsync()
        {
            if (feature.menuBook == null || !feature.menuBook.IsOpen) { return; }

            try
            {
                readingCanvasRayRT = true;
                var request = await AsyncGPUReadback.RequestAsync(feature.menuBook.CanvasRayRT);

                if (request.hasError || !canvasRayActive) { return; }

                RG32Pixel rawPixel = request.GetData<RG32Pixel>()[0];
                feature.CanvasRayUV = new(rawPixel.R / 65535f, rawPixel.G / 65535f);
            }
            finally
            {
                readingCanvasRayRT = false;
            }
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            MenuBook menuBook = feature.menuBook;

            if (menuBook == null || menuBook.Renderer == null || !menuBook.IsOpen) { return; }

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            Camera currentCamera = frameData.Get<UniversalCameraData>().camera;
            Camera canvasRayCamera = feature.menuBook.PageCanvasRayCamera;

            if (currentCamera != canvasRayCamera) { return; }

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("CanvasRayPass", out var passData))
            {
                Camera bookCamera = feature.menuBook.BookCamera;
                Vector2 screenPosition = feature.menuBook.MouseNormalizedPosition;
                (Matrix4x4 view, Matrix4x4 projection) = GetPixelRayMatrices(bookCamera, screenPosition);

                passData.ViewMatrix = view;
                passData.ProjectionMatrix = projection;
                passData.BookRenderer = feature.menuBook.Renderer;
                passData.UVMaterial = feature.uvMaterial;

                builder.SetRenderAttachment(resourceData.cameraColor, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resourceData.cameraDepth, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                {
                    ctx.cmd.SetViewProjectionMatrices(data.ViewMatrix, data.ProjectionMatrix);
                    ctx.cmd.DrawRenderer(data.BookRenderer, data.UVMaterial, 0, 0);
                });
            }
        }

        private (Matrix4x4 view, Matrix4x4 projection) GetPixelRayMatrices(Camera camera, Vector2 screenPosition)
        {
            Vector3 position = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;
            Matrix4x4 viewMatrix = Matrix4x4.TRS(position, rotation, Vector3.one).inverse;
            viewMatrix.m20 = -viewMatrix.m20;
            viewMatrix.m21 = -viewMatrix.m21;
            viewMatrix.m22 = -viewMatrix.m22;
            viewMatrix.m23 = -viewMatrix.m23;

            Vector2 center = new(screenPosition.x * 2f - 1f, screenPosition.y * 2f - 1f);
            Matrix4x4 T = Matrix4x4.Translate(new Vector3(-center.x, -center.y, 0));
            Matrix4x4 S = Matrix4x4.Scale(new Vector3(1000f, 1000f, 1f));
            Matrix4x4 CropMatrix = S * T;
            float fov = camera.fieldOfView;
            float aspect = camera.aspect;
            Matrix4x4 projectionMatrix = CropMatrix * Matrix4x4.Perspective(fov, aspect, 0.1f, 10f);

            return (viewMatrix, projectionMatrix);
        }
    }
}
