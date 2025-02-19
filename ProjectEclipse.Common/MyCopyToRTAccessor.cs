using HarmonyLib;
using SharpDX.Direct3D11;
using System;

namespace ProjectEclipse.Common
{
    public static class MyCopyToRTAccessor
    {
        private struct MyPixelShaders_Id
        {
            public int Index;
        }

        private static readonly Type _MyCopyToRT = AccessTools.TypeByName("VRageRender.MyCopyToRT");
        private static readonly Type _MyPixelShaders = AccessTools.TypeByName("VRageRender.MyPixelShaders");
        private static readonly Func<object, PixelShader> _MyPixelShaders_GetShader = _MyPixelShaders.Method("GetShader").CreateGenericStaticFunc<object, PixelShader>();
        private static readonly Func<object> _MyCopyToRT_m_copyPs       = _MyCopyToRT.Field("m_copyPs").CreateGenericStaticGetter<object>();
        private static readonly Func<object> _MyCopyToRT_m_copyFilterPs = _MyCopyToRT.Field("m_copyFilterPs").CreateGenericStaticGetter<object>();
        private static readonly Func<object> _MyCopyToRT_m_clearAlphaPs = _MyCopyToRT.Field("m_clearAlphaPs").CreateGenericStaticGetter<object>();
        private static readonly Func<object> _MyCopyToRT_m_stretchPs    = _MyCopyToRT.Field("m_stretchPs").CreateGenericStaticGetter<object>();

        private static readonly object _copyPsId;
        private static readonly object _copyFilterPsId;
        private static readonly object _clearAlphaPsId;
        private static readonly object _stretchPsId;

        static MyCopyToRTAccessor()
        {
            _copyPsId = _MyCopyToRT_m_copyPs.Invoke();
            _copyFilterPsId = _MyCopyToRT_m_copyFilterPs.Invoke();
            _clearAlphaPsId = _MyCopyToRT_m_clearAlphaPs.Invoke();
            _stretchPsId = _MyCopyToRT_m_stretchPs.Invoke();
        }

        public static PixelShader GetCopyPs() => _MyPixelShaders_GetShader.Invoke(_copyPsId);
        public static PixelShader GetCopyFilterPs() => _MyPixelShaders_GetShader.Invoke(_copyFilterPsId);
        public static PixelShader GetCopyAlphaPs() => _MyPixelShaders_GetShader.Invoke(_clearAlphaPsId);
        public static PixelShader GetCopyStretchPs() => _MyPixelShaders_GetShader.Invoke(_stretchPsId);
    }
}
