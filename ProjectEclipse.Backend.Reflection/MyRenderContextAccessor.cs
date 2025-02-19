using HarmonyLib;
using ProjectEclipse.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectEclipse.Backend.Reflection
{
    internal static class MyRenderContextAccessor
    {
        public static readonly Type Type_MyRenderContext = AccessTools.TypeByName("VRage.Render11.RenderContext.MyRenderContext");

        private static readonly Action<object> _MyRenderContext_ClearState = Type_MyRenderContext.Method("ClearState").CreateGenericAction<object>();

        public static void ClearState(object myRenderContextInstance) => _MyRenderContext_ClearState.Invoke(myRenderContextInstance);
    }
}
