using System;
using System.Collections.Generic;

namespace Parcel.Serialization
{
    internal class PrimitiveSerializer<T> : Serializer where T : struct
    {
        private static readonly string EXCP_NOT_PRIMITIVE = "Cannot create instance of PrimitiveSerializer with non primitive type {0}";

        private static readonly Dictionary<Type, Action<ByteWriter, object>> _serializationExpressions = new Dictionary<Type, Action<ByteWriter, object>>()
        {
            {typeof(byte), (ByteWriter bw, object o) => bw.Write((byte)o) },
            {typeof(sbyte), (ByteWriter bw, object o) => bw.Write((sbyte)o) },
            {typeof(short), (ByteWriter bw, object o) => bw.Write((short)o) },
            {typeof(ushort), (ByteWriter bw, object o) => bw.Write((ushort)o) },
            {typeof(int), (ByteWriter bw, object o) => bw.Write((int)o) },
            {typeof(uint), (ByteWriter bw, object o) => bw.Write((uint)o) },
            {typeof(long), (ByteWriter bw, object o) => bw.Write((long)o) },
            {typeof(ulong), (ByteWriter bw, object o) => bw.Write((ulong)o) },
            {typeof(float), (ByteWriter bw, object o) => bw.Write((float)o) },
            {typeof(double), (ByteWriter bw, object o) => bw.Write((double)o) },
            {typeof(decimal), (ByteWriter bw, object o) => bw.Write((decimal)o) },
            {typeof(char), (ByteWriter bw, object o) => bw.Write((char)o) },
            {typeof(bool), (ByteWriter bw, object o) => bw.Write((bool)o) },
        };

        private static readonly Dictionary<Type, Func<ByteReader, object>> _deserializationExpressions = new Dictionary<Type, Func<ByteReader, object>>()
        {
            {typeof(byte), (ByteReader br) => br.ReadByte() },
            {typeof(sbyte), (ByteReader br) => br.ReadSByte() },
            {typeof(short), (ByteReader br) => br.ReadShort() },
            {typeof(ushort), (ByteReader br) => br.ReadUShort() },
            {typeof(int), (ByteReader br) => br.ReadInt() },
            {typeof(uint), (ByteReader br) => br.ReadUInt() },
            {typeof(long), (ByteReader br) => br.ReadLong() },
            {typeof(ulong), (ByteReader br) => br.ReadULong() },
            {typeof(float), (ByteReader br) => br.ReadFloat() },
            {typeof(double), (ByteReader br) => br.ReadDouble() },
            {typeof(decimal), (ByteReader br) => br.ReadDecimal() },
            {typeof(char), (ByteReader br) => br.ReadChar() },
            {typeof(bool), (ByteReader br) => br.ReadBool() },
        };

        public PrimitiveSerializer()
        {
            if (!_serializationExpressions.ContainsKey(typeof(T)) || !_deserializationExpressions.ContainsKey(typeof(T)))
                throw new NotSupportedException(string.Format(EXCP_NOT_PRIMITIVE, nameof(T)));
        }

        public override object Deserialize(ByteReader reader)
        {
            return _deserializationExpressions[typeof(T)](reader);
        }

        public override void Serialize(ByteWriter writer, object obj)
        {
            _serializationExpressions[typeof(T)](writer, obj);
        }

        public override bool CanSerialize(Type type)
        {
            return type.IsPrimitive;
        }
    }
}
