
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;


public class HubRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private Material sceneFullMaterial = null;

    private HubRendererPass hubRendererPass = null;

    public override void Create()
    {
        hubRendererPass = new(sceneFullMaterial);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        string cameraName = renderingData.cameraData.camera.gameObject.name;
        bool useHubPass = cameraName.StartsWith("Hub");

#if UNITY_EDITOR
        Scene scene = renderingData.cameraData.camera.gameObject.scene;

        string sceneName = string.IsNullOrEmpty(scene.name)
            ? SceneManager.GetActiveScene().name
            : scene.name;

        useHubPass |= sceneName.StartsWith("_Hub") && (cameraName == "SceneCamera");
#endif

        if (useHubPass)
        { renderer.EnqueuePass(hubRendererPass); }
    }


    private class HubRendererPass : ScriptableRenderPass
    {
        private class PassData
        {
            public Material SceneFullMaterial = null;
            public TextureHandle SourceTexture = default;
        }

        private Material sceneFullMaterial = null;

        internal HubRendererPass(Material sceneFullMaterial)
        {
            this.sceneFullMaterial = sceneFullMaterial;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal | ScriptableRenderPassInput.Color);
        }

        public void SetSceneFullMaterial(Material value) => sceneFullMaterial = value;

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (sceneFullMaterial == null) { return; }

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            TextureHandle depthTexture = resourceData.cameraDepthTexture;
            TextureHandle normalsTexture = resourceData.cameraNormalsTexture;

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.msaaSamples = 1;
            desc.depthBufferBits = 0;

            TextureHandle modifiedColorTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "ModifiedColorTexture", false);

            if (!depthTexture.IsValid() || !normalsTexture.IsValid() || !modifiedColorTexture.IsValid()) { return; }

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("SceneFullScreenPass", out var passData))
            {
                passData.SceneFullMaterial = sceneFullMaterial;
                passData.SourceTexture = modifiedColorTexture;

                builder.UseTexture(depthTexture, AccessFlags.Read);
                builder.UseTexture(normalsTexture, AccessFlags.Read);
                builder.SetRenderAttachment(modifiedColorTexture, 0, AccessFlags.ReadWrite);

                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                { Blitter.BlitTexture(ctx.cmd, Vector2.one, data.SceneFullMaterial, 0); });
            }

            resourceData.cameraColor = modifiedColorTexture;
        }
    }


#if UNITY_EDITOR

    protected virtual void OnValidate()
    {
        hubRendererPass?.SetSceneFullMaterial(sceneFullMaterial);
    }

#endif
}
