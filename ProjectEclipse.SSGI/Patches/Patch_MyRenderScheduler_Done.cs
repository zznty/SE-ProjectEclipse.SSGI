using HarmonyLib;
using ProjectEclipse.Backend.Reflection;
using ProjectEclipse.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                var rc = MyRender11Accessor.GetDeviceInstance().ImmediateContext;
                var tempUav = Plugin.ResourcePool.BorrowTexture2DSrvRtvUav("ssgi_output_temp", MyRender11Accessor.GetViewportResolution(), SharpDX.DXGI.Format.R16G16B16A16_Float);

                var frameBuffer = MyGBufferAccessor.GetLBuffer();
                var depthBuffer = MyGBufferAccessor.GetDepthStencilSrvDepth();
                var gbuffer0 = MyGBufferAccessor.GetGBuffer0();
                var gbuffer1 = MyGBufferAccessor.GetGBuffer1();
                var gbuffer2 = MyGBufferAccessor.GetGBuffer2();

                Plugin.Renderer.Draw(rc, frameBuffer, depthBuffer, gbuffer0, gbuffer1, gbuffer2, tempUav);

                Plugin.RenderUtils.CopyToRT(rc, tempUav, frameBuffer);
                rc.OutputMerger.SetTargets();

                tempUav.Return();

                MyRender11Accessor.GetRC().ClearState();
            }
        }
    }
}
