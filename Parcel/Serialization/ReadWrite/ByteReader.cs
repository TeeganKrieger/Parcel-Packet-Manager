using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Parcel.Lib.CorrectedSizeOf;

namespace Parcel.Serialization
{

    /// <summary>
    /// Facilitates the reading of primitives and objects to an array of bytes.
    /// </summary>
    public sealed class ByteReader
    {
        private static string EXCP_POS_RANGE = "Cannot set postion to {0}. NewPosition must be between 0 and the length of the internal data stream.";
        private static string EXCP_NOT_ENUM = "Cannot read enum. Type provided '{0}' is not an enum.";

        private byte[] _data;
        private int _position = 0;

        /// <summary>
        /// Get the length of the internal data stream.
        /// </summary>
        public int Length => _data.Length;

        /// <summary>
        /// Get the current position of the ByteReader.
        /// </summary>
        public int Position => _position;

        /// <summary>
        /// The <see cref="Serialization.SerializerResolver"/> used within this ByteReader.
        /// </summary>
        public SerializerResolver SerializerResolver { get; private set; }


        #region CONSTRUCTOR

        /// <summary>
        /// Construct a new instance of ByteReader.
        /// </summary>
        /// <param name="data">The array of bytes to read from.</param>
        /// <param name="serializerResolver">The <see cref="Serialization.SerializerResolver"/> to use.</param>
        public ByteReader(byte[] data, SerializerResolver serializerResolver = null)
        {
            this._data = data;
            this.SerializerResolver = serializerResolver ?? SerializerResolver.Global;
        }

        #endregion


        #region READ PRIMITIVES

        /// <summary>
        /// Read the next byte in the ByteReader.
        /// </summary>
        /// <returns>The next byte in the ByteReader.</returns>
        public byte ReadByte()
        {
            return ReadPrimitive<byte>();
        }

        /// <summary>
        /// Read the next sbyte in the ByteReader.
        /// </summary>
        /// <returns>The next sbyte in the ByteReader.</returns>
        public sbyte ReadSByte()
        {
            return ReadPrimitive<sbyte>();
        }

        /// <summary>
        /// Read the next short in the ByteReader.
        /// </summary>
        /// <returns>The next short in the ByteReader.</returns>
        public short ReadShort()
        {
            return ReadPrimitive<short>();
        }

        /// <summary>
        /// Read the next ushort in the ByteReader.
        /// </summary>
        /// <returns>The next ushort in the ByteReader.</returns>
        public ushort ReadUShort()
        {
            return ReadPrimitive<ushort>();
        }

        /// <summary>
        /// Read the next int in the ByteReader.
        /// </summary>
        /// <returns>The next int in the ByteReader.</returns>
        public int ReadInt()
        {
            return ReadPrimitive<int>();
        }

        /// <summary>
        /// Read the next uint in the ByteReader.
        /// </summary>
        /// <returns>The next uint in the ByteReader.</returns>
        public uint ReadUInt()
        {
            return ReadPrimitive<uint>();
        }

        /// <summary>
        /// Read the next long in the ByteReader.
        /// </summary>
        /// <returns>The next long in the ByteReader.</returns>
        public long ReadLong()
        {
            return ReadPrimitive<long>();
        }

        /// <summary>
        /// Read the next ulong in the ByteReader.
        /// </summary>
        /// <returns>The next ulong in the ByteReader.</returns>
        public ulong ReadULong()
        {
            return ReadPrimitive<ulong>();
        }

        /// <summary>
        /// Read the next float in the ByteReader.
        /// </summary>
        /// <returns>The next float in the ByteReader.</returns>
        public float ReadFloat()
        {
            return ReadPrimitive<float>();
        }

        /// <summary>
        /// Read the next double in the ByteReader.
        /// </summary>
        /// <returns>The next double in the ByteReader.</returns>
        public double ReadDouble()
        {
            return ReadPrimitive<double>();
        }

        /// <summary>
        /// Read the next decimal in the ByteReader.
        /// </summary>
        /// <returns>The next decimal in the ByteReader.</returns>
        public decimal ReadDecimal()
        {
            unsafe
            {
                int len = 4 * SizeOf<int>();
                int[] bits = new int[4];
                GCHandle arrHandle = GCHandle.Alloc(bits, GCHandleType.Pinned);

                //Copy bytes from bytes array into arr and free new handle
                Marshal.Copy(_data, _position, arrHandle.AddrOfPinnedObject(), len);
                arrHandle.Free();

                _position += len;
                return new decimal(bits);
            }
        }

        /// <summary>
        /// Read the next bool in the ByteReader.
        /// </summary>
        /// <returns>The next bool in the ByteReader.</returns>
        public bool ReadBool()
        {
            bool val = _data[_position] == 1;
            _position += SizeOf<bool>();
            return val;
        }

        /// <summary>
        /// Read the next char in the ByteReader.
        /// </summary>
        /// <returns>The next char in the ByteReader.</returns>
        public char ReadChar()
        {
            return (char)ReadPrimitive<ushort>();
        }

        #endregion


        #region READ PRIMITIVE ARRAYS

        /// <summary>
        /// Read the next array of bytes in the ByteReader.
        /// </summary>
        /// <returns>The next array of bytes in the ByteReader.</returns>
        public byte[] ReadByteArray()
        {
            return ReadPrimitiveArray<byte>();
        }

        /// <summary>
        /// Read the next array of sbytes in the ByteReader.
        /// </summary>
        /// <returns>The next array of sbytes in the ByteReader.</returns>
        public sbyte[] ReadSByteArray()
        {
            return ReadPrimitiveArray<sbyte>();
        }

        /// <summary>
        /// Read the next array of shorts in the ByteReader.
        /// </summary>
        /// <returns>The next array of shorts in the ByteReader.</returns>
        public short[] ReadShortArray()
        {
            return ReadPrimitiveArray<short>();
        }

        /// <summary>
        /// Read the next array of ushorts in the ByteReader.
        /// </summary>
        /// <returns>The next array of ushorts in the ByteReader.</returns>
        public ushort[] ReadUShortArray()
        {
            return ReadPrimitiveArray<ushort>();
        }

        /// <summary>
        /// Read the next array of ints in the ByteReader.
        /// </summary>
        /// <returns>The next array of ints in the ByteReader.</returns>
        public int[] ReadIntArray()
        {
            return ReadPrimitiveArray<int>();
        }

        /// <summary>
        /// Read the next array of uints in the ByteReader.
        /// </summary>
        /// <returns>The next array of uints in the ByteReader.</returns>
        public uint[] ReadUIntArray()
        {
            return ReadPrimitiveArray<uint>();
        }

        /// <summary>
        /// Read the next array of longs in the ByteReader.
        /// </summary>
        /// <returns>The next array of longs in the ByteReader.</returns>
        public long[] ReadLongArray()
        {
            return ReadPrimitiveArray<long>();
        }

        /// <summary>
        /// Read the next array of ulongs in the ByteReader.
        /// </summary>
        /// <returns>The next array of ulongs in the ByteReader.</returns>
        public ulong[] ReadULongArray()
        {
            return ReadPrimitiveArray<ulong>();
        }

        /// <summary>
        /// Read the next array of floats in the ByteReader.
        /// </summary>
        /// <returns>The next array of floats in the ByteReader.</returns>
        public float[] ReadFloatArray()
        {
            return ReadPrimitiveArray<float>();
        }

        /// <summary>
        /// Read the next array of doubles in the ByteReader.
        /// </summary>
        /// <returns>The next array of doubles in the ByteReader.</returns>
        public double[] ReadDoubleArray()
        {
            return ReadPrimitiveArray<double>();
        }

        /// <summary>
        /// Read the next array of decimals in the ByteReader.
        /// </summary>
        /// <returns>The next array of decimals in the ByteReader.</returns>
        public decimal[] ReadDecimalArray()
        {
            unsafe
            {
                //Pin bytes array and get array length
                GCHandle handle = GCHandle.Alloc(_data, GCHandleType.Pinned);
                int len = Marshal.PtrToStructure<int>(handle.AddrOfPinnedObject() + _position);
                handle.Free();
                _position += SizeOf<int>();

                //Create T array and pin T array
                decimal[] arr = new decimal[len];
                int[] bits = new int[4];
                GCHandle arrHandle = GCHandle.Alloc(bits, GCHandleType.Pinned);

                for (int i = 0; i < arr.Length; i++)
                {
                    Marshal.Copy(_data, _position, arrHandle.AddrOfPinnedObject(), SizeOf<int>() * 4);
                    _position += SizeOf<int>() * 4;
                    arr[i] = new decimal(bits);
                }
                arrHandle.Free();

                return arr;
            }
        }

        /// <summary>
        /// Read the next array of bools in the ByteReader.
        /// </summary>
        /// <returns>The next array of bools in the ByteReader.</returns>
        public bool[] ReadBoolArray()
        {
            return ReadPrimitiveArray<bool>();
        }

        /// <summary>
        /// Read the next array of chars in the ByteReader.
        /// </summary>
        /// <returns>The next array of chars in the ByteReader.</returns>
        public char[] ReadCharArray()
        {
            return ReadPrimitiveArray<char>();
        }

        #endregion


        #region READ STRING

        /// <summary>
        /// Read the next string in the ByteReader.
        /// </summary>
        /// <returns>The next string in the ByteReader.</returns>
        public string ReadString()
        {
            unsafe
            {
                GCHandle handle = GCHandle.Alloc(_data, GCHandleType.Pinned);
                int len = Marshal.PtrToStructure<int>(handle.AddrOfPinnedObject() + _position);
                handle.Free();

                if (len == -1)
                {
                    _position += sizeof(int);
                    return null;
                }

                string s = System.Text.Encoding.Unicode.GetString(_data, _position + sizeof(int), len);
                _position += sizeof(int) + len;
                return s;
            }
        }

        #endregion


        #region READ ENUM

        /// <summary>
        /// Read the next Ehar in the ByteReader.
        /// </summary>
        /// <param name="enumType">The Type of the Enum to read.</param>
        /// <returns>The next Ehar in the ByteReader.</returns>
        public Enum ReadEnum(Type enumType)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum)
                throw new ArgumentException(string.Format(EXCP_NOT_ENUM, enumType.FullName));

            Dictionary<Type, Func<object>> @switch = new Dictionary<Type, Func<object>>()
            {
                { typeof(byte), () => { return ReadByte(); } },
                { typeof(sbyte), () => { return ReadSByte(); } },
                { typeof(short), () => { return ReadShort(); } },
                { typeof(ushort), () => { return ReadUShort(); } },
                { typeof(int), () => { return ReadInt(); } },
                { typeof(uint), () => { return ReadUInt(); } },
                { typeof(long), () => { return ReadLong(); } },
                { typeof(ulong), () => { return ReadULong(); } },
            };

            Type underlyingType = Enum.GetUnderlyingType(enumType);

            return (Enum)Enum.ToObject(enumType, @switch[underlyingType]());
        }

        /// <summary>
        /// Read the next Enum in the ByteReader.
        /// </summary>
        /// <typeparam name="T">The Type of the Enum to read.</typeparam>
        /// <returns>The next Ehar in the ByteReader.</returns>
        public T ReadEnum<T>() where T : Enum
        {
            Dictionary<Type, Func<object>> @switch = new Dictionary<Type, Func<object>>()
            {
                { typeof(byte), () => { return ReadByte(); } },
                { typeof(sbyte), () => { return ReadSByte(); } },
                { typeof(short), () => { return ReadShort(); } },
                { typeof(ushort), () => { return ReadUShort(); } },
                { typeof(int), () => { return ReadInt(); } },
                { typeof(uint), () => { return ReadUInt(); } },
                { typeof(long), () => { return ReadLong(); } },
                { typeof(ulong), () => { return ReadULong(); } },
            };

            Type underlyingType = Enum.GetUnderlyingType(typeof(T));

            return (T)Enum.ToObject(typeof(T), @switch[underlyingType]());
        }

        #endregion


        #region READ OBJECTS

        /// <summary>
        /// Read the next <see cref="TypeHashCode"/> in the ByteReader.
        /// </summary>
        /// <returns>The next <see cref="TypeHashCode"/> in the ByteReader.</returns>
        internal TypeHashCode ReadTypeHashCode()
        {
            ulong hashcode = ReadULong();
            TypeHashCode temp = new TypeHashCode(hashcode, null);

            TypeHashCode[] genericArgs = null;
            if (temp.IsGenericType)
            {
                genericArgs = new TypeHashCode[temp.GenericArgumentCount];
                for (int i = 0; i < temp.GenericArgumentCount; i++)
                {
                    genericArgs[i] = ReadTypeHashCode();
                }
            }

            return new TypeHashCode(hashcode, genericArgs ?? new TypeHashCode[0]);
        }

        /// <summary>
        /// Read the next object in the ByteReader.
        /// </summary>
        /// <param name="type">The Type of the object to read. If left as <see langword="null"/>, will parse the type from the ByteReader.</param>
        /// <returns>The next object in the ByteReader.</returns>
        public object ReadObject(Type type = null)
        {
            bool isNull = ReadBool();
            if (isNull)
                return null;

            if (type == null)
            {
                TypeHashCode typeHashCode = ReadTypeHashCode();
                type = TypeHashCode.ParseType(typeHashCode);
            }

            Serializer serializer = this.SerializerResolver.GetSerializer(type);
            return serializer.Deserialize(this);
        }

        /// <summary>
        /// Read the next object in the ByteReader.
        /// </summary>
        /// <typeparam name="T">The Type of the object to read.</typeparam>
        /// <returns>The next object in the ByteReader.</returns>
        public T ReadObject<T>()
        {
            bool isNull = ReadBool();
            if (isNull)
                return default(T);

            TypeHashCode typeHashCode = ReadTypeHashCode();
            Type type = TypeHashCode.ParseType(typeHashCode);

            Serializer serializer = this.SerializerResolver.GetSerializer(type);
            return (T)serializer.Deserialize(this);
        }

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
                T val = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject() + _position);
                handle.Free();

                _position += SizeOf<T>();
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
            unsafe
            {
                //Pin bytes array and get array length
                GCHandle handle = GCHandle.Alloc(_data, GCHandleType.Pinned);
                int len = Marshal.PtrToStructure<int>(handle.AddrOfPinnedObject() + _position);
                handle.Free();

                //Create T array and pin T array
                T[] arr = new T[len];
                GCHandle arrHandle = GCHandle.Alloc(arr, GCHandleType.Pinned);

                //Copy bytes from bytes array into arr and free new handle
                Marshal.Copy(_data, sizeof(int) + _position, arrHandle.AddrOfPinnedObject(), len * SizeOf<T>());
                arrHandle.Free();

                _position += sizeof(int) + len * SizeOf<T>();
                return arr;
            }
        }

        #endregion


        #region MISC

        /// <summary>
        /// Set the read position of the ByteReader.
        /// </summary>
        /// <param name="newPosition">The new read position.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="newPosition"/> is below 0 or greater than the length of the internal data stream.</exception>
        internal void SetPosition(int newPosition)
        {
            if (newPosition < 0 || newPosition > _data.Length)
                throw new ArgumentOutOfRangeException(nameof(newPosition), string.Format(EXCP_POS_RANGE, newPosition));

            this._position = newPosition;
        }

        #endregion
    }
}
