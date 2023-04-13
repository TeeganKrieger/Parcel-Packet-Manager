using Parcel.DataStructures;
using Parcel.Lib;
using Parcel.Networking;
using Parcel.Packets;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Parcel.Serialization
{
    /// <summary>
    /// Represents a collection of useful information for serialization purposes about a Type.
    /// </summary>
    public sealed class ObjectCache : IEnumerable<ObjectProperty>
    {
        private const string EXCP_INVALID_HASH = "Cannot get object cache. No cache found with hash {0}.";

        private static ConcurrentDictionary<Type, ObjectCache> ObjectCachesByType = new ConcurrentDictionary<Type, ObjectCache>();
        private static ConcurrentDictionary<TypeHashCode, ObjectCache> ObjectCachesByHash = new ConcurrentDictionary<TypeHashCode, ObjectCache>();

        private Dictionary<string, ObjectProperty> _propertiesByName;
        private Dictionary<uint, ObjectProperty> _propertiesByHash;
        private Dictionary<string, ObjectProcedure> _proceduresByName;
        private Dictionary<ulong, ObjectProcedure> _proceduresByHash;
        private Dictionary<Type, List<Attribute>> _attributes;

        /// <summary>
        /// The Type this ObjectCache is for.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// The name of the Type.
        /// </summary>
        public string Name => Type.Name;

        /// <summary>
        /// The <see cref="TypeHashCode"/> of the Type.
        /// </summary>
        public TypeHashCode HashCode { get; private set; }


        #region CONSTRUCTOR

        /// <summary>
        /// Construct a new instance of ObjectCache.
        /// </summary>
        /// <param name="type">The Type the ObjectCache is for.</param>
        /// <param name="propertiesByHash">A dictionary of <see cref="ObjectProperty"/> instances stored by their name hash codes.</param>
        /// <param name="propertiesByName">A dictionary of <see cref="ObjectProperty"/> instances stored by their names.</param>
        /// <param name="attributes">A dictionary of Attributes that this Type has, stored by the Attribute Type.</param>
        private ObjectCache(Type type, Dictionary<uint, ObjectProperty> propertiesByHash,
            Dictionary<string, ObjectProperty> propertiesByName, Dictionary<ulong, ObjectProcedure> proceduresByHash,
            Dictionary<string, ObjectProcedure> proceduresByName, Dictionary<Type, List<Attribute>> attributes)
        {
            Type = type;
            HashCode = type.GetTypeHashCode();
            _propertiesByHash = propertiesByHash;
            _propertiesByName = propertiesByName;
            _proceduresByHash = proceduresByHash;
            _proceduresByName = proceduresByName;
            _attributes = attributes;
        }

        #endregion


        #region INSTANCE ACCESS

        /// <inheritdoc/>
        public IEnumerator<ObjectProperty> GetEnumerator()
        {
            return _propertiesByHash.Values.GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _propertiesByHash.Values.GetEnumerator();
        }

        /// <summary>
        /// Get a <see cref="ObjectProperty"/> instance using its name hash code.
        /// </summary>
        /// <param name="propertyHash">The name hash code to use.</param>
        /// <returns>An <see cref="ObjectProperty"/> instance.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if no value with the key <paramref name="propertyHash"/> was found.</exception>
        public ObjectProperty GetProperty(uint propertyHash)
        {
            if (!_propertiesByHash.ContainsKey(propertyHash))
                throw new KeyNotFoundException();
            return _propertiesByHash[propertyHash];
        }

        /// <summary>
        /// Get a <see cref="ObjectProperty"/> instance using its name.
        /// </summary>
        /// <param name="propertyName">The name to use.</param>
        /// <returns>An <see cref="ObjectProperty"/> instance.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if no value with the key <paramref name="propertyName"/> was found.</exception>
        public ObjectProperty GetProperty(string propertyName)
        {
            if (!_propertiesByName.ContainsKey(propertyName))
                throw new KeyNotFoundException();
            return _propertiesByName[propertyName];
        }

        /// <summary>
        /// Get an Enumerator that iterates over all properties in this <see cref="ObjectCache"/>.
        /// </summary>
        /// <returns>An Enumerator that iterates over all properties in this <see cref="ObjectCache"/>.</returns>
        public IEnumerator<ObjectProperty> GetProperties()
        {
            return _propertiesByHash.Values.GetEnumerator();
        }

        /// <summary>
        /// Get a <see cref="ObjectProcedure"/> instance using its hash code.
        /// </summary>
        /// <param name="procedureHash">The hash code to use.</param>
        /// <returns>An <see cref="ObjectProcedure"/> instance.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if no value with the key <paramref name="procedureHash"/> was found.</exception>
        public ObjectProcedure GetProcedure(ulong procedureHash)
        {
            if (!_proceduresByHash.ContainsKey(procedureHash))
                throw new KeyNotFoundException();
            return _proceduresByHash[procedureHash];
        }

        /// <summary>
        /// Get a <see cref="ObjectProcedure"/> instance using its name.
        /// </summary>
        /// <param name="procedureName">The name to use.</param>
        /// <returns>An <see cref="ObjectProcedure"/> instance.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if no value with the key <paramref name="procedureName"/> was found.</exception>
        public ObjectProcedure GetProcedure(string procedureName)
        {
            if (!_proceduresByName.ContainsKey(procedureName))
                throw new KeyNotFoundException();
            return _proceduresByName[procedureName];
        }

        /// <summary>
        /// Get an Enumerator that iterates over all procedures in this <see cref="ObjectCache"/>.
        /// </summary>
        /// <returns>An Enumerator that iterates over all procedures in this <see cref="ObjectCache"/>.</returns>
        public IEnumerator<ObjectProcedure> GetProcedures()
        {
            return _proceduresByHash.Values.GetEnumerator();
        }

        /// <summary>
        /// Get an Attribute of this Type, if it exists.
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
        /// Get all Attributes of Type <typeparamref name="T"/> belonging to this Type, if they exists.
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


        #region STATIC ACCESS

        /// <summary>
        /// Get or construct an ObjectCache instance from Type <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of ObjectCache to get or construct.</param>
        /// <returns>An ObjectCache instance.</returns>
        public static ObjectCache FromType(Type type)
        {
            ObjectCache storedCache;
            while (!ObjectCachesByType.TryGetValue(type, out storedCache))
            {
                Console.WriteLine($"Constructing New Cache From Type {type.FullName}");
                ObjectCache cache = GenerateCache(type);
                ObjectCachesByType.TryAdd(type, cache);
                ObjectCachesByHash.TryAdd(type.GetTypeHashCode(), cache);
            }
            return storedCache;
        }

        /// <summary>
        /// Get or construct an ObjectCache instance from a <see cref="TypeHashCode"/>.
        /// </summary>
        /// <param name="hash">The <see cref="TypeHashCode"/> to use.</param>
        /// <returns>An ObjectCache instance.</returns>
        /// <exception cref="ArgumentException">Thrown if no Type could be derived from <paramref name="hash"/>. This usually indicated data corruption.</exception>
        public static ObjectCache FromHash(TypeHashCode hash)
        {
            ObjectCache storedCache;
            while (!ObjectCachesByHash.TryGetValue(hash, out storedCache))
            {
                Console.WriteLine($"Constructing New Cache From Hash {hash}");
                Type type = TypeHashCode.ParseType(hash);
                if (type == null)
                    throw new ArgumentException(string.Format(EXCP_INVALID_HASH, hash), nameof(hash));

                ObjectCache cache = GenerateCache(type);
                ObjectCachesByType.TryAdd(type, cache);
                ObjectCachesByHash.TryAdd(type.GetTypeHashCode(), cache);
            }
            return storedCache;
        }

        #endregion


        #region CACHE GENERATION

        /// <summary>
        /// Generate a new instance of ObjectCache from <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The Type to generate an ObjectCache for.</param>
        /// <returns>The new ObjectCache instance.</returns>
        private static ObjectCache GenerateCache(Type type)
        {
            bool optIn = false;

            if (type.GetCustomAttribute<OptInAttribute>() != null)
                optIn = true;

            //Create objects needed to initialize ObjectCache

            //Cache Properties
            Dictionary<uint, ObjectProperty> propertiesByHash = new Dictionary<uint, ObjectProperty>();
            Dictionary<string, ObjectProperty> propertiesByName = new Dictionary<string, ObjectProperty>();

            PropertyInfo[] propertiesArray = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (PropertyInfo property in propertiesArray)
            {
                //Skip properties with the ignore attribute
                if (property.GetCustomAttribute<IgnoreAttribute>() != null)
                    continue;
                else
                {
                    if (optIn)
                    {
                        if (property.GetCustomAttribute<SerializeAttribute>() != null && ObjectProperty.TryCreate(property, out ObjectProperty objectProperty))
                        {
                            propertiesByHash.Add(property.GetPropertyNameHash(), objectProperty);
                            propertiesByName.Add(property.Name, objectProperty);
                        }
                    }
                    else
                    {
                        if (ObjectProperty.TryCreate(property, out ObjectProperty objectProperty))
                        {
                            propertiesByHash.Add(property.GetPropertyNameHash(), objectProperty);
                            propertiesByName.Add(property.Name, objectProperty);
                        }
                    }
                }
            }

            //Cache Procedures
            Dictionary<ulong, ObjectProcedure> proceduresByHash = new Dictionary<ulong, ObjectProcedure>();
            Dictionary<string, ObjectProcedure> proceduresByName = new Dictionary<string, ObjectProcedure>();

            MethodInfo[] methodsArray = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (MethodInfo methodInfo in methodsArray)
            {
                if (methodInfo.GetCustomAttribute<RPCAttribute>() == null)
                    continue;
                else if (ObjectProcedure.TryCreate(methodInfo, out ObjectProcedure objectProcedure))
                {
                    proceduresByHash.Add((ulong)methodInfo.GetProcedureHash(), objectProcedure);
                }
            }

            //Cache Attributes
            Attribute[] allAttributes = type.GetCustomAttributes().ToArray();

            Dictionary<Type, List<Attribute>> sortedAttributes = new Dictionary<Type, List<Attribute>>();

            foreach (Attribute att in allAttributes)
            {
                Type attType = att.GetType();
                if (!sortedAttributes.ContainsKey(attType))
                    sortedAttributes.Add(attType, new List<Attribute>());
                sortedAttributes[attType].Add(att);
            }

            return new ObjectCache(type, propertiesByHash, propertiesByName, proceduresByHash, proceduresByName, sortedAttributes);
        }

        #endregion

    }
}
