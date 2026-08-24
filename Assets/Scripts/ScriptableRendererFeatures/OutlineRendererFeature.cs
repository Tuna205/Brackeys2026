using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Diagnostics.Tracing;

public class OutlineRendererFeature : ScriptableRendererFeature
{
    [SerializeField] OutlineRenderFeatureSettings settings;
    OutlinePass outlinePass;

    public override void Create()
    {
        outlinePass = new OutlinePass(settings)
        {
            renderPassEvent = settings.InjectionPoint
        };

        outlinePass.ConfigureInput(ScriptableRenderPassInput.Normal | ScriptableRenderPassInput.Depth);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(outlinePass);
    }

    [Serializable]
    public class OutlineRenderFeatureSettings
    {
        public Material material;
        [Range(0, 1)] public float NormalThreshold = 0.5f;
        public float DepthThreshold = 0.5f;
        public RenderPassEvent InjectionPoint;
    }

    class OutlinePass : ScriptableRenderPass
    {
        readonly OutlineRenderFeatureSettings settings;

        public OutlinePass(OutlineRenderFeatureSettings settings)
        {
            this.settings = settings;
        }

        private class OutlinePassData
        {
            internal Material material;
            internal TextureHandle source;
            internal TextureHandle depth;
            internal Vector2 texelSize;
            internal float normalThreshold;
            internal float depthThreshold;
        }

        static void ExecutePass(OutlinePassData data, RasterGraphContext context)
        {
            data.material.SetVector("_TexelSize", data.texelSize);
            data.material.SetFloat("_NormalThreshold", data.normalThreshold);
            data.material.SetFloat("_DepthThreshold", data.depthThreshold);
            data.material.SetTexture("_DepthTexture", data.depth);
            Blitter.BlitTexture(
                context.cmd,
                data.source,
                new Vector4(1,1,1,1),
                data.material,
                0
                );
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            const string passName = "Outline pass";
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;

            TextureHandle tempA = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, "Temp A", false);
            TextureHandle tempB = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, "Temp B", false);

            using (var builder = renderGraph.AddRasterRenderPass<OutlinePassData>(passName, out var passData))
            {
                passData.material = settings.material;
                passData.source = resourceData.cameraNormalsTexture;
                passData.depth = resourceData.cameraDepthTexture;
                passData.texelSize = new Vector2(1f/descriptor.width, 1f/descriptor.height);
                passData.normalThreshold = settings.NormalThreshold;
                passData.depthThreshold = settings.DepthThreshold;
                builder.UseTexture(passData.source);
                builder.SetRenderAttachment(resourceData.cameraColor, 0);

                builder.SetRenderFunc((OutlinePassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }
    }
}
