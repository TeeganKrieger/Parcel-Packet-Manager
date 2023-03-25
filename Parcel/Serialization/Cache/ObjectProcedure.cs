using Parcel.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Parcel.DataStructures;
using Parcel.Packets;

namespace Parcel.Serialization
{
    /// <summary>
    /// Represents a collection of useful information for serialization purposes about a Procedure.
    /// </summary>
    public class ObjectProcedure
    {
        private Dictionary<Type, List<Attribute>> _attributes;

        /// <summary>
        /// The MethodInfo instance of this procedure.
        /// </summary>
        public MethodInfo MethodInfo { get; private set; }

        /// <summary>
        /// The name of the procedure.
        /// </summary>
        public string Name => MethodInfo.Name;

        /// <summary>
        /// Whether the procedure is static or not.
        /// </summary>
        public bool IsStatic => MethodInfo.IsStatic;

        /// <summary>
        /// The declaring type of this procedure.
        /// </summary>
        public Type DeclaringType => MethodInfo.DeclaringType;

        /// <summary>
        /// The hash code of the procedure.
        /// </summary>
        public ProcedureHashCode HashCode { get; private set; }

        /// <summary>
        /// A delegate capable of executing the procedure.
        /// </summary>
        internal QuickDelegate Delegate { get; private set; }


        #region CONSTRUCTOR

        /// <summary>
        /// Construct a new instance of ObjectProcedure.
        /// </summary>
        /// <param name="methodInfo">The <see cref="System.Reflection.MethodInfo"/> of this Procedure.</param>
        /// <param name="hash">The procedure hash code.</param>
        private ObjectProcedure(MethodInfo methodInfo, ProcedureHashCode hash, QuickDelegate delegte, Dictionary<Type, List<Attribute>> attributes)
        {
            MethodInfo = methodInfo;
            HashCode = hash;
            Delegate = delegte;
            _attributes = attributes;
        }

        #endregion


        #region STATIC CREATION

        /// <summary>
        /// Attempts to create an instance of ObjectProcedure using <paramref name="methodInfo"/>.
        /// </summary>
        /// <param name="methodInfo">The MethodInfo to construct an ObjectProcedure instance from.</param>
        /// <param name="objectProcedure">The ObjectProcedure instance that was created.</param>
        /// <returns><see langword="true"/> if the ObjectProcedure was successfully created; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// ObjectProcedure creation will fail if the method provided does not have the <see cref="RPCAttribute"/>.
        /// </remarks>
        public static bool TryCreate(MethodInfo methodInfo, out ObjectProcedure objectProcedure)
        {
            Attribute[] allAttributes = methodInfo.GetCustomAttributes().ToArray();

            Dictionary<Type, List<Attribute>> sortedAttributes = new Dictionary<Type, List<Attribute>>();

            bool hasRPCAttribute = false;

            foreach (Attribute att in allAttributes)
            {
                Type attType = att.GetType();

                if (attType == typeof(RPCAttribute))
                    hasRPCAttribute = true;

                if (!sortedAttributes.ContainsKey(attType))
                    sortedAttributes.Add(attType, new List<Attribute>());
                sortedAttributes[attType].Add(att);
            }

            if (hasRPCAttribute && (methodInfo.IsStatic || typeof(SyncedObject).IsAssignableFrom(methodInfo.DeclaringType)))
            {
                objectProcedure = new ObjectProcedure(methodInfo, methodInfo.GetProcedureHash(), methodInfo.Bind(), sortedAttributes);
                return true;
            }
            else
            {
                objectProcedure = null;
                return false;
            }
        }

        #endregion


        #region INSTANCE ACCESS

        /// <summary>
        /// Get an Attribute of this Procedure, if it exists.
        /// </summary>
        /// <typeparam name="T">The Type of Attribute to get.</typeparam>
        /// <returns>The Attribute instance if it exists; otherwise, <see langword="null"/>.</returns>
        public T GetCustomAttribute<T>() where T : Attribute
        {
            if (!_attributes.ContainsKey(typeof(T)))
                return null;
            return (T)_attributes[typeof(T)].FirstOrDefault();
        }

        /// <summary>
        /// Get all Attributes of Type <typeparamref name="T"/> belonging to this Procedure, if they exists.
        /// </summary>
        /// <typeparam name="T">The Type of Attribute to get.</typeparam>
        /// <returns>An array of Attribute instances if they exist; otherwise, <see langword="null"/>.</returns>
        public T[] GetCustomAttributes<T>() where T : Attribute
        {
            if (!_attributes.ContainsKey(typeof(T)))
                return null;
            return (T[])_attributes[typeof(T)].ToArray();
        }

        #endregion
    }
}
