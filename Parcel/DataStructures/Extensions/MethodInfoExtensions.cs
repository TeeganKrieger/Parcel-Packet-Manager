using Parcel.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Parcel.DataStructures
{
    /// <summary>
    /// Extension methods for System.Reflection.MethodInfo.
    /// </summary>
    public static class MethodInfoExtensions
    {
        private static Dictionary<MethodInfo, ProcedureHashCode> ProcedureHashCodeLookupTable = new Dictionary<MethodInfo, ProcedureHashCode>();

        /// <summary>
        /// Get the <see cref="ProcedureHashCode"/> for a MethodInfo.
        /// </summary>
        /// <param name="methodInfo">The MethodInfo to get the hashcode of.</param>
        /// <returns>A <see cref="ProcedureHashCode"/>.</returns>
        public static ProcedureHashCode GetProcedureHash(this MethodInfo methodInfo)
        {
            if (!ProcedureHashCodeLookupTable.ContainsKey(methodInfo))
                ProcedureHashCodeLookupTable.Add(methodInfo, new ProcedureHashCode(methodInfo));
            return ProcedureHashCodeLookupTable[methodInfo];
        }

    }
}
