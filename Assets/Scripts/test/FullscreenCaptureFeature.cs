using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FullscreenCaptureFeature : ScriptableRendererFeature
{
    class FullscreenCapturePass : ScriptableRenderPass
    {
        private RenderTargetIdentifier source;
        private RenderTargetHandle temporaryRT;

        public FullscreenCapturePass()
        {
            temporaryRT.Init("_SceneColorRT");
        }

        public void Setup(RenderTargetIdentifier source)
        {
            this.source = source;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Capture Scene Color");
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            cmd.GetTemporaryRT(temporaryRT.id, descriptor);
            Blit(cmd, source, temporaryRT.Identifier());
            cmd.SetGlobalTexture("_SceneColorTexture", temporaryRT.Identifier());
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(temporaryRT.id);
        }
    }

    FullscreenCapturePass capturePass;

    public override void Create()
    {
        capturePass = new FullscreenCapturePass();
        capturePass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        capturePass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(capturePass);
    }
}
