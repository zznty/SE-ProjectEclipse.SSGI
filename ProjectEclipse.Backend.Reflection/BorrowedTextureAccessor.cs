using HarmonyLib;
using ProjectEclipse.Common;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using VRageMath;
using Resource = SharpDX.Direct3D11.Resource;

namespace ProjectEclipse.Backend.Reflection
{
    internal static class BorrowedTextureAccessor
    {
        public static readonly Type Type_IBorrowedSrvTexture = AccessTools.TypeByName("VRage.Render11.Resources.IBorrowedSrvTexture");
        public static readonly Type Type_IBorrowedRtvTexture = AccessTools.TypeByName("VRage.Render11.Resources.IBorrowedRtvTexture");
        public static readonly Type Type_IBorrowedUavTexture = AccessTools.TypeByName("VRage.Render11.Resources.IBorrowedUavTexture");

        private static readonly Action<object> _Release = Type_IBorrowedUavTexture.FindMethod("Release").CreateGenericAction<object>();
        private static readonly Func<object, Format> _Format_Getter = Type_IBorrowedUavTexture.FindPropertyGetter("Format").CreateGenericFunc<object, Format>();
        private static readonly Func<object, int> _MipLevels_Getter = Type_IBorrowedUavTexture.FindPropertyGetter("MipLevels").CreateGenericFunc<object, int>();

        internal static void Release(object instance) => _Release(instance);

        internal static Resource GetResource(object instance) => ViewBindableAccessor.GetResource(instance);
        internal static ShaderResourceView GetSrv(object instance) => ViewBindableAccessor.GetSrv(instance);
        internal static RenderTargetView GetRtv(object instance) => ViewBindableAccessor.GetRtv(instance);
        internal static UnorderedAccessView GetUav(object instance) => ViewBindableAccessor.GetUav(instance);
        internal static Vector2I GetSize(object instance) => ViewBindableAccessor.GetSize(instance);
        internal static Format GetFormat(object instance) => _Format_Getter(instance);
        internal static int GetMipLevels(object instance) => _MipLevels_Getter(instance);
    }
}
