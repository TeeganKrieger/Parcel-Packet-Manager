using System;
using System.Collections.Generic;

namespace Parcel.Serialization.Binary
{
    internal class PrimitiveSerializer<T> : SerializerV2, IBinarySerializer where T : struct
    {
        private static readonly string EXCP_NOT_PRIMITIVE = "Cannot create instance of PrimitiveSerializer with non primitive type {0}";

        private static readonly Dictionary<Type, Action<DataWriter, object>> SerializationExpressions = new Dictionary<Type, Action<DataWriter, object>>()
        {
            {typeof(byte), (DataWriter dw, object o) => dw.Write((byte)o) },
            {typeof(sbyte), (DataWriter dw, object o) => dw.Write((sbyte)o) },
            {typeof(short), (DataWriter dw, object o) => dw.Write((short)o) },
            {typeof(ushort), (DataWriter dw, object o) => dw.Write((ushort)o) },
            {typeof(int), (DataWriter dw, object o) => dw.Write((int)o) },
            {typeof(uint), (DataWriter dw, object o) => dw.Write((uint)o) },
            {typeof(long), (DataWriter dw, object o) => dw.Write((long)o) },
            {typeof(ulong), (DataWriter dw, object o) => dw.Write((ulong)o) },
            {typeof(float), (DataWriter dw, object o) => dw.Write((float)o) },
            {typeof(double), (DataWriter dw, object o) => dw.Write((double)o) },
            {typeof(decimal), (DataWriter dw, object o) => dw.Write((decimal)o) },
            {typeof(char), (DataWriter dw, object o) => dw.Write((char)o) },
            {typeof(bool), (DataWriter dw, object o) => dw.Write((bool)o) },
        };

        private static readonly Dictionary<Type, Func<DataReader, object>> DeserializationExpressions = new Dictionary<Type, Func<DataReader, object>>()
        {
            {typeof(byte), (DataReader dr) => dr.ReadByte() },
            {typeof(sbyte), (DataReader dr) => dr.ReadSByte() },
            {typeof(short), (DataReader dr) => dr.ReadShort() },
            {typeof(ushort), (DataReader dr) => dr.ReadUShort() },
            {typeof(int), (DataReader dr) => dr.ReadInt() },
            {typeof(uint), (DataReader dr) => dr.ReadUInt() },
            {typeof(long), (DataReader dr) => dr.ReadLong() },
            {typeof(ulong), (DataReader dr) => dr.ReadULong() },
            {typeof(float), (DataReader dr) => dr.ReadFloat() },
            {typeof(double), (DataReader dr) => dr.ReadDouble() },
            {typeof(decimal), (DataReader dr) => dr.ReadDecimal() },
            {typeof(char), (DataReader dr) => dr.ReadChar() },
            {typeof(bool), (DataReader dr) => dr.ReadBoolean() },
        };

        public PrimitiveSerializer()
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
            return type.IsPrimitive;
        }
    }
}
