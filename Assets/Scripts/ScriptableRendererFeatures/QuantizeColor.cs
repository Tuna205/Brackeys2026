using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class QuantizeColor : ScriptableRendererFeature
{
    [SerializeField] QuantizeColorSettings settings;
    QuantizeColorPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new QuantizeColorPass(settings);

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = settings.injectionPoint;

        // You can request URP color texture and depth buffer as inputs by uncommenting the line below,
        // URP will ensure copies of these resources are available for sampling before executing the render pass.
        // Only uncomment it if necessary, it will have a performance impact, especially on mobiles and other TBDR GPUs where it will break render passes.
        //m_ScriptablePass.ConfigureInput(ScriptableRenderPassInput.Color);

        // You can request URP to render to an intermediate texture by uncommenting the line below.
        // Use this option for passes that do not support rendering directly to the backbuffer.
        // Only uncomment it if necessary, it will have a performance impact, especially on mobiles and other TBDR GPUs where it will break render passes.
        //m_ScriptablePass.requiresIntermediateTexture = true;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }

    // Use this class to pass around settings from the feature to the pass
    [Serializable]
    public class QuantizeColorSettings
    {
        public Material material;
        public RenderPassEvent injectionPoint;
        public float Steps;
    }

    class QuantizeColorPass : ScriptableRenderPass
    {
        readonly QuantizeColorSettings settings;

        public QuantizeColorPass(QuantizeColorSettings settings)
        {
            this.settings = settings;
        }

        // This class stores the data needed by the RenderGraph pass.
        // It is passed as a parameter to the delegate function that executes the RenderGraph pass.
        private class PassData
        {
            internal Material material;
            internal float steps;
            internal TextureHandle source;
            internal int passIndex;
        }

        // This static method is passed as the RenderFunc delegate to the RenderGraph render pass.
        // It is used to execute draw commands.
        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            data.material.SetFloat("_Steps", data.steps);
            Blitter.BlitTexture(
                context.cmd,
                data.source,
                new Vector4(1,1,1,1),
                data.material,
                data.passIndex
            );
        }

        // RecordRenderGraph is where the RenderGraph handle can be accessed, through which render passes can be added to the graph.
        // FrameData is a context container through which URP resources can be accessed and managed.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            TextureHandle tempA = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, "temp A", false);
            AddComposizePass(renderGraph, resourceData.cameraColor, tempA, "copyColor", 0, ExecutePass);
            AddComposizePass(renderGraph, tempA, resourceData.cameraColor, "quantizeColor", 1, ExecutePass);

        }

        private void AddComposizePass(RenderGraph renderGraph, TextureHandle source, TextureHandle destination, string passName, int passIndex, BaseRenderFunc<PassData, RasterGraphContext> renderFunc)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
            {
                passData.material = settings.material;
                passData.source = source;
                passData.passIndex = passIndex;
                passData.steps = settings.Steps;
                builder.UseTexture(passData.source);
                builder.SetRenderAttachment(destination, 0);
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => renderFunc(data, context));
            }
        }
    }
}
