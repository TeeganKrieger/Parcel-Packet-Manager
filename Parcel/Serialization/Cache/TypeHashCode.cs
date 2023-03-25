using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Parcel.Serialization
{
    /// <summary>
    /// Represents a reversible hashcode for Types.
    /// </summary>
    [OptIn]
    public class TypeHashCode : IEnumerable<TypeHashCode>
    {
        private const string EXCP_ARR_RANK = "Cannot create TypeHashCode for type {0} because it has a rank of {1}. Maximum supported rank is 127.";
        private const string EXCP_GENERIC_COUNT = "Cannot create TypeHashCode for type {0} because it has {1} generic arguments. Maximum supported generic arguments is 127.";
        private const string EXCP_PARSE_FAILED = "Failed to parse TypeHashCode. TypeHashCode might not be valid.";

        private static Dictionary<TypeHashCode, Type> TypeLookupTable = new Dictionary<TypeHashCode, Type>();
        private const ulong MASK = 0xFF_FF_FF_FF_FF_FF_FF_00;

        /// <summary>
        /// The ulong representation of the hashcode.
        /// </summary>
        [Serialize]
        private ulong HashCode { get; set; }

        /// <summary>
        /// An array of <see cref="TypeHashCode">TypeHashCodes</see> representing any generic arguments of this type.
        /// </summary>
        [Serialize]
        private TypeHashCode[] GenericArguments { get; set; }

        /// <summary>
        /// Whether this type is generic or not.
        /// </summary>
        [Ignore]
        public bool IsGenericType => (HashCode & ~MASK) > 0 && (HashCode & ~MASK & 128) == 0;

        /// <summary>
        /// Whether this type is an array or not.
        /// </summary>
        [Ignore]
        public bool IsArrayType => (HashCode & ~MASK) > 0 && (HashCode & ~MASK & 128) == 128;

        /// <summary>
        /// Get the number of generic arguments.
        /// </summary>
        [Ignore]
        public int GenericArgumentCount => (int)(HashCode & ~MASK & 127);

        /// <summary>
        /// Get the rank of the array if the Type is an array.
        /// </summary>
        [Ignore]
        public int ArrayRank => (int)(HashCode & ~MASK & 127);


        #region CONSTRUCTOR

        /// <summary>
        /// Construct a new instance of TypeHashCode manually.
        /// </summary>
        /// <param name="hashcode">The ulong hashcode.</param>
        /// <param name="genericArgs">The generic arguments.</param>
        internal TypeHashCode(ulong hashcode, TypeHashCode[] genericArgs)
        {
            HashCode = hashcode;
            GenericArguments = genericArgs;
        }

        /// <summary>
        /// Construct a new instance of TypeHashCode from <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type to construct the <see cref="TypeHashCode"/> from.</param>
        /// <exception cref="ArgumentException">Thrown if either the array rank or generic argument count is greater than 127.</exception>
        internal TypeHashCode(Type type)
        {
            Type cleanType = type;
            byte infoByte = 0;
            //First bit says: 0 = Generic or Normal Type  |  1 = Array Type
            //Bit 2 - 8 = 0 - 127 -> if this is a non-0 number, refer to first bit to decypher

            if (type.IsArray)
            {
                int arrRank = type.GetArrayRank();

                if (arrRank > 127)
                    throw new ArgumentException(string.Format(EXCP_ARR_RANK, type.Name, arrRank), nameof(type));

                cleanType = type.GetElementType();

                infoByte = (byte)arrRank;
                infoByte |= (byte)128;
            }
            else if (type.IsGenericType)
            {
                int genericArgsCount = type.GenericTypeArguments.Length;

                if (genericArgsCount > 127)
                    throw new ArgumentException(string.Format(EXCP_GENERIC_COUNT, type.Name, genericArgsCount), nameof(type));

                cleanType = type.GetGenericTypeDefinition();

                infoByte = (byte)genericArgsCount;

                GenericArguments = new TypeHashCode[genericArgsCount];
                for (int i = 0; i < GenericArguments.Length; i++)
                {
                    GenericArguments[i] = new TypeHashCode(type.GenericTypeArguments[i]);
                }
            }

            HashCode = CalculateHashCode(cleanType);
            HashCode |= (ulong)(infoByte << 64);

            if (!TypeLookupTable.ContainsKey(this))
                TypeLookupTable.Add(this, type);
        }

        #endregion


        #region STATIC

        /// <summary>
        /// Convert a TypeHashCode into its ulong representation.
        /// </summary>
        /// <param name="typeHashCode">The <see cref="TypeHashCode"/> to convert.</param>
        public static explicit operator ulong(TypeHashCode typeHashCode)
        {
            return typeHashCode.HashCode;
        }

        /// <summary>
        /// Calculate the ulong hashcode of <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type to get the ulong hashcode of.</param>
        /// <returns>The ulong hashcode of <paramref name="type"/>.</returns>
        private static ulong CalculateHashCode(Type type)
        {
            ulong hashcode = 697857195361211UL;

            foreach (char c in type.FullName)
            {
                hashcode ^= 713447423515501UL;
                hashcode += c * 561180837325049UL;
            }
            hashcode &= MASK;
            return hashcode;
        }

        /// <summary>
        /// Parse the Type from a <see cref="TypeHashCode"/>.
        /// </summary>
        /// <param name="typeHashCode">The <see cref="TypeHashCode"/> to parse.</param>
        /// <returns>The Type that was parsed.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no Type could be parsed.</exception>
        public static Type ParseType(TypeHashCode typeHashCode)
        {
            if (!TypeLookupTable.ContainsKey(typeHashCode))
            {
                ulong masked = typeHashCode.HashCode & MASK;
                Type foundType = null;

                foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
                    foreach (Type type in ass.GetTypes())
                        if (CalculateHashCode(type).Equals(masked))
                            foundType = type;

                if (foundType == null)
                    throw new InvalidOperationException(EXCP_PARSE_FAILED);

                if (typeHashCode.IsGenericType)
                {
                    Type[] genericArgs = new Type[typeHashCode.GenericArgumentCount];
                    for (int i = 0; i < typeHashCode.GenericArgumentCount; i++)
                    {
                        genericArgs[i] = ParseType(typeHashCode.GenericArguments[i]);
                    }
                    foundType = foundType.MakeGenericType(genericArgs);
                }
                else if (typeHashCode.IsArrayType)
                {
                    foundType = foundType.MakeArrayType(typeHashCode.ArrayRank);
                }

                TypeLookupTable.Add(typeHashCode, foundType);
            }
            return TypeLookupTable[typeHashCode];
        }

        #endregion


        #region OVERRIDES

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is TypeHashCode thc)
            {
                if (thc.HashCode != HashCode)
                    return false;

                if (thc.IsGenericType)
                {
                    bool equal = thc.IsGenericType && IsGenericType;

                    if (thc.GenericArgumentCount != GenericArgumentCount)
                        return false;

                    for (int i = 0; i < thc.GenericArgumentCount; i++)
                        equal &= thc.GenericArguments[i].Equals(GenericArguments[i]);

                    return equal;
                }
                else if (thc.IsArrayType)
                {
                    return thc.IsArrayType && IsArrayType && thc.ArrayRank == ArrayRank;
                }

                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (int)HashCode;
        }

        #endregion


        #region IENUMERABLE INTERFACE

        /// <inheritdoc/>
        public IEnumerator<TypeHashCode> GetEnumerator()
        {
            return (IEnumerator<TypeHashCode>)GenericArguments.GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GenericArguments.GetEnumerator();
        }

        #endregion


        #region OPERATORS

        /// <summary>
        /// Get the generic argument at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index of the generic argument.</param>
        /// <returns>A TypeHashCode instance.</returns>
        public TypeHashCode this[int index] => GenericArguments[index];

        #endregion
    }
}
