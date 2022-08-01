using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Parcel.Lib
{
    /// <summary>
    /// Allows for quickly determining the size in bytes of any value type.
    /// </summary>
    internal static class CorrectedSizeOf
    {
        private static string EXCP_NOT_VALUE = "Cannot determine size of non-value type {0}";

        private static Dictionary<Type, int> primitiveSizeDict = new Dictionary<Type, int>()
        {
            {typeof(sbyte), sizeof(sbyte) },
            {typeof(byte), sizeof(byte) },
            {typeof(short), sizeof(short) },
            {typeof(ushort), sizeof(ushort) },
            {typeof(int), sizeof(int) },
            {typeof(uint), sizeof(uint) },
            {typeof(long), sizeof(long) },
            {typeof(ulong), sizeof(ulong) },
            {typeof(float), sizeof(float) },
            {typeof(double), sizeof(double) },
            {typeof(decimal), sizeof(decimal) },
            {typeof(bool), sizeof(bool) },
            {typeof(char), sizeof(char) }
        };

        /// <summary>
        /// Get the size of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to get the size of.</typeparam>
        /// <returns>The size of type <typeparamref name="T"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf<T>() where T : struct
        {
            Type type = typeof(T);
            if (primitiveSizeDict.ContainsKey(type))
            {
                return primitiveSizeDict[type];
            }
            else
            {
                primitiveSizeDict.Add(type, Marshal.SizeOf<T>());
                return primitiveSizeDict[type];
            }
        }

        /// <summary>
        /// Get the size of <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type to get the size of.</param>
        /// <returns>The size of <paramref name="type"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf(Type type)
        {
            if (!type.IsValueType)
                throw new ArgumentException(string.Format(EXCP_NOT_VALUE, type.Name), nameof(type));

            if (primitiveSizeDict.ContainsKey(type))
            {
                return primitiveSizeDict[type];
            }
            else
            {
                primitiveSizeDict.Add(type, Marshal.SizeOf(type));
                return primitiveSizeDict[type];
            }
        }

    }
}
