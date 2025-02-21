using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace ProjectEclipse.SSGI.Common
{
    public static class GenericDelegateHelper
    {
        private static readonly MethodInfo _genericActionHelper = typeof(GenericDelegateHelper).Method(nameof(MagicMethodHelperGenericAction));
        private static readonly MethodInfo _genericActionHelper1 = typeof(GenericDelegateHelper).Method(nameof(MagicMethodHelperGenericAction1));
        private static readonly MethodInfo _genericActionHelper2 = typeof(GenericDelegateHelper).Method(nameof(MagicMethodHelperGenericAction2));
        private static readonly MethodInfo _genericActionHelper3 = typeof(GenericDelegateHelper).Method(nameof(MagicMethodHelperGenericAction3));
        private static readonly MethodInfo _genericStaticActionHelper = typeof(GenericDelegateHelper).Method(nameof(MagicMethodHelperGenericStaticAction));
        private static readonly MethodInfo _genericStaticActionHelper1 = typeof(GenericDelegateHelper).Method(nameof(MagicMethodHelperGenericStaticAction1));
        private static readonly MethodInfo _genericStaticActionHelper2 = typeof(GenericDelegateHelper).Method(nameof(MagicMethodHelperGenericStaticAction2));
        private static readonly MethodInfo _genericStaticActionHelper3 = typeof(GenericDelegateHelper).Method(nameof(MagicMethodHelperGenericStaticAction3));
        private static readonly MethodInfo _genericFuncHelper = typeof(GenericDelegateHelper).Method(nameof(MagicMethodHelperGenericFunc));
        private static readonly MethodInfo _genericFuncHelper1 = typeof(GenericDelegateHelper).Method(nameof(MagicMethodHelperGenericFunc1));
        private static readonly MethodInfo _genericFuncHelper2 = typeof(GenericDelegateHelper).Method(nameof(MagicMethodHelperGenericFunc2));
        private static readonly MethodInfo _genericFuncHelper3 = typeof(GenericDelegateHelper).Method(nameof(MagicMethodHelperGenericFunc3));
        private static readonly MethodInfo _genericFuncHelper4 = typeof(GenericDelegateHelper).Method(nameof(MagicMethodHelperGenericFunc4));
        private static readonly MethodInfo _genericFuncHelper5 = typeof(GenericDelegateHelper).Method(nameof(MagicMethodHelperGenericFunc5));
        private static readonly MethodInfo _genericFuncHelper6 = typeof(GenericDelegateHelper).Method(nameof(MagicMethodHelperGenericFunc6));
        private static readonly MethodInfo _genericStaticFuncHelper = typeof(GenericDelegateHelper).Method(nameof(MagicMethodHelperGenericStaticFunc));
        private static readonly MethodInfo _genericStaticFuncHelper1 = typeof(GenericDelegateHelper).Method(nameof(MagicMethodHelperGenericStaticFunc1));
        private static readonly MethodInfo _genericStaticFuncHelper2 = typeof(GenericDelegateHelper).Method(nameof(MagicMethodHelperGenericStaticFunc2));
        private static readonly MethodInfo _genericStaticFuncHelper3 = typeof(GenericDelegateHelper).Method(nameof(MagicMethodHelperGenericStaticFunc3));
        private static readonly MethodInfo _genericFieldGetterHelper = typeof(GenericDelegateHelper).Method(nameof(MagicFieldGetterHelperGeneric));
        private static readonly MethodInfo _genericStaticFieldGetterHelper = typeof(GenericDelegateHelper).Method(nameof(MagicStaticFieldGetterHelperGeneric));

        #region action
        public static Action<TTarget> CreateGenericAction<TTarget>(this MethodInfo method) =>
            (Action<TTarget>)CreateGenericDelegateInternal(method, _genericActionHelper);

        public static Action<TTarget, TParam1> CreateGenericAction<TTarget, TParam1>(this MethodInfo method) =>
            (Action<TTarget, TParam1>)CreateGenericDelegateInternal(method, _genericActionHelper1);

        public static Action<TTarget, TParam1, TParam2> CreateGenericAction<TTarget, TParam1, TParam2>(this MethodInfo method) =>
            (Action<TTarget, TParam1, TParam2>)CreateGenericDelegateInternal(method, _genericActionHelper2);

        public static Action<TTarget, TParam1, TParam2, TParam3> CreateGenericAction<TTarget, TParam1, TParam2, TParam3>(this MethodInfo method) =>
            (Action<TTarget, TParam1, TParam2, TParam3>)CreateGenericDelegateInternal(method, _genericActionHelper3);
        #endregion action

        #region static action
        public static Func<TReturn> CreateGenericStaticAction<TReturn>(this MethodInfo method) =>
            (Func<TReturn>)CreateGenericDelegateInternal(method, _genericStaticActionHelper);

        public static Func<TParam1, TReturn> CreateGenericStaticAction<TParam1, TReturn>(this MethodInfo method) =>
            (Func<TParam1, TReturn>)CreateGenericDelegateInternal(method, _genericStaticActionHelper1);

        public static Func<TParam1, TParam2, TReturn> CreateGenericStaticAction<TParam1, TParam2, TReturn>(this MethodInfo method) =>
            (Func<TParam1, TParam2, TReturn>)CreateGenericDelegateInternal(method, _genericStaticActionHelper2);

        public static Func<TParam1, TParam2, TParam3, TReturn> CreateGenericStaticAction<TParam1, TParam2, TParam3, TReturn>(this MethodInfo method) =>
            (Func<TParam1, TParam2, TParam3, TReturn>)CreateGenericDelegateInternal(method, _genericStaticActionHelper3);
        #endregion static action

        #region func
        public static Func<TTarget, TReturn> CreateGenericFunc<TTarget, TReturn>(this MethodInfo method) =>
            (Func<TTarget, TReturn>)CreateGenericDelegateInternal(method, _genericFuncHelper, typeof(TReturn));

        public static Func<TTarget, TParam1, TReturn> CreateGenericFunc<TTarget, TParam1, TReturn>(this MethodInfo method) =>
            (Func<TTarget, TParam1, TReturn>)CreateGenericDelegateInternal(method, _genericFuncHelper1, typeof(TReturn));

        public static Func<TTarget, TParam1, TParam2, TReturn> CreateGenericFunc<TTarget, TParam1, TParam2, TReturn>(this MethodInfo method) =>
            (Func<TTarget, TParam1, TParam2, TReturn>)CreateGenericDelegateInternal(method, _genericFuncHelper2, typeof(TReturn));

        public static Func<TTarget, TParam1, TParam2, TParam3, TReturn> CreateGenericFunc<TTarget, TParam1, TParam2, TParam3, TReturn>(this MethodInfo method) =>
            (Func<TTarget, TParam1, TParam2, TParam3, TReturn>)CreateGenericDelegateInternal(method, _genericFuncHelper3, typeof(TReturn));

        public static Func<TTarget, TParam1, TParam2, TParam3, TParam4, TReturn> CreateGenericFunc<TTarget, TParam1, TParam2, TParam3, TParam4, TReturn>(this MethodInfo method) =>
            (Func<TTarget, TParam1, TParam2, TParam3, TParam4, TReturn>)CreateGenericDelegateInternal(method, _genericFuncHelper4, typeof(TReturn));

        public static Func<TTarget, TParam1, TParam2, TParam3, TParam4, TParam5, TReturn> CreateGenericFunc<TTarget, TParam1, TParam2, TParam3, TParam4, TParam5, TReturn>(this MethodInfo method) =>
            (Func<TTarget, TParam1, TParam2, TParam3, TParam4, TParam5, TReturn>)CreateGenericDelegateInternal(method, _genericFuncHelper5, typeof(TReturn));

        public static Func<TTarget, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TReturn> CreateGenericFunc<TTarget, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TReturn>(this MethodInfo method) =>
            (Func<TTarget, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TReturn>)CreateGenericDelegateInternal(method, _genericFuncHelper6, typeof(TReturn));
        #endregion func

        #region static func
        public static Func<TReturn> CreateGenericStaticFunc<TReturn>(this MethodInfo method) =>
            (Func<TReturn>)CreateGenericDelegateInternal(method, _genericStaticFuncHelper, typeof(TReturn));

        public static Func<TParam1, TReturn> CreateGenericStaticFunc<TParam1, TReturn>(this MethodInfo method) =>
            (Func<TParam1, TReturn>)CreateGenericDelegateInternal(method, _genericStaticFuncHelper1, typeof(TReturn));

        public static Func<TParam1, TParam2, TReturn> CreateGenericStaticFunc<TParam1, TParam2, TReturn>(this MethodInfo method) =>
            (Func<TParam1, TParam2, TReturn>)CreateGenericDelegateInternal(method, _genericStaticFuncHelper2, typeof(TReturn));

        public static Func<TParam1, TParam2, TParam3, TReturn> CreateGenericStaticFunc<TParam1, TParam2, TParam3, TReturn>(this MethodInfo method) =>
            (Func<TParam1, TParam2, TParam3, TReturn>)CreateGenericDelegateInternal(method, _genericStaticFuncHelper3, typeof(TReturn));
        #endregion static func

        #region field getter
        public static Func<TTarget, TReturn> CreateGenericGetter<TTarget, TReturn>(this FieldInfo field)
        {
            var constructedHelper = _genericFieldGetterHelper.MakeGenericMethod(field.DeclaringType, field.FieldType, typeof(TReturn));
            return (Func<TTarget, TReturn>)constructedHelper.Invoke(null, new object[] { field });
        }
        #endregion field getter

        #region static field getter
        public static Func<TReturn> CreateGenericStaticGetter<TReturn>(this FieldInfo field)
        {
            var constructedHelper = _genericStaticFieldGetterHelper.MakeGenericMethod(field.FieldType, typeof(TReturn));
            return (Func<TReturn>)constructedHelper.Invoke(null, new object[] { field });
        }
        #endregion static field getter

        private static object CreateGenericDelegateInternal(MethodInfo method, MethodInfo helperMethod, Type returnType = null)
        {
            var types = method.GetParameters().Select(i => i.ParameterType);
            if (!method.IsStatic)
                types = types.Prepend(method.DeclaringType);
            if (method.ReturnType != typeof(void))
            {
                types = types.Append(method.ReturnType);
                types = types.Append(returnType);
            }
            var constructedHelper = helperMethod.MakeGenericMethod(types.ToArray());
            return constructedHelper.Invoke(null, new object[] { method });
        }

        private static Action<object> MagicMethodHelperGenericAction<TTarget>(MethodInfo method) where TTarget : class
        {
            var action = (Action<TTarget>)method.CreateDelegate(typeof(Action<TTarget>));
            return (target) => action((TTarget)target);
        }

        private static Action<object, object> MagicMethodHelperGenericAction1<TTarget, TParam1>(MethodInfo method) where TTarget : class
        {
            var action = (Action<TTarget, TParam1>)method.CreateDelegate(typeof(Action<TTarget, TParam1>));
            return (target, param1) => action((TTarget)target, (TParam1)param1);
        }

        private static Action<object, object, object> MagicMethodHelperGenericAction2<TTarget, TParam1, TParam2>(MethodInfo method) where TTarget : class
        {
            var action = (Action<TTarget, TParam1, TParam2>)method.CreateDelegate(typeof(Action<TTarget, TParam1, TParam2>));
            return (target, param1, param2) => action((TTarget)target, (TParam1)param1, (TParam2)param2);
        }

        private static Action<object, object, object, object> MagicMethodHelperGenericAction3<TTarget, TParam1, TParam2, TParam3>(MethodInfo method) where TTarget : class
        {
            var action = (Action<TTarget, TParam1, TParam2, TParam3>)method.CreateDelegate(typeof(Action<TTarget, TParam1, TParam2, TParam3>));
            return (target, param1, param2, param3) => action((TTarget)target, (TParam1)param1, (TParam2)param2, (TParam3)param3);
        }

        private static Action MagicMethodHelperGenericStaticAction(MethodInfo method)
        {
            var func = (Action)method.CreateDelegate(typeof(Action));
            return () => func();
        }

        private static Action<object> MagicMethodHelperGenericStaticAction1<TParam1>(MethodInfo method)
        {
            var func = (Action<TParam1>)method.CreateDelegate(typeof(Action<TParam1>));
            return (param1) => func((TParam1)param1);
        }

        private static Action<object, object> MagicMethodHelperGenericStaticAction2<TParam1, TParam2>(MethodInfo method)
        {
            var func = (Action<TParam1, TParam2>)method.CreateDelegate(typeof(Action<TParam1, TParam2>));
            return (param1, param2) => func((TParam1)param1, (TParam2)param2);
        }

        private static Action<object, object, object> MagicMethodHelperGenericStaticAction3<TParam1, TParam2, TParam3>(MethodInfo method)
        {
            var func = (Action<TParam1, TParam2, TParam3>)method.CreateDelegate(typeof(Action<TParam1, TParam2, TParam3>));
            return (param1, param2, param3) => func((TParam1)param1, (TParam2)param2, (TParam3)param3);
        }

        private static Func<object, TDelegateReturn> MagicMethodHelperGenericFunc<TTarget, TReturn, TDelegateReturn>(MethodInfo method) where TTarget : class where TReturn : TDelegateReturn
        {
            var func = (Func<TTarget, TReturn>)method.CreateDelegate(typeof(Func<TTarget, TReturn>));
            return (target) => func((TTarget)target);
        }

        private static Func<object, object, TDelegateReturn> MagicMethodHelperGenericFunc1<TTarget, TParam1, TReturn, TDelegateReturn>(MethodInfo method) where TTarget : class where TReturn : TDelegateReturn
        {
            var func = (Func<TTarget, TParam1, TReturn>)method.CreateDelegate(typeof(Func<TTarget, TParam1, TReturn>));
            return (target, param1) => func((TTarget)target, (TParam1)param1);
        }

        private static Func<object, object, object, TDelegateReturn> MagicMethodHelperGenericFunc2<TTarget, TParam1, TParam2, TReturn, TDelegateReturn>(MethodInfo method) where TTarget : class where TReturn : TDelegateReturn
        {
            var func = (Func<TTarget, TParam1, TParam2, TReturn>)method.CreateDelegate(typeof(Func<TTarget, TParam1, TParam2, TReturn>));
            return (target, param1, param2) => func((TTarget)target, (TParam1)param1, (TParam2)param2);
        }

        private static Func<object, object, object, object, TDelegateReturn> MagicMethodHelperGenericFunc3<TTarget, TParam1, TParam2, TParam3, TReturn, TDelegateReturn>(MethodInfo method) where TTarget : class where TReturn : TDelegateReturn
        {
            var func = (Func<TTarget, TParam1, TParam2, TParam3, TReturn>)method.CreateDelegate(typeof(Func<TTarget, TParam1, TParam2, TParam3, TReturn>));
            return (target, param1, param2, param3) => func((TTarget)target, (TParam1)param1, (TParam2)param2, (TParam3)param3);
        }

        private static Func<object, object, object, object, object, TDelegateReturn> MagicMethodHelperGenericFunc4<TTarget, TParam1, TParam2, TParam3, TParam4, TReturn, TDelegateReturn>(MethodInfo method) where TTarget : class where TReturn : TDelegateReturn
        {
            var func = (Func<TTarget, TParam1, TParam2, TParam3, TParam4, TReturn>)method.CreateDelegate(typeof(Func<TTarget, TParam1, TParam2, TParam3, TParam4, TReturn>));
            return (target, param1, param2, param3, param4) => func((TTarget)target, (TParam1)param1, (TParam2)param2, (TParam3)param3, (TParam4)param4);
        }

        private static Func<object, object, object, object, object, object, TDelegateReturn> MagicMethodHelperGenericFunc5<TTarget, TParam1, TParam2, TParam3, TParam4, TParam5, TReturn, TDelegateReturn>(MethodInfo method) where TTarget : class where TReturn : TDelegateReturn
        {
            var func = (Func<TTarget, TParam1, TParam2, TParam3, TParam4, TParam5, TReturn>)method.CreateDelegate(typeof(Func<TTarget, TParam1, TParam2, TParam3, TParam4, TParam5, TReturn>));
            return (target, param1, param2, param3, param4, param5) => func((TTarget)target, (TParam1)param1, (TParam2)param2, (TParam3)param3, (TParam4)param4, (TParam5)param5);
        }

        private static Func<object, object, object, object, object, object, object, TDelegateReturn> MagicMethodHelperGenericFunc6<TTarget, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TReturn, TDelegateReturn>(MethodInfo method) where TTarget : class where TReturn : TDelegateReturn
        {
            var func = (Func<TTarget, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TReturn>)method.CreateDelegate(typeof(Func<TTarget, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TReturn>));
            return (target, param1, param2, param3, param4, param5, param6) => func((TTarget)target, (TParam1)param1, (TParam2)param2, (TParam3)param3, (TParam4)param4, (TParam5)param5, (TParam6)param6);
        }

        private static Func<TDelegateReturn> MagicMethodHelperGenericStaticFunc<TReturn, TDelegateReturn>(MethodInfo method) where TReturn : TDelegateReturn
        {
            var func = (Func<TReturn>)method.CreateDelegate(typeof(Func<TReturn>));
            return () => func();
        }

        private static Func<object, TDelegateReturn> MagicMethodHelperGenericStaticFunc1<TParam1, TReturn, TDelegateReturn>(MethodInfo method) where TReturn : TDelegateReturn
        {
            var func = (Func<TParam1, TReturn>)method.CreateDelegate(typeof(Func<TParam1, TReturn>));
            return (param1) => func((TParam1)param1);
        }

        private static Func<object, object, TDelegateReturn> MagicMethodHelperGenericStaticFunc2<TParam1, TParam2, TReturn, TDelegateReturn>(MethodInfo method) where TReturn : TDelegateReturn
        {
            var func = (Func<TParam1, TParam2, TReturn>)method.CreateDelegate(typeof(Func<TParam1, TParam2, TReturn>));
            return (param1, param2) => func((TParam1)param1, (TParam2)param2);
        }

        private static Func<object, object, object, TDelegateReturn> MagicMethodHelperGenericStaticFunc3<TParam1, TParam2, TParam3, TReturn, TDelegateReturn>(MethodInfo method) where TReturn : TDelegateReturn
        {
            var func = (Func<TParam1, TParam2, TParam3, TReturn>)method.CreateDelegate(typeof(Func<TParam1, TParam2, TParam3, TReturn>));
            return (param1, param2, param3) => func((TParam1)param1, (TParam2)param2, (TParam3)param3);
        }

        private static Func<object, TDelegateReturn> MagicFieldGetterHelperGeneric<TTarget, TReturn, TDelegateReturn>(FieldInfo field) where TReturn : TDelegateReturn
        {
            if (!field.FieldType.IsAssignableTo(typeof(TReturn)))
            {
                throw new Exception();
            }

            var methodName = field.ReflectedType.FullName + ".get_" + field.Name;
            var getterMethod = new DynamicMethod(methodName, field.FieldType, new Type[] { field.DeclaringType }, true);
            var ilGen = getterMethod.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldfld, field);
            ilGen.Emit(OpCodes.Ret);

            var func = (Func<TTarget, TReturn>)getterMethod.CreateDelegate(typeof(Func<TTarget, TReturn>));
            return (target) => func((TTarget)target);
        }

        private static Func<TDelegateReturn> MagicStaticFieldGetterHelperGeneric<TReturn, TDelegateReturn>(FieldInfo field) where TReturn : TDelegateReturn
        {
            if (!field.FieldType.IsAssignableTo(typeof(TReturn)))
            {
                throw new Exception();
            }

            var methodName = field.ReflectedType.FullName + ".get_" + field.Name;
            var getterMethod = new DynamicMethod(methodName, field.FieldType, null, true);
            var ilGen = getterMethod.GetILGenerator();
            ilGen.Emit(OpCodes.Ldsfld, field);
            ilGen.Emit(OpCodes.Ret);

            var func = (Func<TReturn>)getterMethod.CreateDelegate(typeof(Func<TReturn>));
            return () => func();
        }
    }
}
