using HarmonyLib;
using ProjectEclipse.Backend.Reflection.Wrappers;
using ProjectEclipse.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectEclipse.Backend.Reflection
{
    public static class MyGBufferAccessor
    {
        private static readonly Type _MyGBuffer = AccessTools.TypeByName("VRage.Render11.Resources.MyGBuffer");
        private static readonly Type _IDepthStencil = AccessTools.TypeByName("VRage.Render11.Resources.IDepthStencil");

        private static readonly Func<object> _MyGBuffer_Main_Getter = _MyGBuffer.Field("Main").CreateGenericStaticGetter<object>();
        private static readonly Func<object, object> _MyGBuffer_LBuffer_Getter = _MyGBuffer.PropertyGetter("LBuffer").CreateGenericFunc<object, object>();
        private static readonly Func<object, object> _MyGBuffer_DepthStencil_Getter = _MyGBuffer.PropertyGetter("DepthStencil").CreateGenericFunc<object, object>();
        private static readonly Func<object, object> _MyGBuffer_GBuffer0_Getter = _MyGBuffer.PropertyGetter("GBuffer0").CreateGenericFunc<object, object>();
        private static readonly Func<object, object> _MyGBuffer_GBuffer1_Getter = _MyGBuffer.PropertyGetter("GBuffer1").CreateGenericFunc<object, object>();
        private static readonly Func<object, object> _MyGBuffer_GBuffer2_Getter = _MyGBuffer.PropertyGetter("GBuffer2").CreateGenericFunc<object, object>();
        private static readonly Func<object, object> _IDepthStencil_SrvDepth_Getter = _IDepthStencil.FindPropertyGetter("SrvDepth").CreateGenericFunc<object, object>();

        public static RtvTexture2DWrapper GetLBuffer() => new RtvTexture2DWrapper(_MyGBuffer_LBuffer_Getter(_MyGBuffer_Main_Getter()));
        public static SrvTexture2DWrapper GetDepthStencilSrvDepth()
        {
            object depthStencil = _MyGBuffer_DepthStencil_Getter(_MyGBuffer_Main_Getter());
            object srvDepth = _IDepthStencil_SrvDepth_Getter(depthStencil);
            return new SrvTexture2DWrapper(srvDepth);
        }

        public static RtvTexture2DWrapper GetGBuffer0() => new RtvTexture2DWrapper(_MyGBuffer_GBuffer0_Getter(_MyGBuffer_Main_Getter()));
        public static RtvTexture2DWrapper GetGBuffer1() => new RtvTexture2DWrapper(_MyGBuffer_GBuffer1_Getter(_MyGBuffer_Main_Getter()));
        public static RtvTexture2DWrapper GetGBuffer2() => new RtvTexture2DWrapper(_MyGBuffer_GBuffer2_Getter(_MyGBuffer_Main_Getter()));
    }
}
