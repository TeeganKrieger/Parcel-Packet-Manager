using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Parcel.Lib.CorrectedSizeOf;

namespace Parcel.Serialization.Binary
{
    public class BinaryReader : DataReader
    {
        private static string EXCP_NOT_ENUM = "Cannot read enum. Type provided '{0}' is not an enum.";

        private byte[] _data;
        public override int Length => this._data.Length;

        public BinaryReader(SerializerResolverV2 serializerResolver, byte[] data) : base(serializerResolver) 
        {
            this._data = data;
        }


        #region ABSTRACT IMPLEMENTATION


        #region PRIMITIVES

        public override byte ReadByte()
        {
            return this.ReadPrimitive<byte>();
        }

        public override sbyte ReadSByte()
        {
            return this.ReadPrimitive<sbyte>();
        }

        public override short ReadShort()
        {
            return this.ReadPrimitive<short>();
        }

        public override ushort ReadUShort()
        {
            return this.ReadPrimitive<ushort>();
        }

        public override int ReadInt()
        {
            return this.ReadPrimitive<int>();
        }

        public override uint ReadUInt()
        {
            return this.ReadPrimitive<uint>();
        }

        public override long ReadLong()
        {
            return this.ReadPrimitive<long>();
        }

        public override ulong ReadULong()
        {
            return this.ReadPrimitive<ulong>();
        }

        public override float ReadFloat()
        {
            return this.ReadPrimitive<float>();
        }

        public override double ReadDouble()
        {
            return this.ReadPrimitive<double>();
        }

        public override decimal ReadDecimal()
        {
            unsafe
            {
                int len = 4 * SizeOf<int>();
                int[] bits = new int[4];
                GCHandle arrHandle = GCHandle.Alloc(bits, GCHandleType.Pinned);

                //Copy bytes from bytes array into arr and free new handle
                Marshal.Copy(this._data, this._position, arrHandle.AddrOfPinnedObject(), len);
                arrHandle.Free();

                this._position += len;
                return new decimal(bits);
            }
        }

        public override bool ReadBoolean()
        {
            return this.ReadPrimitive<byte>() == 1;
        }

        public override char ReadChar()
        {
            return (char)this.ReadPrimitive<ushort>();
        }

        #endregion


        #region PRIMITIVE ARRAYS

        public override byte[] ReadByteArray()
        {
            return this.ReadPrimitiveArray<byte>();
        }

        public override sbyte[] ReadSByteArray()
        {
            return this.ReadPrimitiveArray<sbyte>();
        }

        public override short[] ReadShortArray()
        {
            return this.ReadPrimitiveArray<short>();
        }

        public override ushort[] ReadUShortArray()
        {
            return this.ReadPrimitiveArray<ushort>();
        }

        public override int[] ReadIntArray()
        {
            return this.ReadPrimitiveArray<int>();
        }

        public override uint[] ReadUIntArray()
        {
            return this.ReadPrimitiveArray<uint>();
        }

        public override long[] ReadLongArray()
        {
            return this.ReadPrimitiveArray<long>();
        }

        public override ulong[] ReadULongArray()
        {
            return this.ReadPrimitiveArray<ulong>();
        }

        public override float[] ReadFloatArray()
        {
            return this.ReadPrimitiveArray<float>();
        }

        public override double[] ReadDoubleArray()
        {
            return this.ReadPrimitiveArray<double>();
        }

        public override decimal[] ReadDecimalArray()
        {
            bool isNull = this.ReadBoolean();
            if (isNull)
                return null;

            unsafe
            {
                //Pin bytes array and get array length
                GCHandle handle = GCHandle.Alloc(this._data, GCHandleType.Pinned);
                int len = Marshal.PtrToStructure<int>(handle.AddrOfPinnedObject() + this._position);
                handle.Free();
                this._position += SizeOf<int>();

                //Create T array and pin T array
                decimal[] arr = new decimal[len];
                int[] bits = new int[4];
                GCHandle arrHandle = GCHandle.Alloc(bits, GCHandleType.Pinned);

                for (int i = 0; i < arr.Length; i++)
                {
                    Marshal.Copy(this._data, this._position, arrHandle.AddrOfPinnedObject(), SizeOf<int>() * 4);
                    this._position += SizeOf<int>() * 4;
                    arr[i] = new decimal(bits);
                }
                arrHandle.Free();

                return arr;
            }
        }

        public override bool[] ReadBooleanArray()
        {
            byte[] bytes = this.ReadPrimitiveArray<byte>();
            bool[] bools = new bool[bytes.Length];

            for (int i = 0; i < bytes.Length; i++)
                bools[i] = bytes[i] == 1;

            return bools;
        }

        public override char[] ReadCharArray()
        {
            return this.ReadPrimitiveArray<char>();
        }

        #endregion


        #region STRINGS

        public override string ReadString()
        {
            bool isNull = this.ReadBoolean();
            if (isNull)
                return null;

            unsafe
            {
                GCHandle handle = GCHandle.Alloc(this._data, GCHandleType.Pinned);

                int len = Marshal.PtrToStructure<int>(handle.AddrOfPinnedObject() + this._position);
                handle.Free();

                string s = System.Text.Encoding.Unicode.GetString(this._data, this._position + sizeof(int), len);
                this._position += sizeof(int) + len;
                return s;
            }
        }

        public override string[] ReadStringArray()
        {
            bool isNull = this.ReadBoolean();
            if (isNull)
                return null;

            unsafe
            {
                GCHandle handle = GCHandle.Alloc(this._data, GCHandleType.Pinned);

                int len = Marshal.PtrToStructure<int>(handle.AddrOfPinnedObject() + this._position);
                handle.Free();

                this._position += sizeof(int);

                string[] strings = new string[len];

                for (int i = 0; i < strings.Length; i++)
                    strings[i] = this.ReadString();
                return strings;
            }
        }

        #endregion


        #region ENUMS

        public override Enum ReadEnum(Type enumType)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum)
                throw new ArgumentException(string.Format(EXCP_NOT_ENUM, enumType.FullName));

            Dictionary<Type, Func<object>> @switch = new Dictionary<Type, Func<object>>()
            {
                { typeof(byte), () => { return this.ReadByte(); } },
                { typeof(sbyte), () => { return this.ReadSByte(); } },
                { typeof(short), () => { return this.ReadShort(); } },
                { typeof(ushort), () => { return this.ReadUShort(); } },
                { typeof(int), () => { return this.ReadInt(); } },
                { typeof(uint), () => { return this.ReadUInt(); } },
                { typeof(long), () => { return this.ReadLong(); } },
                { typeof(ulong), () => { return this.ReadULong(); } },
            };

            Type underlyingType = Enum.GetUnderlyingType(enumType);

            return (Enum)Enum.ToObject(enumType, @switch[underlyingType]());
        }

        public override E ReadEnum<E>()
        {
            Dictionary<Type, Func<object>> @switch = new Dictionary<Type, Func<object>>()
            {
                { typeof(byte), () => { return this.ReadByte(); } },
                { typeof(sbyte), () => { return this.ReadSByte(); } },
                { typeof(short), () => { return this.ReadShort(); } },
                { typeof(ushort), () => { return this.ReadUShort(); } },
                { typeof(int), () => { return this.ReadInt(); } },
                { typeof(uint), () => { return this.ReadUInt(); } },
                { typeof(long), () => { return this.ReadLong(); } },
                { typeof(ulong), () => { return this.ReadULong(); } },
            };

            Type underlyingType = Enum.GetUnderlyingType(typeof(E));

            return (E)Enum.ToObject(typeof(E), @switch[underlyingType]());
        }

        public override Enum[] ReadEnumArray(Type enumType)
        {
            throw new NotImplementedException();
        }

        public override E[] ReadEnumArray<E>()
        {
            throw new NotImplementedException();
        }

        #endregion


        #region OBJECTS

        public override object ReadObject(bool readTypeInformation = true, Type type = null)
        {
            if (!readTypeInformation && type == null)
                throw new ArgumentException(nameof(type));

            bool isNull = this.ReadBoolean();
            if (isNull)
                return null;

            if (readTypeInformation && type != typeof(TypeHashCode))
            {
                TypeHashCode typeHashCode = this.ReadObject<TypeHashCode>();
                type = TypeHashCode.ParseType(typeHashCode);
            }

            SerializerV2 serializer = this.SerializerResolver.GetSerializer(type);
            return serializer.Deserialize(this);
        }

        public override T ReadObject<T>(bool readTypeInformation = true)
        {
            bool isNull = this.ReadBoolean();
            if (isNull)
                return default(T);

            Type type = typeof(T);

            if (readTypeInformation && type != typeof(TypeHashCode))
            {
                TypeHashCode typeHashCode = this.ReadObject<TypeHashCode>();
                type = TypeHashCode.ParseType(typeHashCode);
            }

            SerializerV2 serializer = this.SerializerResolver.GetSerializer(type);
            return (T)serializer.Deserialize(this);
        }

        public override object[] ReadObjectArray(bool readTypeInformation = true, Type type = null)
        {
            bool isNull = this.ReadBoolean();
            if (isNull)
                return null;

            int length = this.ReadPrimitive<int>();
            object[] oArr = new object[length];

            for (int i = 0; i < length; i++)
            {
                oArr[i] = this.ReadObject(readTypeInformation, type);
            }

            return oArr;
        }

        public override T[] ReadObjectArray<T>(bool readTypeInformation = true)
        {
            bool isNull = this.ReadBoolean();
            if (isNull)
                return null;

            Type type = typeof(T);

            int length = this.ReadPrimitive<int>();
            T[] oArr = new T[length];

            for (int i = 0; i < length; i++)
            {
                oArr[i] = (T)this.ReadObject(readTypeInformation, type);
            }

            return oArr;
        }

        #endregion


        #endregion


        #region HELPERS

        /// <summary>
        /// Read a primitive value from the internal data stream.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <returns>The value that was read.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T ReadPrimitive<T>() where T : struct
        {
            unsafe
            {
                GCHandle handle = GCHandle.Alloc(_data, GCHandleType.Pinned);
                T val = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject() + this._position);
                handle.Free();

                this._position += SizeOf<T>();
                return val;
            }
        }

        /// <summary>
        /// Read a array of primitive values from the internal data stream.
        /// </summary>
        /// <typeparam name="T">The type of the values.</typeparam>
        /// <returns>The values that were read.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T[] ReadPrimitiveArray<T>() where T : struct
        {
            bool isNull = this.ReadBoolean();
            if (isNull)
                return null;

            unsafe
            {
                //Pin bytes array and get array length
                GCHandle handle = GCHandle.Alloc(_data, GCHandleType.Pinned);
                int len = Marshal.PtrToStructure<int>(handle.AddrOfPinnedObject() + this._position);
                handle.Free();

                //Create T array and pin T array
                T[] arr = new T[len];
                GCHandle arrHandle = GCHandle.Alloc(arr, GCHandleType.Pinned);

                //Copy bytes from bytes array into arr and free new handle
                Marshal.Copy(_data, sizeof(int) + this._position, arrHandle.AddrOfPinnedObject(), len * SizeOf<T>());
                arrHandle.Free();

                this._position += sizeof(int) + len * SizeOf<T>();
                return arr;
            }
        }

        #endregion


        #region IENUMERABLE IMPLEMENTATION

        public override IEnumerator<byte> GetEnumerator()
        {
            for (int i = 0; i < this.Length; i++)
            {
                yield return this._data[i];
            }
        }

        #endregion

    }
}
