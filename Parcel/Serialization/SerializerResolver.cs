using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Parcel.Serialization
{
    /// <summary>
    /// Stored a collection of <see cref="Serializer">Serializers</see> and resolves which <see cref="Serializer">Serializers</see> should be used
    /// for each Type.
    /// </summary>
    /// <remarks>
    /// <see cref="Serializer">Serializers</see> that are registered with the SerializerResolver class will be stored. When an unseen type attempts to serialize,
    /// the SerializerResolver will check each registered <see cref="Serializer"/> and find the first one to return <see langword="true"/> from the
    /// <see cref="Serializer.CanSerialize(Type)"/> call. This <see cref="Serializer"/> will be cloned and the appropriate <see cref="ObjectCache"/> instance will be placed
    /// into the <see cref="Serializer"/>.
    /// </remarks>
    public class SerializerResolver
    {
        /// <summary>
        /// A global default instance of SerializerResolver that can be fallen back upon.
        /// </summary>
        public static SerializerResolver Global { get; private set; }

        private ConcurrentDictionary<Type, Serializer> _resolvedSerializers = new ConcurrentDictionary<Type, Serializer>()
        {
            
        };

        private List<Serializer> _registeredSerializers = new List<Serializer>() { new EnumSerializer(), new IGenericDictionarySerializer(), new IGenericCollectionSerializer() };

        #region CONSTRUCTOR

        static SerializerResolver()
        {
            Global = new SerializerResolver();
        }

        /// <summary>
        /// Construct a new instance of SerializerResolver.
        /// </summary>
        public SerializerResolver()
        {
            //Add all built in serializers
            this._resolvedSerializers.TryAdd(typeof(byte), new PrimitiveSerializer<byte>());
            this._resolvedSerializers.TryAdd(typeof(sbyte), new PrimitiveSerializer<sbyte>());
            this._resolvedSerializers.TryAdd(typeof(short), new PrimitiveSerializer<short>());
            this._resolvedSerializers.TryAdd(typeof(ushort), new PrimitiveSerializer<ushort>());
            this._resolvedSerializers.TryAdd(typeof(int), new PrimitiveSerializer<int>());
            this._resolvedSerializers.TryAdd(typeof(uint), new PrimitiveSerializer<uint>());
            this._resolvedSerializers.TryAdd(typeof(long), new PrimitiveSerializer<long>());
            this._resolvedSerializers.TryAdd(typeof(ulong), new PrimitiveSerializer<ulong>());
            this._resolvedSerializers.TryAdd(typeof(float), new PrimitiveSerializer<float>());
            this._resolvedSerializers.TryAdd(typeof(double), new PrimitiveSerializer<double>());
            this._resolvedSerializers.TryAdd(typeof(decimal), new PrimitiveSerializer<decimal>());
            this._resolvedSerializers.TryAdd(typeof(char), new PrimitiveSerializer<char>());
            this._resolvedSerializers.TryAdd(typeof(bool), new PrimitiveSerializer<bool>());

            this._resolvedSerializers.TryAdd(typeof(byte[]), new PrimitiveArraySerializer<byte>());
            this._resolvedSerializers.TryAdd(typeof(sbyte[]), new PrimitiveArraySerializer<sbyte>());
            this._resolvedSerializers.TryAdd(typeof(short[]), new PrimitiveArraySerializer<short>());
            this._resolvedSerializers.TryAdd(typeof(ushort[]), new PrimitiveArraySerializer<ushort>());
            this._resolvedSerializers.TryAdd(typeof(int[]), new PrimitiveArraySerializer<int>());
            this._resolvedSerializers.TryAdd(typeof(uint[]), new PrimitiveArraySerializer<uint>());
            this._resolvedSerializers.TryAdd(typeof(long[]), new PrimitiveArraySerializer<long>());
            this._resolvedSerializers.TryAdd(typeof(ulong[]), new PrimitiveArraySerializer<ulong>());
            this._resolvedSerializers.TryAdd(typeof(float[]), new PrimitiveArraySerializer<float>());
            this._resolvedSerializers.TryAdd(typeof(double[]), new PrimitiveArraySerializer<double>());
            this._resolvedSerializers.TryAdd(typeof(decimal[]), new PrimitiveArraySerializer<decimal>());
            this._resolvedSerializers.TryAdd(typeof(char[]), new PrimitiveArraySerializer<char>());
            this._resolvedSerializers.TryAdd(typeof(bool[]), new PrimitiveArraySerializer<bool>());

            this._resolvedSerializers.TryAdd(typeof(string), new StringSerializer());
            this._resolvedSerializers.TryAdd(typeof(object), new ObjectSerializer() { ObjectCache = ObjectCache.FromType(typeof(object)) });
            this._resolvedSerializers.TryAdd(typeof(TypeHashCode), new TypeHashCodeSerializer());
        }

        #endregion


        #region METHODS

        /// <summary>
        /// Register a <see cref="Serializer"/> with this SerializerResolver.
        /// </summary>
        /// <param name="serializer">The <see cref="Serializer"/> to register.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="serializer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if this SerializerResolver has already registered <paramref name="serializer"/>.</exception>
        public void RegisterSerializer(Serializer serializer)
        {
            if (serializer == null)
                throw new ArgumentNullException(nameof(serializer));
            if (_registeredSerializers.Contains(serializer))
                throw new ArgumentException();

            _registeredSerializers.Add(serializer);
        }

        /// <summary>
        /// Unregister a <see cref="Serializer"/> with this SerializerResolver.
        /// </summary>
        /// <param name="serializer">The <see cref="Serializer"/> to unregister.</param>
        public void UnRegisterSerializer(Serializer serializer)
        {
            _registeredSerializers.Remove(serializer);
        }

        /// <summary>
        /// Get or Resolve the <see cref="Serializer"/> to use with Type <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The Type to get or resolve a <see cref="Serializer"/> for.</param>
        /// <returns>The appropriate <see cref="Serializer"/>.</returns>
        public Serializer GetSerializer(Type type)
        {
            if (!_resolvedSerializers.ContainsKey(type))
            {
                foreach (Serializer serializer in _registeredSerializers)
                    if (serializer.CanSerialize(type))
                    {
                        Serializer newSerializer = (Serializer)serializer.Clone();
                        ObjectCache cache = ObjectCache.FromType(type);
                        newSerializer.ObjectCache = cache;

                        _resolvedSerializers.TryAdd(type, newSerializer);
                        break;
                    }
            }
            if (!_resolvedSerializers.ContainsKey(type))
            {
                Serializer newSerializer = (Serializer)_resolvedSerializers[typeof(object)].Clone();
                ObjectCache cache = ObjectCache.FromType(type);
                newSerializer.ObjectCache = cache;

                _resolvedSerializers.TryAdd(type, newSerializer);
            }
            return _resolvedSerializers[type];
        }

        #endregion

    }
}
