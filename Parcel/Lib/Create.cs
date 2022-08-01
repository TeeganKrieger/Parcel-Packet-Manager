using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Parcel.Lib
{
    /// <summary>
    /// Allows for fast cached creation of any type with a parameterless constructor.
    /// </summary>
    internal static class Create
    {
        private static ConcurrentDictionary<Type, Func<object>> Cache = new ConcurrentDictionary<Type, Func<object>>();

        /// <summary>
        /// Creates a new instance of <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The Type to create an instance of.</param>
        /// <returns>A new object.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the provided type does not have a parameterless constructor.</exception>
        /// <remarks>
        /// Constructors can have any accessibility, so long as they are parameterless. Objects can still have constructors with parameters as well.
        /// </remarks>
        public static object New(Type type)
        {
            if (Cache.ContainsKey(type))
                return Cache[type].Invoke();
            else
            {
                if (type == typeof(string))
                {
                    Cache.TryAdd(type, () => { return ""; });
                    return Cache[type].Invoke();
                }
                else if (type.IsValueType)
                {
                    MethodInfo newExp = typeof(Create).GetMethod("NewObj", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(type);
                    newExp.Bind();

                    Cache.TryAdd(type, () => { return newExp.Invoke(null, null); });
                    return Cache[type].Invoke();
                }
                else
                {
                    if (!CheckForValidConstructor(type))
                        throw new InvalidOperationException($"Failed to create new object. {type.FullName} does not have a default constructor!");

                    Func<object> func = Expression.Lambda<Func<object>>(Expression.New(type)).Compile();
                    Cache.TryAdd(type, func);
                    return func.Invoke();
                }
            }
        }

        private static T NewObj<T>() where T : struct
        {
            return new T();
        }

        /// <summary>
        /// Check if <paramref name="type"/> has a parameterless constructor.
        /// </summary>
        /// <param name="type">The Type to check.</param>
        /// <returns><see langword="true"/> if <paramref name="type"/> has a parameterless constructor; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckForValidConstructor(Type type)
        {
            if (type.IsPrimitive || type.IsArray || type == typeof(string) || type.IsInterface || type == typeof(decimal))
                return true;

            return type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null) != null;
        }
    }
}
