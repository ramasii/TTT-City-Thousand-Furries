using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule; 

public class OutlineFeature : ScriptableRendererFeature
{
    class OutlinePass : ScriptableRenderPass
    {
        private Material outlineMaterial;
        private OutlineVolume outlineSettings;

        public OutlinePass(Material material)
        {
            outlineMaterial = material;
            ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
        }

        private class PassData
        {
            public Material material;
            public TextureHandle source;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (outlineMaterial == null) return;

            var stack = VolumeManager.instance.stack;
            outlineSettings = stack.GetComponent<OutlineVolume>();

            if (outlineSettings == null || !outlineSettings.IsActive()) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (cameraData.cameraType == CameraType.Preview) return;

            TextureHandle activeColor = resourceData.activeColorTexture;
            if (!activeColor.IsValid()) return;

            // Kirim semua parameter termasuk Grazing Tolerance ke Material Shader
            outlineMaterial.SetColor("_OutlineColor", outlineSettings.outlineColor.value);
            outlineMaterial.SetFloat("_OutlineScale", outlineSettings.outlineScale.value);
            outlineMaterial.SetFloat("_DistanceFalloff", outlineSettings.distanceFalloff.value);
            outlineMaterial.SetFloat("_MinOutlineScale", outlineSettings.minOutlineScale.value);
            outlineMaterial.SetFloat("_DepthThreshold", outlineSettings.depthThreshold.value);
            outlineMaterial.SetFloat("_NormalThreshold", outlineSettings.normalThreshold.value);
            outlineMaterial.SetFloat("_GrazingTolerance", outlineSettings.grazingTolerance.value);

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0; 
            TextureHandle tempColorTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "OutlineTempColor", false);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Roystan Outline Process Pass", out var passData))
            {
                passData.material = outlineMaterial;
                passData.source = activeColor;

                builder.UseTexture(activeColor, AccessFlags.Read);
                builder.SetRenderAttachment(tempColorTexture, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Roystan Outline Copy Back", out var passData))
            {
                passData.source = tempColorTexture;

                builder.UseTexture(tempColorTexture, AccessFlags.Read);
                builder.SetRenderAttachment(activeColor, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), Blitter.GetBlitMaterial(TextureDimension.Tex2D), 0);
                });
            }
        }
    }

    [System.Serializable]
    public class OutlineSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public Shader outlineShader;
    }

    public OutlineSettings settings = new OutlineSettings();
    private OutlinePass outlinePass;
    private Material outlineMaterial;

    public override void Create()
    {
        if (settings.outlineShader == null)
            settings.outlineShader = Shader.Find("Hidden/Custom/OutlineURP");

        if (settings.outlineShader != null)
        {
            outlineMaterial = CoreUtils.CreateEngineMaterial(settings.outlineShader);
            outlinePass = new OutlinePass(outlineMaterial) { renderPassEvent = settings.renderPassEvent };
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (outlineMaterial == null || renderingData.cameraData.cameraType == CameraType.Preview) return;
        renderer.EnqueuePass(outlinePass);
    }

    protected override void Dispose(bool disposing) => CoreUtils.Destroy(outlineMaterial);
}