using System;
using System.Collections.Generic;
using System.Linq;

namespace AspNetCore.Mvc.MvcAsApi.Extensions
{
    public static class TypeExtensions
    {
        public static IEnumerable<Type> GetTypeAndInterfaceHierarchy(this Type type)
        {
            return type.BaseType == typeof(object)
                ? Enumerable
                    .Repeat(type, 1)
                    .Concat(type.GetInterfaces())
                : Enumerable
                    .Repeat(type, 1)
                    .Concat(type.GetInterfaces())
                    .Concat(type.BaseType.GetTypeAndInterfaceHierarchy());
        }
    }

}
