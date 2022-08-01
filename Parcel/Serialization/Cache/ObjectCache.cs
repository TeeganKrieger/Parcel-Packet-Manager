using Parcel.Lib;
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
            Dictionary<string, ObjectProperty> propertiesByName, Dictionary<Type, List<Attribute>> attributes)
        {
            this.Type = type;
            this.HashCode = type.GetTypeHashCode();
            this._propertiesByHash = propertiesByHash;
            this._propertiesByName = propertiesByName;
            this._attributes = attributes;
        }

        #endregion


        #region INSTANCE ACCESS

        /// <summary>
        /// Get a <see cref="ObjectProperty"/> instance using its name hash code.
        /// </summary>
        /// <param name="propertyHash">The name hash code to use.</param>
        /// <returns>An <see cref="ObjectProperty"/> instance.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if no value with the key <paramref name="propertyHash"/> was found.</exception>
        public ObjectProperty this[uint propertyHash]
        {
            get
            {
                if (!this._propertiesByHash.ContainsKey(propertyHash))
                    throw new KeyNotFoundException();
                return this._propertiesByHash[propertyHash];
            }
        }

        /// <summary>
        /// Get a <see cref="ObjectProperty"/> instance using its name.
        /// </summary>
        /// <param name="propertyName">The name to use.</param>
        /// <returns>An <see cref="ObjectProperty"/> instance.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if no value with the key <paramref name="propertyHash"/> was found.</exception>
        public ObjectProperty this[string propertyName]
        {
            get
            {
                if (!this._propertiesByName.ContainsKey(propertyName))
                    throw new KeyNotFoundException();
                return this._propertiesByName[propertyName];
            }
        }

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
        /// Get an Attribute of this Type, if it exists.
        /// </summary>
        /// <typeparam name="T">The Type of Attribute to get.</typeparam>
        /// <returns>The Attribute instance if it exists; otherwise, <see langword="null"/>.</returns>
        public T GetCustomAttribute<T>() where T : Attribute
        {
            if (!this._attributes.ContainsKey(typeof(T)))
                return null;
            return (T)this._attributes[typeof(T)].FirstOrDefault();
        }

        /// <summary>
        /// Get all Attributes of Type <typeparamref name="T"/> belonging to this Type, if they exists.
        /// </summary>
        /// <typeparam name="T">The Type of Attribute to get.</typeparam>
        /// <returns>An array of Attribute instances if they exist; otherwise, <see langword="null"/>.</returns>
        public T[] GetCustomAttributes<T>() where T : Attribute
        {
            if (!this._attributes.ContainsKey(typeof(T)))
                return null;
            return (T[])this._attributes[typeof(T)].ToArray();
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
            if (!ObjectCachesByType.ContainsKey(type))
            {
                ObjectCache cache = GenerateCache(type);
                ObjectCachesByType.TryAdd(type, cache);
                ObjectCachesByHash.TryAdd(type.GetTypeHashCode(), cache);
            }
            return ObjectCachesByType[type];
        }

        /// <summary>
        /// Get or construct an ObjectCache instance from a <see cref="TypeHashCode"/>.
        /// </summary>
        /// <param name="hash">The <see cref="TypeHashCode"/> to use.</param>
        /// <returns>An ObjectCache instance.</returns>
        /// <exception cref="ArgumentException">Thrown if no Type could be derived from <paramref name="hash"/>. This usually indicated data corruption.</exception>
        public static ObjectCache FromHash(TypeHashCode hash)
        {
            if (!ObjectCachesByHash.ContainsKey(hash))
            {
                Type type = TypeHashCode.ParseType(hash);
                if (type == null)
                    throw new ArgumentException(string.Format(EXCP_INVALID_HASH, hash), nameof(hash));

                ObjectCache cache = GenerateCache(type);
                ObjectCachesByType.TryAdd(type, cache);
                ObjectCachesByHash.TryAdd(type.GetTypeHashCode(), cache);
            }
            return ObjectCachesByHash[hash];
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

            Attribute[] allAttributes = type.GetCustomAttributes().ToArray();

            Dictionary<Type, List<Attribute>> sortedAttributes = new Dictionary<Type, List<Attribute>>();

            foreach (Attribute att in allAttributes)
            {
                Type attType = att.GetType();
                if (!sortedAttributes.ContainsKey(attType))
                    sortedAttributes.Add(attType, new List<Attribute>());
                sortedAttributes[attType].Add(att);
            }

            return new ObjectCache(type, propertiesByHash, propertiesByName, sortedAttributes);
        }

        #endregion

    }
}
