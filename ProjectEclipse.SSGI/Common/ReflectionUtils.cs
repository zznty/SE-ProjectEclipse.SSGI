using System;
using System.Reflection;
using HarmonyLib;

namespace ProjectEclipse.SSGI.Common
{
    public static class ReflectionUtils
    {
        public static MethodInfo FindMethod(this Type type, string methodName, Type[] parameters = null, Type[] generics = null)
        {
            var method = type.Method(methodName, parameters, generics);
            if (method != null || !type.IsInterface)
            {
                return method;
            }

            var interfaces = type.GetInterfaces();
            foreach (var face in interfaces)
            {
                if ((method = face.Method(methodName, parameters, generics)) is MethodInfo)
                {
                    return method;
                }
            }

            throw new Exception($"Method not found. {nameof(type)}={type}, {nameof(methodName)}={methodName}");
        }

        public static MethodInfo FindPropertyGetter(this Type type, string propertyName)
        {
            var getter = type.PropertyGetter(propertyName);
            if (getter != null || !type.IsInterface)
            {
                return getter;
            }

            var interfaces = type.GetInterfaces();
            foreach (var face in interfaces)
            {
                if ((getter = face.PropertyGetter(propertyName)) is MethodInfo)
                {
                    return getter;
                }
            }

            throw new Exception($"Property getter not found. {nameof(type)}={type}, {nameof(propertyName)}={propertyName}");
        }

        public static bool IsAssignableTo(this Type type, Type c)
        {
            return c.IsAssignableFrom(type);
        }
    }
}
