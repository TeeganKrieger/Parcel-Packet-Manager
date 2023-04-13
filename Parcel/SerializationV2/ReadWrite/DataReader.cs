using System;
using System.Collections;
using System.Collections.Generic;

namespace Parcel.Serialization
{
    public abstract class DataReader : IEnumerable<byte>
    {
        protected int _position;

        public SerializerResolverV2 SerializerResolver { get; private set; }
        public int Position => this._position;
        public abstract int Length { get; }


        #region CONSTRUCTOR

        public DataReader(SerializerResolverV2 serializerResolver)
        {
            this.SerializerResolver = serializerResolver;
        }

        #endregion


        #region CONCRETE METHODS

        public void SetPosition(int position)
        {
            if (position < 0 || position > this.Length)
                throw new ArgumentOutOfRangeException(nameof(position));

            this._position = position;
        }

        public void Reset()
        {
            this._position = 0;
        }

        #endregion


        #region ABSTRACT METHODS

        public abstract byte ReadByte();
        public abstract sbyte ReadSByte();
        public abstract short ReadShort();
        public abstract ushort ReadUShort();
        public abstract int ReadInt();
        public abstract uint ReadUInt();
        public abstract long ReadLong();
        public abstract ulong ReadULong();
        public abstract float ReadFloat();
        public abstract double ReadDouble();
        public abstract decimal ReadDecimal();
        public abstract bool ReadBoolean();
        public abstract char ReadChar();

        public abstract byte[] ReadByteArray();
        public abstract sbyte[] ReadSByteArray();
        public abstract short[] ReadShortArray();
        public abstract ushort[] ReadUShortArray();
        public abstract int[] ReadIntArray();
        public abstract uint[] ReadUIntArray();
        public abstract long[] ReadLongArray();
        public abstract ulong[] ReadULongArray();
        public abstract float[] ReadFloatArray();
        public abstract double[] ReadDoubleArray();
        public abstract decimal[] ReadDecimalArray();
        public abstract bool[] ReadBooleanArray();
        public abstract char[] ReadCharArray();

        public abstract string ReadString();
        public abstract string[] ReadStringArray();

        public abstract Enum ReadEnum(Type enumType);
        public abstract E ReadEnum<E>() where E : Enum;
        public abstract Enum[] ReadEnumArray(Type enumType);
        public abstract E[] ReadEnumArray<E>() where E : Enum;

        public abstract object ReadObject(bool readTypeInformation = true, Type type = null);
        public abstract T ReadObject<T>(bool readTypeInformation = true);
        public abstract object[] ReadObjectArray(bool readTypeInformation = true, Type type = null);
        public abstract T[] ReadObjectArray<T>(bool readTypeInformation = true);

        #endregion


        #region IENUMERABLE IMPLEMENTATION

        public abstract IEnumerator<byte> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion
    
    }
}
