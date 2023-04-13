using System;

namespace Parcel.Serialization.Binary
{
    public class BinarySerializerResolver : SerializerResolverV2
    {
        public static BinarySerializerResolver Default { get; private set; }

        static BinarySerializerResolver()
        {
            Default = new BinarySerializerResolver();

            //Resolved Defaults Here
            Default._resolvedSerializers.TryAdd(typeof(byte), new PrimitiveSerializer<byte>());
            Default._resolvedSerializers.TryAdd(typeof(sbyte), new PrimitiveSerializer<sbyte>());
            Default._resolvedSerializers.TryAdd(typeof(short), new PrimitiveSerializer<short>());
            Default._resolvedSerializers.TryAdd(typeof(ushort), new PrimitiveSerializer<ushort>());
            Default._resolvedSerializers.TryAdd(typeof(int), new PrimitiveSerializer<int>());
            Default._resolvedSerializers.TryAdd(typeof(uint), new PrimitiveSerializer<uint>());
            Default._resolvedSerializers.TryAdd(typeof(long), new PrimitiveSerializer<long>());
            Default._resolvedSerializers.TryAdd(typeof(ulong), new PrimitiveSerializer<ulong>());
            Default._resolvedSerializers.TryAdd(typeof(float), new PrimitiveSerializer<float>());
            Default._resolvedSerializers.TryAdd(typeof(double), new PrimitiveSerializer<double>());
            Default._resolvedSerializers.TryAdd(typeof(decimal), new PrimitiveSerializer<decimal>());
            Default._resolvedSerializers.TryAdd(typeof(bool), new PrimitiveSerializer<bool>());
            Default._resolvedSerializers.TryAdd(typeof(char), new PrimitiveSerializer<char>());

            Default._resolvedSerializers.TryAdd(typeof(byte[]), new PrimitiveArraySerializer<byte>());
            Default._resolvedSerializers.TryAdd(typeof(sbyte[]), new PrimitiveArraySerializer<sbyte>());
            Default._resolvedSerializers.TryAdd(typeof(short[]), new PrimitiveArraySerializer<short>());
            Default._resolvedSerializers.TryAdd(typeof(ushort[]), new PrimitiveArraySerializer<ushort>());
            Default._resolvedSerializers.TryAdd(typeof(int[]), new PrimitiveArraySerializer<int>());
            Default._resolvedSerializers.TryAdd(typeof(uint[]), new PrimitiveArraySerializer<uint>());
            Default._resolvedSerializers.TryAdd(typeof(long[]), new PrimitiveArraySerializer<long>());
            Default._resolvedSerializers.TryAdd(typeof(ulong[]), new PrimitiveArraySerializer<ulong>());
            Default._resolvedSerializers.TryAdd(typeof(float[]), new PrimitiveArraySerializer<float>());
            Default._resolvedSerializers.TryAdd(typeof(double[]), new PrimitiveArraySerializer<double>());
            Default._resolvedSerializers.TryAdd(typeof(decimal[]), new PrimitiveArraySerializer<decimal>());
            Default._resolvedSerializers.TryAdd(typeof(bool[]), new PrimitiveArraySerializer<bool>());
            Default._resolvedSerializers.TryAdd(typeof(char[]), new PrimitiveArraySerializer<char>());

            Default._resolvedSerializers.TryAdd(typeof(string), new StringSerializer());
            Default._resolvedSerializers.TryAdd(typeof(string[]), new StringArraySerializer());

            Default._resolvedSerializers.TryAdd(typeof(object), new ObjectSerializer() { ObjectCache = ObjectCache.FromType(typeof(object)) });
            Default._resolvedSerializers.TryAdd(typeof(object[]), new ObjectArraySerializer() { ObjectCache = ObjectCache.FromType(typeof(object[])) });

            Default._resolvedSerializers.TryAdd(typeof(TypeHashCode), new TypeHashCodeSerializer());

            //Registered Defaults Here
            Default.RegisterSerializer(new EnumSerializer());
            Default.RegisterSerializer(new IGenericDictionarySerializer());
            Default.RegisterSerializer(new IGenericCollectionSerializer());
        }

        public override DataReader NewDataReader(byte[] data)
        {
            return new BinaryReader(this, data);
        }

        public override DataWriter NewDataWriter()
        {
            return new BinaryWriter(this);
        }

        public override void RegisterSerializer(SerializerV2 serializer)
        {
            if (serializer == null)
                throw new ArgumentNullException(nameof(serializer));
            if (!typeof(IBinarySerializer).IsAssignableFrom(serializer.GetType()))
                throw new ArgumentException(nameof(serializer));

            base.RegisterSerializer(serializer);
        }
    }
}
