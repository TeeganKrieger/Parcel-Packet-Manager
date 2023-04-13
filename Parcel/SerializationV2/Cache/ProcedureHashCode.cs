using System;
using System.Collections.Generic;
using System.Reflection;

namespace Parcel.Serialization
{
    [OptIn]
    public class ProcedureHashCode
    {
        private const string EXCP_PARSE_FAILED = "Failed to parse ProcedureHashCode. ProcedureHashCode might not be valid.";

        private static Dictionary<ProcedureHashCode, MethodInfo> ProcedureLookupTable = new Dictionary<ProcedureHashCode, MethodInfo>();

        /// <summary>
        /// The ulong representation of the hashcode.
        /// </summary>
        [Serialize]
        private ulong HashCode { get; set; }


        #region CONSTRUCTOR

        private ProcedureHashCode() { }

        /// <summary>
        /// Construct a new instance of ProcedureHashCode manually.
        /// </summary>
        /// <param name="hashcode">The ulong hashcode.</param>
        internal ProcedureHashCode(ulong hashcode)
        {
            HashCode = hashcode;
        }

        /// <summary>
        /// Construct a new instance of ProcedureHashCode from <paramref name="methodInfo"/>.
        /// </summary>
        /// <param name="methodInfo">The method info to construct the <see cref="ProcedureHashCode"/> from.</param>
        /// <exception cref="ArgumentException">Thrown if the provided method info has more than 255 parameters.</exception>
        internal ProcedureHashCode(MethodInfo methodInfo)
        {
            HashCode = CalculateHashCode(methodInfo);

            if (!ProcedureLookupTable.ContainsKey(this))
                ProcedureLookupTable.Add(this, methodInfo);
        }

        #endregion


        #region STATIC

        /// <summary>
        /// Convert a ProcedureHashCode into its ulong representation.
        /// </summary>
        /// <param name="procedureHashCode">The <see cref="ProcedureHashCode"/> to convert.</param>
        public static explicit operator ulong(ProcedureHashCode procedureHashCode)
        {
            return procedureHashCode.HashCode;
        }

        /// <summary>
        /// Calculate the ulong hashcode of <paramref name="methodInfo"/>.
        /// </summary>
        /// <param name="methodInfo">The method info to get the ulong hashcode of.</param>
        /// <returns>The ulong hashcode of <paramref name="methodInfo"/>.</returns>
        private static ulong CalculateHashCode(MethodInfo methodInfo)
        {
            ParameterInfo[] parameters = methodInfo.GetParameters();

            ulong hashcode = 435782743062377UL;

            foreach (char c in methodInfo.Name)
            {
                hashcode ^= 409298683300459UL;
                hashcode += c * 958219429008503UL;
            }

            foreach (ParameterInfo parameter in parameters)
            {
                foreach (char c in parameter.ParameterType.FullName)
                {
                    hashcode ^= 409298683300459UL;
                    hashcode += c * 958219429008503UL;
                }
            }

            return hashcode;
        }

        /// <summary>
        /// Parse the Type from a <see cref="ProcedureHashCode"/>.
        /// </summary>
        /// <param name="procedureHashCode">The <see cref="ProcedureHashCode"/> to parse.</param>
        /// <returns>The Type that was parsed.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no Type could be parsed.</exception>
        public static MethodInfo ParseMethodInfo(ProcedureHashCode procedureHashCode)
        {
            if (!ProcedureLookupTable.ContainsKey(procedureHashCode))
            {
                ulong hashcode = procedureHashCode.HashCode;
                MethodInfo foundMethodInfo = null;

                foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
                    foreach (Type type in ass.GetTypes())
                        foreach (MethodInfo methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic))
                            if (CalculateHashCode(methodInfo).Equals(hashcode))
                                foundMethodInfo = methodInfo;

                if (foundMethodInfo == null)
                    throw new InvalidOperationException(EXCP_PARSE_FAILED);

                ProcedureLookupTable.Add(procedureHashCode, foundMethodInfo);
            }
            return ProcedureLookupTable[procedureHashCode];
        }

        #endregion


        #region OVERRIDES

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is ProcedureHashCode phc)
                return phc.HashCode == HashCode;
            else
                return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (int)HashCode;
        }

        #endregion

    }
}
