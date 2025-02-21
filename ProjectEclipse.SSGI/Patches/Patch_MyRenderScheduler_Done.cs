using HarmonyLib;
using VRage.Render11.Resources;
using VRageRender;

namespace ProjectEclipse.SSGI.Patches
{
    [HarmonyPatch("VRage.Render11.Render.MyRenderScheduler", "Done")]
    public static class Patch_MyRenderScheduler_Done
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            // TODO: find a better method to patch (before drawing the light glare particles)
            if (Plugin.Config.Data.Enabled)
            {
                var rc = MyRender11.DeviceInstance.ImmediateContext;
                var tempUav = Plugin.ResourcePool.BorrowUav("ssgi_output_temp", SharpDX.DXGI.Format.R16G16B16A16_Float);

                var frameBuffer = MyGBuffer.Main.LBuffer;
                var depthBuffer = MyGBuffer.Main.GetDepthStencilCopyRtv(MyRender11.RC);
                var gbuffer0 = MyGBuffer.Main.GBuffer0;
                var gbuffer1 = MyGBuffer.Main.GBuffer1;
                var gbuffer2 = MyGBuffer.Main.GBuffer2;

                Plugin.Renderer.Draw(rc, frameBuffer, depthBuffer, gbuffer0, gbuffer1, gbuffer2, tempUav);

                Plugin.RenderUtils.CopyToRT(rc, tempUav, frameBuffer);
                rc.OutputMerger.SetTargets();

                tempUav.Release();

                MyRender11.RC.ClearState();
            }
        }
    }
}
