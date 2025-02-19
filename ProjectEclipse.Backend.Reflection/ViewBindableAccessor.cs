using HarmonyLib;
using ProjectEclipse.Common;
using SharpDX.Direct3D11;
using System;
using VRageMath;

namespace ProjectEclipse.Backend.Reflection
{
    public static class ViewBindableAccessor
    {
        public static readonly Type Type_IResource = AccessTools.TypeByName("VRage.Render11.Resources.IResource");
        public static readonly Type Type_ISrvBindable = AccessTools.TypeByName("VRage.Render11.Resources.ISrvBindable");
        public static readonly Type Type_IRtvBindable = AccessTools.TypeByName("VRage.Render11.Resources.IRtvBindable");
        public static readonly Type Type_IUavBindable = AccessTools.TypeByName("VRage.Render11.Resources.IUavBindable");

        private static readonly Func<object, Resource> _IResource_Resource_Getter = Type_IResource.PropertyGetter("Resource").CreateGenericFunc<object, Resource>();
        private static readonly Func<object, Vector2I> _IResource_Size_Getter = Type_IResource.PropertyGetter("Size").CreateGenericFunc<object, Vector2I>();
        private static readonly Func<object, ShaderResourceView> _ISrvBindable_Srv_Getter = Type_ISrvBindable.PropertyGetter("Srv").CreateGenericFunc<object, ShaderResourceView>();
        private static readonly Func<object, RenderTargetView> _IRtvBindable_Rtv_Getter = Type_IRtvBindable.PropertyGetter("Rtv").CreateGenericFunc<object, RenderTargetView>();
        private static readonly Func<object, UnorderedAccessView> _IUavBindable_Uav_Getter = Type_IUavBindable.PropertyGetter("Uav").CreateGenericFunc<object, UnorderedAccessView>();

        public static Resource GetResource(object srvBindableInstance) => _IResource_Resource_Getter.Invoke(srvBindableInstance);
        public static Vector2I GetSize(object srvBindableInstance) => _IResource_Size_Getter.Invoke(srvBindableInstance);
        public static ShaderResourceView GetSrv(object srvBindableInstance) => _ISrvBindable_Srv_Getter.Invoke(srvBindableInstance);
        public static RenderTargetView GetRtv(object rtvBindableInstance) => _IRtvBindable_Rtv_Getter.Invoke(rtvBindableInstance);
        public static UnorderedAccessView GetUav(object uavBindableInstance) => _IUavBindable_Uav_Getter.Invoke(uavBindableInstance);
    }
}
