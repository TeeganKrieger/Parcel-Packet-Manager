using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Parcel.Lib.CorrectedSizeOf;

namespace Parcel.Serialization
{
    /// <summary>
    /// Facilitates the writing of primitives and objects to an array of bytes.
    /// </summary>
    public sealed class ByteWriter : IEnumerable<byte>
    {
        private static string EXCP_INVALID_POS = "Cannot write to position {0} as it would write out of the current boundaries of the stream.";
        private static string EXCP_POS_RANGE = "Cannot set position to {0}. NewPosition must be between 0 and the length of the internal data stream.";

        private byte[] _data;
        private int _position = 0;

        /// <summary>
        /// Get a copy of the data written to the ByteWriter.
        /// </summary>
        public byte[] Data
        {
            get
            {
                byte[] truncated = new byte[_position];
                Array.Copy(_data, truncated, _position);
                return truncated;
            }
        }

        /// <summary>
        /// Get the current length of the internal data stream.
        /// </summary>
        /// <remarks>
        /// The Length and <see cref="Position"/> Properties will produce the same value. Two different Properties exist purely for code clarity.
        /// </remarks>
        public int Length => _position;

        /// <summary>
        /// Get the current position of the internal data stream.
        /// </summary>
        /// <remarks>
        /// The <see cref="Length"/> and Position Properties will produce the same value. Two different Properties exist purely for code clarity.
        /// </remarks>
        public int Position => _position;

        /// <summary>
        /// The <see cref="Serialization.SerializerResolver"/> used within this ByteWriter.
        /// </summary>
        public SerializerResolver SerializerResolver { get; private set; }


        #region CONSTRUCTOR

        /// <summary>
        /// Construct a new instance of ByteWriter.
        /// </summary>
        /// <param name="serializerResolver">The <see cref="Serialization.SerializerResolver"/> to use.</param>
        public ByteWriter(SerializerResolver serializerResolver = null)
        {
            this._data = new byte[32];
            this.SerializerResolver = serializerResolver ?? SerializerResolver.Global;
        }

        #endregion


        #region WRITE PRIMITIVES

        /// <summary>
        /// Write a byte to the ByteWriter.
        /// </summary>
        /// <param name="b">The byte to write.</param>
        public void Write(byte b)
        {
            WritePrimitive(b);
        }

        /// <summary>
        /// Write a byte to the ByteWriter.
        /// </summary>
        /// <param name="b">The byte to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(byte b, int position)
        {
            WritePrimitive(b, position);
        }

        /// <summary>
        /// Write an sbyte to the ByteWriter.
        /// </summary>
        /// <param name="b">The sbyte to write.</param>
        public void Write(sbyte b)
        {
            WritePrimitive(b);
        }

        /// <summary>
        /// Write an sbyte to the ByteWriter.
        /// </summary>
        /// <param name="b">The sbyte to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(sbyte b, int position)
        {
            WritePrimitive(b, position);
        }

        /// <summary>
        /// Write a short to the ByteWriter.
        /// </summary>
        /// <param name="s">The short to write.</param>
        public void Write(short s)
        {
            WritePrimitive(s);
        }

        /// <summary>
        /// Write a short to the ByteWriter.
        /// </summary>
        /// <param name="s">The short to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(short s, int position)
        {
            WritePrimitive(s, position);
        }

        /// <summary>
        /// Write a ushort to the ByteWriter.
        /// </summary>
        /// <param name="s">The ushort to write.</param>
        public void Write(ushort s)
        {
            WritePrimitive(s);
        }

        /// <summary>
        /// Write a ushort to the ByteWriter.
        /// </summary>
        /// <param name="s">The ushort to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(ushort s, int position)
        {
            WritePrimitive(s, position);
        }

        /// <summary>
        /// Write an int to the ByteWriter.
        /// </summary>
        /// <param name="i">The int to write.</param>
        public void Write(int i)
        {
            WritePrimitive(i);
        }

        /// <summary>
        /// Write an int to the ByteWriter.
        /// </summary>
        /// <param name="i">The int to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(int i, int position)
        {
            WritePrimitive(i, position);
        }

        /// <summary>
        /// Write a uint to the ByteWriter.
        /// </summary>
        /// <param name="i">The uint to write.</param>
        public void Write(uint i)
        {
            WritePrimitive(i);
        }

        /// <summary>
        /// Write a uint to the ByteWriter.
        /// </summary>
        /// <param name="i">The uint to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(uint i, int position)
        {
            WritePrimitive(i, position);
        }

        /// <summary>
        /// Write a long to the ByteWriter.
        /// </summary>
        /// <param name="l">The long to write.</param>
        public void Write(long l)
        {
            WritePrimitive(l);
        }

        /// <summary>
        /// Write a long to the ByteWriter.
        /// </summary>
        /// <param name="l">The long to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(long l, int position)
        {
            WritePrimitive(l, position);
        }

        /// <summary>
        /// Write a ulong to the ByteWriter.
        /// </summary>
        /// <param name="l">The ulong to write.</param>
        public void Write(ulong l)
        {
            WritePrimitive(l);
        }

        /// <summary>
        /// Write a ulong to the ByteWriter.
        /// </summary>
        /// <param name="l">The ulong to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(ulong l, int position)
        {
            WritePrimitive(l, position);
        }

        /// <summary>
        /// Write a float to the ByteWriter.
        /// </summary>
        /// <param name="f">The float to write.</param>
        public void Write(float f)
        {
            WritePrimitive(f);
        }

        /// <summary>
        /// Write a float to the ByteWriter.
        /// </summary>
        /// <param name="f">The float to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(float f, int position)
        {
            WritePrimitive(f, position);
        }

        /// <summary>
        /// Write a double to the ByteWriter.
        /// </summary>
        /// <param name="d">The double to write.</param>
        public void Write(double d)
        {
            WritePrimitive(d);
        }

        /// <summary>
        /// Write a double to the ByteWriter.
        /// </summary>
        /// <param name="d">The double to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(double d, int position)
        {
            WritePrimitive(d, position);
        }

        /// <summary>
        /// Write a decimal to the ByteWriter.
        /// </summary>
        /// <param name="d">The decimal to write.</param>
        public void Write(decimal d)
        {
            int[] bits = decimal.GetBits(d);

            int len = SizeOf<int>() * 4;

            while (_position + len > _data.Length)
                Array.Resize(ref _data, _data.Length * 2);

            unsafe
            {
                //Write array
                GCHandle handle = GCHandle.Alloc(bits, GCHandleType.Pinned);
                Marshal.Copy(handle.AddrOfPinnedObject(), _data, _position, len);
                handle.Free();

                _position += len;
            }
        }

        /// <summary>
        /// Write a decimal to the ByteWriter.
        /// </summary>
        /// <param name="d">The decimal to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(decimal d, int position)
        {
            int[] bits = decimal.GetBits(d);

            int len = SizeOf<int>() * 4;

            if (position + len > _data.Length)
                throw new ArgumentException(string.Format(EXCP_INVALID_POS, position), nameof(position));

            unsafe
            {
                //Write array
                GCHandle handle = GCHandle.Alloc(bits, GCHandleType.Pinned);
                Marshal.Copy(handle.AddrOfPinnedObject(), _data, position, len);
                handle.Free();
            }
        }

        /// <summary>
        /// Write a bool to the ByteWriter.
        /// </summary>
        /// <param name="b">The bool to write.</param>
        public void Write(bool b)
        {
            if (_position + SizeOf<byte>() > _data.Length)
                Array.Resize(ref _data, _data.Length * 2);

            _data[_position] = b ? (byte)1 : (byte)0;
            _position += SizeOf<byte>();
        }

        /// <summary>
        /// Write a bool to the ByteWriter.
        /// </summary>
        /// <param name="b">The bool to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(bool b, int position)
        {
            if (_position + SizeOf<byte>() > _data.Length)
                throw new ArgumentException(string.Format(EXCP_INVALID_POS, position), nameof(position));

            _data[position] = b ? (byte)1 : (byte)0;
        }

        /// <summary>
        /// Write a char to the ByteWriter.
        /// </summary>
        /// <param name="c">The char to write.</param>
        public void Write(char c)
        {
            WritePrimitive<ushort>((ushort)c);
        }

        /// <summary>
        /// Write a char to the ByteWriter.
        /// </summary>
        /// <param name="c">The char to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(char c, int position)
        {
            WritePrimitive<ushort>((ushort)c, position);
        }

        #endregion


        #region WRITE PRIMITIVE ARRAYS

        /// <summary>
        /// Write an array of bytes to the ByteWriter.
        /// </summary>
        /// <param name="b">The array of bytes to write.</param>
        public void Write(byte[] b)
        {
            WritePrimitiveArray(b);
        }

        /// <summary>
        /// Write an array of bytes to the ByteWriter.
        /// </summary>
        /// <param name="b">The array of bytes to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(byte[] b, int position)
        {
            WritePrimitiveArray(b, position);
        }

        /// <summary>
        /// Write an array of sbytes to the ByteWriter.
        /// </summary>
        /// <param name="b">The array of sbytes to write.</param>
        public void Write(sbyte[] b)
        {
            WritePrimitiveArray(b);
        }

        /// <summary>
        /// Write an array of sbytes to the ByteWriter.
        /// </summary>
        /// <param name="b">The array of sbytes to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(sbyte[] b, int position)
        {
            WritePrimitiveArray(b, position);
        }

        /// <summary>
        /// Write an array of shorts to the ByteWriter.
        /// </summary>
        /// <param name="s">The array of shorts to write.</param>
        public void Write(short[] s)
        {
            WritePrimitiveArray(s);
        }

        /// <summary>
        /// Write an array of shorts to the ByteWriter.
        /// </summary>
        /// <param name="s">The array of shorts to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(short[] s, int position)
        {
            WritePrimitiveArray(s, position);
        }

        /// <summary>
        /// Write an array of ushorts to the ByteWriter.
        /// </summary>
        /// <param name="s">The array of ushort to write.</param>
        public void Write(ushort[] s)
        {
            WritePrimitiveArray(s);
        }

        /// <summary>
        /// Write an array of ushorts to the ByteWriter.
        /// </summary>
        /// <param name="s">The array of ushorts to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(ushort[] s, int position)
        {
            WritePrimitiveArray(s, position);
        }

        /// <summary>
        /// Write an array of ints to the ByteWriter.
        /// </summary>
        /// <param name="i">The array of ints to write.</param>
        public void Write(int[] i)
        {
            WritePrimitiveArray(i);
        }

        /// <summary>
        /// Write an array of ints to the ByteWriter.
        /// </summary>
        /// <param name="i">The array of ints to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(int[] i, int position)
        {
            WritePrimitiveArray(i, position);
        }

        /// <summary>
        /// Write an array of uints to the ByteWriter.
        /// </summary>
        /// <param name="i">The array of uints to write.</param>
        public void Write(uint[] i)
        {
            WritePrimitiveArray(i);
        }

        /// <summary>
        /// Write an array of uints to the ByteWriter.
        /// </summary>
        /// <param name="i">The array of uints to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(uint[] i, int position)
        {
            WritePrimitiveArray(i, position);
        }

        /// <summary>
        /// Write an array of longs to the ByteWriter.
        /// </summary>
        /// <param name="l">The array of longs to write.</param>
        public void Write(long[] l)
        {
            WritePrimitiveArray(l);
        }

        /// <summary>
        /// Write an array of longs to the ByteWriter.
        /// </summary>
        /// <param name="l">The array of longs to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(long[] l, int position)
        {
            WritePrimitiveArray(l, position);
        }

        /// <summary>
        /// Write an array of ulongs to the ByteWriter.
        /// </summary>
        /// <param name="l">The array of ulongs to write.</param>
        public void Write(ulong[] l)
        {
            WritePrimitiveArray(l);
        }

        /// <summary>
        /// Write an array of ulongs to the ByteWriter.
        /// </summary>
        /// <param name="l">The array of ulongs to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(ulong[] l, int position)
        {
            WritePrimitiveArray(l, position);
        }

        /// <summary>
        /// Write an array of floats to the ByteWriter.
        /// </summary>
        /// <param name="f">The array of floats to write.</param>
        public void Write(float[] f)
        {
            WritePrimitiveArray(f);
        }

        /// <summary>
        /// Write an array of floats to the ByteWriter.
        /// </summary>
        /// <param name="f">The array of floats to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(float[] f, int position)
        {
            WritePrimitiveArray(f);
        }

        /// <summary>
        /// Write an array of doubles to the ByteWriter.
        /// </summary>
        /// <param name="d">The array of doubles to write.</param>
        public void Write(double[] d)
        {
            WritePrimitiveArray(d);
        }

        /// <summary>
        /// Write an array of doubles to the ByteWriter.
        /// </summary>
        /// <param name="d">The array of doubles to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(double[] d, int position)
        {
            WritePrimitiveArray(d);
        }

        /// <summary>
        /// Write an array of decimals to the ByteWriter.
        /// </summary>
        /// <param name="d">The array of decimals to write.</param>
        public void Write(decimal[] d)
        {
            int len = SizeOf<int>() * 4 * d.Length;

            int[] bits;

            while (_position + sizeof(int) + len > _data.Length)
                Array.Resize(ref _data, _data.Length * 2);

            unsafe
            {
                //Write length of array
                GCHandle handle = GCHandle.Alloc(d.Length, GCHandleType.Pinned);
                Marshal.Copy(handle.AddrOfPinnedObject(), _data, _position, sizeof(int));
                handle.Free();
                _position += sizeof(int);
            }

            for (int i = 0; i < d.Length; i++)
            {
                bits = decimal.GetBits(d[i]);

                unsafe
                {
                    //Write array
                    GCHandle handle = GCHandle.Alloc(bits, GCHandleType.Pinned);
                    Marshal.Copy(handle.AddrOfPinnedObject(), _data, _position, SizeOf<int>() * 4);
                    handle.Free();

                    _position += SizeOf<int>() * 4;
                }
            }
        }

        /// <summary>
        /// Write an array of decimals to the ByteWriter.
        /// </summary>
        /// <param name="d">The array of decimals to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(decimal[] d, int position)
        {
            int len = SizeOf<int>() * 4 * d.Length;

            int[] bits;

            if (position + sizeof(int) + len > _data.Length)
                throw new ArgumentException(string.Format(EXCP_INVALID_POS, position), nameof(position));

            unsafe
            {
                //Write length of array
                GCHandle handle = GCHandle.Alloc(d.Length, GCHandleType.Pinned);
                Marshal.Copy(handle.AddrOfPinnedObject(), _data, position, sizeof(int));
                handle.Free();
            }

            for (int i = 0; i < d.Length; i++)
            {
                bits = decimal.GetBits(d[i]);

                unsafe
                {
                    //Write array
                    GCHandle handle = GCHandle.Alloc(bits, GCHandleType.Pinned);
                    Marshal.Copy(handle.AddrOfPinnedObject(), _data, _position, SizeOf<int>() * 4);
                    handle.Free();
                }
            }
        }

        /// <summary>
        /// Write an array of bools to the ByteWriter.
        /// </summary>
        /// <param name="b">The array of bools to write.</param>
        public void Write(bool[] b)
        {
            byte[] bools = new byte[b.Length];

            for (int i = 0; i < b.Length; i++)
                bools[i] = b[i] ? (byte)1 : (byte)0;

            WritePrimitiveArray(bools);
        }

        /// <summary>
        /// Write an array of bools to the ByteWriter.
        /// </summary>
        /// <param name="b">The array of bools to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(bool[] b, int position)
        {
            byte[] bools = new byte[b.Length];

            for (int i = 0; i < b.Length; i++)
                bools[i] = b[i] ? (byte)1 : (byte)0;

            WritePrimitiveArray(bools, position);
        }

        /// <summary>
        /// Write an array of chars to the ByteWriter.
        /// </summary>
        /// <param name="c">The array of chars to write.</param>
        public void Write(char[] c)
        {
            ushort[] chars = new ushort[c.Length];

            for (int i = 0; i < c.Length; i++)
                chars[i] = (ushort)c[i];

            WritePrimitiveArray(chars);
        }

        /// <summary>
        /// Write an array of chars to the ByteWriter.
        /// </summary>
        /// <param name="c">The array of chars to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(char[] c, int position)
        {
            ushort[] chars = new ushort[c.Length];

            for (int i = 0; i < c.Length; i++)
                chars[i] = (ushort)c[i];

            WritePrimitiveArray(chars, position);
        }

        #endregion


        #region WRITE STRINGS

        /// <summary>
        /// Write a string to the ByteWriter.
        /// </summary>
        /// <param name="s">The string to write.</param>
        public void Write(string s)
        {
            Write(s == null);
            if (s == null)
                return;

            int len = sizeof(char) * s.Length;

            while (_position + sizeof(int) + len > _data.Length)
                Array.Resize(ref _data, _data.Length * 2);

            unsafe
            {
                //Write length of string
                GCHandle handle = GCHandle.Alloc(len, GCHandleType.Pinned);
                Marshal.Copy(handle.AddrOfPinnedObject(), _data, _position, sizeof(int));
                handle.Free();

                //Write string
                handle = GCHandle.Alloc(s, GCHandleType.Pinned);
                Marshal.Copy(handle.AddrOfPinnedObject(), _data, _position + sizeof(int), len);
                handle.Free();

                _position += sizeof(int) + len;
            }
        }

        /// <summary>
        /// Write a string to the ByteWriter.
        /// </summary>
        /// <param name="s">The string to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(string s, int position)
        {
            Write(s == null, position);
            if (s == null)
                return;

            int len = sizeof(char) * s.Length;

            if (position + sizeof(bool) + sizeof(int) + len > _data.Length)
                throw new ArgumentException(string.Format(EXCP_INVALID_POS, position), nameof(position));

            unsafe
            {
                //Write length of string
                GCHandle handle = GCHandle.Alloc(len, GCHandleType.Pinned);
                Marshal.Copy(handle.AddrOfPinnedObject(), _data, position + sizeof(bool), sizeof(int));
                handle.Free();

                //Write string
                handle = GCHandle.Alloc(s, GCHandleType.Pinned);
                Marshal.Copy(handle.AddrOfPinnedObject(), _data, position + sizeof(bool) + sizeof(int), len);
                handle.Free();
            }
        }

        /// <summary>
        /// Write an array of strings to the ByteWriter.
        /// </summary>
        /// <param name="s">The array of strings to write.</param>
        public void Write(string[] s)
        {
            Write(s == null);
            if (s == null)
                return;

            while (_position + sizeof(int) > _data.Length)
                Array.Resize(ref _data, _data.Length * 2);

            Write(s.Length);

            for (int i = 0; i < s.Length; i++)
                Write(s[i]);
        }

        /// <summary>
        /// Write an array of strings to the ByteWriter.
        /// </summary>
        /// <param name="s">The array of strings to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(string[] s, int position)
        {
            Write(s == null, position);
            if (s == null)
                return;

            while (position + sizeof(bool) + sizeof(int) > _data.Length)
                Array.Resize(ref _data, _data.Length * 2);

            Write(s.Length, position + sizeof(bool));

            for (int i = 0, tempPosition = 0; i < s.Length; tempPosition += sizeof(char) * s[i].Length + sizeof(bool), i++)
            {
                Write(s[i], position + sizeof(bool) + tempPosition);
            }
        }

        #endregion


        #region WRITE ENUMS

        /// <summary>
        /// Write an Enum to the ByteWriter.
        /// </summary>
        /// <param name="e">The Enum to write.</param>
        public void Write(Enum e)
        {
            Dictionary<Type, Action> @switch = new Dictionary<Type, Action>()
            {
                { typeof(byte), () => { WritePrimitive((byte)(object)e); } },
                { typeof(sbyte), () => { WritePrimitive((sbyte)(object)e); } },
                { typeof(short), () => { WritePrimitive((short)(object)e); } },
                { typeof(ushort), () => { WritePrimitive((ushort)(object)e); } },
                { typeof(int), () => { WritePrimitive((int)(object)e); } },
                { typeof(uint), () => { WritePrimitive((uint)(object)e); } },
                { typeof(long), () => { WritePrimitive((long)(object)e); } },
                { typeof(ulong), () => { WritePrimitive((ulong)(object)e); } },
            };

            Type underlyingType = Enum.GetUnderlyingType(e.GetType());
            @switch[underlyingType]();
        }

        /// <summary>
        /// Write an Enum to the ByteWriter.
        /// </summary>
        /// <param name="e">The Enum to write.</param>
        /// <param name="position">The position within the ByteWriter to write to.</param>
        public void Write(Enum e, int position)
        {
            Dictionary<Type, Action> @switch = new Dictionary<Type, Action>()
            {
                { typeof(byte), () => { WritePrimitive((byte)(object)e, position); } },
                { typeof(sbyte), () => { WritePrimitive((sbyte)(object)e, position); } },
                { typeof(short), () => { WritePrimitive((short)(object)e, position); } },
                { typeof(ushort), () => { WritePrimitive((ushort)(object)e, position); } },
                { typeof(int), () => { WritePrimitive((int)(object)e, position); } },
                { typeof(uint), () => { WritePrimitive((uint)(object)e, position); } },
                { typeof(long), () => { WritePrimitive((long)(object)e, position); } },
                { typeof(ulong), () => { WritePrimitive((ulong)(object)e, position); } },
            };

            Type underlyingType = Enum.GetUnderlyingType(e.GetType());
            @switch[underlyingType]();
        }

        #endregion


        #region WRITE OBJECTS

        /// <summary>
        /// Write a <see cref="TypeHashCode"/> to the ByteWriter.
        /// </summary>
        /// <param name="typeHashCode">The <see cref="TypeHashCode"/> to write.</param>
        internal void Write(TypeHashCode typeHashCode)
        {
            ulong hashcode = (ulong)typeHashCode;
            Write(hashcode);

            if (typeHashCode.IsGenericType)
            {
                for (int i = 0; i < typeHashCode.GenericArgumentCount; i++)
                {
                    Write(typeHashCode[i]);
                }
            }
        }

        /// <summary>
        /// Write an object to the ByteWriter excluding type information.
        /// </summary>
        /// <param name="obj">The object to write.</param>
        internal void WriteWithoutTypeInfo(object obj)
        {
            Write(obj == null);
            if (obj == null)
                return;

            Type type = obj.GetType();

            Serializer serializer = this.SerializerResolver.GetSerializer(type);
            serializer.Serialize(this, obj);
        }

        /// <summary>
        /// Write an object to the ByteWriter.
        /// </summary>
        /// <param name="obj">The object to write.</param>
        public void Write(object obj)
        {
            Write(obj == null);
            if (obj == null)
                return;

            Type type = obj.GetType();

            TypeHashCode typeHashCode = type.GetTypeHashCode();
            Write(typeHashCode);

            Serializer serializer = this.SerializerResolver.GetSerializer(type);
            serializer.Serialize(this, obj);
        }

        /// <summary>
        /// Write an array of objects to the ByteWriter.
        /// </summary>
        /// <param name="obj">The array of objects to write.</param>
        public void Write(object[] obj)
        {
            Write(obj == null);
            if (obj == null)
                return;

            Write(obj.Length);

            for (int i = 0; i < obj.Length; i++)
            {
                Write(obj[i]);
            }
        }


        /// <summary>
        /// Write the contents of another ByteWriter to the ByteWriter.
        /// </summary>
        /// <param name="other">The other ByteWriter.</param>
        /// <remarks>
        /// Using this overload is more efficient than writing the <see cref="Data"/> Property of another ByteWriter to the ByteWriter.
        /// </remarks>
        internal void Write(ByteWriter other)
        {
            while (this._position + other._position > this._data.Length)
                Array.Resize(ref this._data, this._data.Length * 2);

            for (int i = 0; i < other._position; i++)
            {
                this._data[this._position] = other._data[i];
                this._position++;
            }
        }

        #endregion


        #region MISC

        /// <summary>
        /// Merge another ByteWriter's data into this one and return the new array of bytes.
        /// </summary>
        /// <param name="other">The other ByteWriter.</param>
        /// <returns>A array of bytes.</returns>
        internal byte[] MergeData(ByteWriter other)
        {
            byte[] buffer = new byte[Position + other.Position];

            unsafe
            {
                //Write self
                GCHandle handle = GCHandle.Alloc(_data, GCHandleType.Pinned);
                Marshal.Copy(handle.AddrOfPinnedObject(), buffer, 0, Position);
                handle.Free();

                //Write other
                handle = GCHandle.Alloc(other._data, GCHandleType.Pinned);
                Marshal.Copy(handle.AddrOfPinnedObject(), buffer, Position, other.Position);
                handle.Free();
            }
            return buffer;
        }

        /// <summary>
        /// Set the write position of the ByteWriter.
        /// </summary>
        /// <param name="newPosition">The new write position.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="newPosition"/> is below 0 or greater than the length of the internal data stream.</exception>
        internal void SetPosition(int newPosition)
        {
            if (newPosition < 0 || newPosition > _data.Length)
                throw new ArgumentOutOfRangeException(nameof(newPosition), string.Format(EXCP_POS_RANGE, newPosition));

            this._position = newPosition;
        }

        /// <summary>
        /// Reset the ByteWriter.
        /// </summary>
        internal void Reset()
        {
            this._position = 0;
        }

        #endregion


        #region IENUMERABLE IMPLEMENTATION

        public IEnumerator<byte> GetEnumerator()
        {
            for (int i = 0; i < this.Length; i++)
            {
                yield return this._data[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < this.Length; i++)
            {
                yield return this._data[i];
            }
        }

        #endregion


        #region HELPERS

        /// <summary>
        /// Write a primitive value to the internal data stream.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="val">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WritePrimitive<T>(T val) where T : struct
        {
            while (_position + SizeOf<T>() > _data.Length)
                Array.Resize(ref _data, _data.Length * 2);

            unsafe
            {
                GCHandle handle = GCHandle.Alloc(val, GCHandleType.Pinned);
                Marshal.Copy(handle.AddrOfPinnedObject(), _data, _position, SizeOf<T>());
                handle.Free();

                _position += SizeOf<T>();
            }
        }

        /// <summary>
        /// Write a primitive value to the internal data stream at <paramref name="position"/>.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="val">The value.</param>
        /// <param name="position">The position to write to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WritePrimitive<T>(T val, int position) where T : struct
        {
            if (position + SizeOf<T>() > _data.Length)
                throw new ArgumentException(string.Format(EXCP_INVALID_POS, position), nameof(position));

            unsafe
            {
                GCHandle handle = GCHandle.Alloc(val, GCHandleType.Pinned);
                Marshal.Copy(handle.AddrOfPinnedObject(), _data, position, SizeOf<T>());
                handle.Free();
            }
        }

        /// <summary>
        /// Write an array of primitive values to the internal data stream.
        /// </summary>
        /// <typeparam name="T">The type of the values.</typeparam>
        /// <param name="arr">The values.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WritePrimitiveArray<T>(T[] arr) where T : struct
        {
            int len = SizeOf<T>() * arr.Length;

            while (_position + sizeof(int) + len > _data.Length)
                Array.Resize(ref _data, _data.Length * 2);

            unsafe
            {
                //Write length of array
                GCHandle handle = GCHandle.Alloc(arr.Length, GCHandleType.Pinned);
                Marshal.Copy(handle.AddrOfPinnedObject(), _data, _position, sizeof(int));
                handle.Free();

                //Write array
                handle = GCHandle.Alloc(arr, GCHandleType.Pinned);
                Marshal.Copy(handle.AddrOfPinnedObject(), _data, _position + sizeof(int), len);
                handle.Free();

                _position += sizeof(int) + len;
            }
        }

        /// <summary>
        /// Write an array of primitive values to the internal data stream at <paramref name="position"/>.
        /// </summary>
        /// <typeparam name="T">The type of the values.</typeparam>
        /// <param name="arr">The values.</param>
        /// <param name="position">The position to write to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WritePrimitiveArray<T>(T[] arr, int position) where T : struct
        {
            int len = SizeOf<T>() * arr.Length;

            if (position + sizeof(int) + len > _data.Length)
                throw new ArgumentException(string.Format(EXCP_INVALID_POS, position), nameof(position));

            unsafe
            {
                //Write length of array
                GCHandle handle = GCHandle.Alloc(arr.Length, GCHandleType.Pinned);
                Marshal.Copy(handle.AddrOfPinnedObject(), _data, position, sizeof(int));
                handle.Free();

                //Write array
                handle = GCHandle.Alloc(arr, GCHandleType.Pinned);
                Marshal.Copy(handle.AddrOfPinnedObject(), _data, position + sizeof(int), len);
                handle.Free();
            }
        }

        #endregion

    }
}
