using System;
using System.Collections.Generic;

namespace Parcel.Serialization.Binary
{
    internal class PrimitiveArraySerializer<T> : SerializerV2, IBinarySerializer where T : struct
    {
        private static readonly string EXCP_NOT_PRIMITIVE = "Cannot create instance of PrimitiveArraySerializer with non primitive type {0}";

        private static readonly Dictionary<Type, Action<DataWriter, object>> SerializationExpressions = new Dictionary<Type, Action<DataWriter, object>>()
        {
            {typeof(byte), (DataWriter dw, object o) => dw.Write((byte[])o) },
            {typeof(sbyte), (DataWriter dw, object o) => dw.Write((sbyte[])o) },
            {typeof(short), (DataWriter dw, object o) => dw.Write((short[])o) },
            {typeof(ushort), (DataWriter dw, object o) => dw.Write((ushort[])o) },
            {typeof(int), (DataWriter dw, object o) => dw.Write((int[])o) },
            {typeof(uint), (DataWriter dw, object o) => dw.Write((uint[])o) },
            {typeof(long), (DataWriter dw, object o) => dw.Write((long[])o) },
            {typeof(ulong), (DataWriter dw, object o) => dw.Write((ulong[])o) },
            {typeof(float), (DataWriter dw, object o) => dw.Write((float[])o) },
            {typeof(double), (DataWriter dw, object o) => dw.Write((double[])o) },
            {typeof(decimal), (DataWriter dw, object o) => dw.Write((decimal[])o) },
            {typeof(char), (DataWriter dw, object o) => dw.Write((char[])o) },
            {typeof(bool), (DataWriter dw, object o) => dw.Write((bool[])o) },
        };

        private static readonly Dictionary<Type, Func<DataReader, object>> DeserializationExpressions = new Dictionary<Type, Func<DataReader, object>>()
        {
            {typeof(byte), (DataReader dr) => dr.ReadByteArray() },
            {typeof(sbyte), (DataReader dr) => dr.ReadSByteArray() },
            {typeof(short), (DataReader dr) => dr.ReadShortArray() },
            {typeof(ushort), (DataReader dr) => dr.ReadUShortArray() },
            {typeof(int), (DataReader dr) => dr.ReadIntArray() },
            {typeof(uint), (DataReader dr) => dr.ReadUIntArray() },
            {typeof(long), (DataReader dr) => dr.ReadLongArray() },
            {typeof(ulong), (DataReader dr) => dr.ReadULongArray() },
            {typeof(float), (DataReader dr) => dr.ReadFloatArray() },
            {typeof(double), (DataReader dr) => dr.ReadDoubleArray() },
            {typeof(decimal), (DataReader dr) => dr.ReadDecimalArray() },
            {typeof(char), (DataReader dr) => dr.ReadCharArray() },
            {typeof(bool), (DataReader dr) => dr.ReadBooleanArray() },
        };


        public PrimitiveArraySerializer()
        {
            if (!SerializationExpressions.ContainsKey(typeof(T)) || !DeserializationExpressions.ContainsKey(typeof(T)))
                throw new NotSupportedException(string.Format(EXCP_NOT_PRIMITIVE, nameof(T)));
        }

        public override object Deserialize(DataReader reader)
        {
            return DeserializationExpressions[typeof(T)](reader);
        }

        public override void Serialize(DataWriter writer, object obj)
        {
            SerializationExpressions[typeof(T)](writer, obj);
        }

        public override bool CanSerialize(Type type)
        {
            return type.IsArray && type.GetElementType().IsPrimitive && type.GetArrayRank() == 1;
        }
    }
}
