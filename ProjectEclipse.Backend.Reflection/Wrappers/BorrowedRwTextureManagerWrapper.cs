using HarmonyLib;
using ProjectEclipse.Common;
using ProjectEclipse.Common.Interfaces;
using SharpDX.DXGI;
using System;

namespace ProjectEclipse.Backend.Reflection.Wrappers
{
    public class BorrowedRwTextureManagerWrapper : IResourcePool
    {
        private static readonly Type _MyManagers = AccessTools.TypeByName("VRage.Render11.Common.MyManagers");
        private static readonly Type _MyBorrowedRwTextureManager = AccessTools.TypeByName("VRage.Render11.Resources.MyBorrowedRwTextureManager");

        private static readonly Func<object> _MyManagers_RwTexturesPool_Getter = _MyManagers.Field("RwTexturesPool").CreateGenericStaticGetter<object>();
        private static readonly Func<object, object, object, object, object, object, object, object> _MyBorrowedRwTextureManager_BorrowRtv = _MyBorrowedRwTextureManager.Method("BorrowRtv",
            new Type[] { typeof(string), typeof(int), typeof(int), typeof(Format), typeof(int), typeof(int) }).CreateGenericFunc<object, object, object, object, object, object, object, object>();
        private static readonly Func<object, object, object, object, object, object, object, object> _MyBorrowedRwTextureManager_BorrowUav = _MyBorrowedRwTextureManager.Method("BorrowUav",
            new Type[] { typeof(string), typeof(int), typeof(int), typeof(Format), typeof(int), typeof(int) }).CreateGenericFunc<object, object, object, object, object, object, object, object>();

        private object _instance;

        public BorrowedRwTextureManagerWrapper()
        {
            _instance = _MyManagers_RwTexturesPool_Getter.Invoke();
        }

        public IBorrowedTexture2DSrvRtv BorrowTexture2DSrvRtv(string debugName, int width, int height, Format format)
        {
            return new BorrowedRtvTextureWrapper(_MyBorrowedRwTextureManager_BorrowRtv.Invoke(_instance, debugName, width, height, format, 1, 0));
        }

        public IBorrowedTexture2DSrvRtvUav BorrowTexture2DSrvRtvUav(string debugName, int width, int height, Format format)
        {
            return new BorrowedUavTextureWrapper(_MyBorrowedRwTextureManager_BorrowUav.Invoke(_instance, debugName, width, height, format, 1, 0));
        }
    }
}
