using System;
using System.Collections.Generic;

namespace Parcel.Serialization
{
    /// <summary>
    /// Extension methods for the System.Type.
    /// </summary>
    internal static class TypeExtensions
    {
        private static Dictionary<Type, TypeHashCode> TypeHashCodeLookupTable = new Dictionary<Type, TypeHashCode>();

        /// <summary>
        /// Get the <see cref="TypeHashCode"/> for a Type.
        /// </summary>
        /// <param name="type">The Type to get the hashcode of.</param>
        /// <returns>A <see cref="TypeHashCode"/>.</returns>
        public static TypeHashCode GetTypeHashCode(this Type type)
        {
            if (!TypeHashCodeLookupTable.ContainsKey(type))
                TypeHashCodeLookupTable.Add(type, new TypeHashCode(type));
            return TypeHashCodeLookupTable[type];
        }
    }
}
