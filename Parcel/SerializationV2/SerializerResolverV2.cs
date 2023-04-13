using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public abstract class SerializerResolverV2 : ICloneable
    {
        protected ConcurrentDictionary<Type, SerializerV2> _resolvedSerializers = new ConcurrentDictionary<Type, SerializerV2>();

        protected List<SerializerV2> _registeredSerializers = new List<SerializerV2>() { };


        #region CONSTRUCTOR

        /// <summary>
        /// Construct a new instance of SerializerResolver.
        /// </summary>
        public SerializerResolverV2()
        {

        }

        #endregion


        #region ABSTRACT METHODS

        public abstract DataWriter NewDataWriter();
        public abstract DataReader NewDataReader(byte[] data);

        #endregion


        #region METHODS

        /// <summary>
        /// Register a <see cref="Serializer"/> with this SerializerResolver.
        /// </summary>
        /// <param name="serializer">The <see cref="Serializer"/> to register.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="serializer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if this SerializerResolver has already registered <paramref name="serializer"/>.</exception>
        public virtual void RegisterSerializer(SerializerV2 serializer)
        {
            if (serializer == null)
                throw new ArgumentNullException(nameof(serializer));
            if (_registeredSerializers.Contains(serializer))
                throw new ArgumentException();

            _registeredSerializers.Add(serializer);
        }

        /// <summary>
        /// Get or Resolve the <see cref="Serializer"/> to use with Type <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The Type to get or resolve a <see cref="Serializer"/> for.</param>
        /// <returns>The appropriate <see cref="Serializer"/>.</returns>
        public SerializerV2 GetSerializer(Type type)
        {
            if (!_resolvedSerializers.ContainsKey(type))
            {
                foreach (SerializerV2 serializer in _registeredSerializers)
                    if (serializer.CanSerialize(type))
                    {
                        SerializerV2 newSerializer = (SerializerV2)serializer.Clone();
                        ObjectCache cache = ObjectCache.FromType(type);
                        newSerializer.ObjectCache = cache;

                        _resolvedSerializers.TryAdd(type, newSerializer);
                        break;
                    }
            }
            if (!_resolvedSerializers.ContainsKey(type))
            {
                SerializerV2 newSerializer;
                if (type.IsArray)
                {
                    newSerializer = (SerializerV2)_resolvedSerializers[typeof(object[])].Clone();
                }
                else
                {
                    newSerializer = (SerializerV2)_resolvedSerializers[typeof(object)].Clone();
                }

                ObjectCache cache = ObjectCache.FromType(type);
                newSerializer.ObjectCache = cache;

                _resolvedSerializers.TryAdd(type, newSerializer);
            }
            return _resolvedSerializers[type];
        }

        public IEnumerable<SerializerV2> GetRegisteredSerializers()
        {
            foreach (SerializerV2 serializer in this._registeredSerializers)
                yield return serializer;
        }

        public IEnumerable<SerializerV2> GetResolvedSerializers()
        {
            foreach (SerializerV2 serializer in this._resolvedSerializers.Values)
                yield return serializer;
        }


        #endregion


        #region ICLONABLE IMPLEMENTATION

        public object Clone()
        {
            ConcurrentDictionary<Type, SerializerV2> resolvedSerializers = new ConcurrentDictionary<Type, SerializerV2>();
            List<SerializerV2> registeredSerializers = new List<SerializerV2>();

            foreach (SerializerV2 serializer in this._registeredSerializers)
                registeredSerializers.Add((SerializerV2)serializer.Clone());

            foreach (Type type in resolvedSerializers.Keys)
                if (this._resolvedSerializers.TryGetValue(type, out SerializerV2 serializer))
                    resolvedSerializers.TryAdd(type, (SerializerV2)serializer.Clone());

            SerializerResolverV2 newResolver = (SerializerResolverV2)this.MemberwiseClone();
            newResolver._resolvedSerializers = resolvedSerializers;
            newResolver._registeredSerializers = registeredSerializers;

            return newResolver;
        }

        #endregion

    }
}
