using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Parcel.Lib.CorrectedSizeOf;

namespace Parcel.Serialization.Binary
{
    public sealed class BinaryWriter : DataWriter
    {
        private byte[] _data;

        public override int InternalLength => this._data.Length;
        public override byte[] Data => this._data;

        public BinaryWriter(SerializerResolverV2 serializerResolver) : base(serializerResolver) 
        {
            this._data = new byte[16];
        }


        #region ABSTRACT IMPLEMENTATION


        #region PRIMITIVES

        public override void Write(byte b)
        {
            this.WritePrimitive(b);
        }

        public override void Write(sbyte b)
        {
            this.WritePrimitive(b);
        }

        public override void Write(short s)
        {
            this.WritePrimitive(s);
        }

        public override void Write(ushort s)
        {
            this.WritePrimitive(s);
        }

        public override void Write(int i)
        {
            this.WritePrimitive(i);
        }

        public override void Write(uint i)
        {
            this.WritePrimitive(i);
        }

        public override void Write(long l)
        {
            this.WritePrimitive(l);
        }

        public override void Write(ulong l)
        {
            this.WritePrimitive(l);
        }

        public override void Write(float f)
        {
            this.WritePrimitive(f);
        }

        public override void Write(double d)
        {
            this.WritePrimitive(d);
        }

        public override void Write(decimal d)
        {
            int[] bits = decimal.GetBits(d);

            int len = SizeOf<int>() * 4;

            while (this._position + len > this._data.Length)
                Array.Resize(ref this._data, this._data.Length * 2);

            unsafe
            {
                //Write array
                GCHandle handle = GCHandle.Alloc(bits, GCHandleType.Pinned);
                Marshal.Copy(handle.AddrOfPinnedObject(), this._data, this._position, len);
                handle.Free();

                this._position += len;
            }
        }

        public override void Write(bool b)
        {
            this.WritePrimitive(b ? (byte)1 : (byte)0);
        }

        public override void Write(char c)
        {
            this.WritePrimitive((ushort)c);
        }

        #endregion


        #region PRIMITIVES AT POSITION

        public override void Write(byte b, int position)
        {
            this.WritePrimitive(b, position);
        }

        public override void Write(sbyte b, int position)
        {
            this.WritePrimitive(b, position);
        }

        public override void Write(short s, int position)
        {
            this.WritePrimitive(s, position);
        }

        public override void Write(ushort s, int position)
        {
            this.WritePrimitive(s, position);
        }

        public override void Write(int i, int position)
        {
            this.WritePrimitive(i, position);
        }

        public override void Write(uint i, int position)
        {
            this.WritePrimitive(i, position);
        }

        public override void Write(long l, int position)
        {
            this.WritePrimitive(l, position);
        }

        public override void Write(ulong l, int position)
        {
            this.WritePrimitive(l, position);
        }

        public override void Write(float f, int position)
        {
            this.WritePrimitive(f, position);
        }

        public override void Write(double d, int position)
        {
            this.WritePrimitive(d, position);
        }

        public override void Write(decimal d, int position)
        {
            int[] bits = decimal.GetBits(d);

            int len = SizeOf<int>() * 4;

            if (position + len > this._data.Length)
                Array.Resize(ref this._data, this._data.Length * 2);//throw new ArgumentException(string.Format(EXCP_INVALID_POS, position), nameof(position));

            unsafe
            {
                //Write array
                GCHandle handle = GCHandle.Alloc(bits, GCHandleType.Pinned);
                Marshal.Copy(handle.AddrOfPinnedObject(), this._data, position, len);
                handle.Free();
            }
        }

        public override void Write(bool b, int position)
        {
            this.WritePrimitive(b ? (byte)1 : (byte)0, position);
        }

        public override void Write(char c, int position)
        {
            this.WritePrimitive(c, position);
        }

        #endregion


        #region PRIMITIVE ARRAYS

        public override void Write(byte[] b)
        {
            this.WritePrimitiveArray(b);
        }

        public override void Write(sbyte[] b)
        {
            this.WritePrimitiveArray(b);
        }

        public override void Write(short[] s)
        {
            this.WritePrimitiveArray(s);
        }

        public override void Write(ushort[] s)
        {
            this.WritePrimitiveArray(s);
        }

        public override void Write(int[] i)
        {
            this.WritePrimitiveArray(i);
        }

        public override void Write(uint[] i)
        {
            this.WritePrimitiveArray(i);
        }

        public override void Write(long[] l)
        {
            this.WritePrimitiveArray(l);
        }

        public override void Write(ulong[] l)
        {
            this.WritePrimitiveArray(l);
        }

        public override void Write(float[] f)
        {
            this.WritePrimitiveArray(f);
        }

        public override void Write(double[] d)
        {
            this.WritePrimitiveArray(d);
        }

        public override void Write(decimal[] d)
        {
            this.Write(d == null);
            if (d == null)
                return;

            int len = SizeOf<int>() * 4 * d.Length;

            int[] bits;

            while (this._position + sizeof(int) + len > this._data.Length)
                Array.Resize(ref this._data, this._data.Length * 2);

            unsafe
            {
                //Write length of array
                GCHandle handle = GCHandle.Alloc(d.Length, GCHandleType.Pinned);
                Marshal.Copy(handle.AddrOfPinnedObject(), this._data, this._position, sizeof(int));
                handle.Free();
                this._position += sizeof(int);
            }

            for (int i = 0; i < d.Length; i++)
            {
                bits = decimal.GetBits(d[i]);

                unsafe
                {
                    //Write array
                    GCHandle handle = GCHandle.Alloc(bits, GCHandleType.Pinned);
                    Marshal.Copy(handle.AddrOfPinnedObject(), this._data, this._position, SizeOf<int>() * 4);
                    handle.Free();

                    this._position += SizeOf<int>() * 4;
                }
            }
        }

        public override void Write(bool[] b)
        {
            if (b == null)
            {
                this.WritePrimitiveArray<byte>(null);
                return;
            }

            byte[] bools = new byte[b.Length];

            for (int i = 0; i < b.Length; i++)
                bools[i] = b[i] ? (byte)1 : (byte)0;

            this.WritePrimitiveArray(bools);
        }

        public override void Write(char[] c)
        {
            this.WritePrimitiveArray(c);
        }

        #endregion


        #region STRINGS

        public override void Write(string s)
        {
            this.Write(s == null);
            if (s == null)
                return;

            int len = sizeof(char) * s.Length;

            while (this._position + sizeof(int) + len > this._data.Length)
                Array.Resize(ref this._data, this._data.Length * 2);

            unsafe
            {
                //Write length of string
                GCHandle handle = GCHandle.Alloc(len, GCHandleType.Pinned);
                Marshal.Copy(handle.AddrOfPinnedObject(), this._data, this._position, sizeof(int));
                handle.Free();

                //Write string
                handle = GCHandle.Alloc(s, GCHandleType.Pinned);
                Marshal.Copy(handle.AddrOfPinnedObject(), this._data, this._position + sizeof(int), len);
                handle.Free();

                this._position += sizeof(int) + len;
            }
        }

        public override void Write(string[] s)
        {
            this.Write(s == null);
            if (s == null)
                return;

            while (this._position + sizeof(int) > this._data.Length)
                Array.Resize(ref this._data, this._data.Length * 2);

            WritePrimitive(s.Length);

            for (int i = 0; i < s.Length; i++)
                Write(s[i]);
        }

        #endregion


        #region ENUMS

        public override void Write(Enum e)
        {
            Dictionary<Type, Action> @switch = new Dictionary<Type, Action>()
            {
                { typeof(byte), () => { this.WritePrimitive((byte)(object)e); } },
                { typeof(sbyte), () => { this.WritePrimitive((sbyte)(object)e); } },
                { typeof(short), () => { this.WritePrimitive((short)(object)e); } },
                { typeof(ushort), () => { this.WritePrimitive((ushort)(object)e); } },
                { typeof(int), () => { this.WritePrimitive((int)(object)e); } },
                { typeof(uint), () => { this.WritePrimitive((uint)(object)e); } },
                { typeof(long), () => { this.WritePrimitive((long)(object)e); } },
                { typeof(ulong), () => { this.WritePrimitive((ulong)(object)e); } },
            };

            Type underlyingType = Enum.GetUnderlyingType(e.GetType());
            @switch[underlyingType]();
        }

        public override void Write(Enum e, int position)
        {
            Dictionary<Type, Action> @switch = new Dictionary<Type, Action>()
            {
                { typeof(byte), () => { this.WritePrimitive((byte)(object)e, position); } },
                { typeof(sbyte), () => { this.WritePrimitive((sbyte)(object)e, position); } },
                { typeof(short), () => { this.WritePrimitive((short)(object)e, position); } },
                { typeof(ushort), () => { this.WritePrimitive((ushort)(object)e, position); } },
                { typeof(int), () => { this.WritePrimitive((int)(object)e, position); } },
                { typeof(uint), () => { this.WritePrimitive((uint)(object)e, position); } },
                { typeof(long), () => { this.WritePrimitive((long)(object)e, position); } },
                { typeof(ulong), () => { this.WritePrimitive((ulong)(object)e, position); } },
            };

            Type underlyingType = Enum.GetUnderlyingType(e.GetType());
            @switch[underlyingType]();
        }

        public override void Write(Enum[] e)
        {
            throw new NotImplementedException();
        }

        #endregion


        #region OBJECTS

        public override void Write(object o, bool writeTypeInformation = true)
        {
            this.Write(o == null);
            if (o == null)
                return;

            Type type = o.GetType();

            if (writeTypeInformation && type != typeof(TypeHashCode))
            {
                TypeHashCode typeHashCode = type.GetTypeHashCode();
                Write(typeHashCode, false);
            }

            SerializerV2 serializer = this.SerializerResolver.GetSerializer(type);
            serializer.Serialize(this, o);
        }

        public override void Write(object[] o, bool writeTypeInformation = true)
        {
            this.Write(o == null);
            if (o == null)
                return;

            this.WritePrimitive(o.Length);

            for (int i = 0; i < o.Length; i++)
            {
                this.Write(o[i], writeTypeInformation);
            }
        }

        #endregion


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
            while (_position + SizeOf<T>() > this._data.Length)
                Array.Resize(ref this._data, this._data.Length * 2);

            unsafe
            {
                GCHandle handle = GCHandle.Alloc(val, GCHandleType.Pinned);
                Marshal.Copy(handle.AddrOfPinnedObject(), this._data, this._position, SizeOf<T>());
                handle.Free();

                this._position += SizeOf<T>();
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
            if (position + SizeOf<T>() > this._data.Length)
                Array.Resize(ref this._data, this._data.Length * 2);//throw new ArgumentException(string.Format(EXCP_INVALID_POS, position), nameof(position));

            unsafe
            {
                GCHandle handle = GCHandle.Alloc(val, GCHandleType.Pinned);
                Marshal.Copy(handle.AddrOfPinnedObject(), this._data, position, SizeOf<T>());
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
            this.Write(arr == null);
            if (arr == null)
                return;

            int len = SizeOf<T>() * arr.Length;

            while (this._position + sizeof(int) + len > this._data.Length)
                Array.Resize(ref this._data, this._data.Length * 2);

            unsafe
            {
                //Write length of array
                GCHandle handle = GCHandle.Alloc(arr.Length, GCHandleType.Pinned);
                Marshal.Copy(handle.AddrOfPinnedObject(), this._data, this._position, sizeof(int));
                handle.Free();

                //Write array
                handle = GCHandle.Alloc(arr, GCHandleType.Pinned);
                Marshal.Copy(handle.AddrOfPinnedObject(), this._data, this._position + sizeof(int), len);
                handle.Free();

                this._position += sizeof(int) + len;
            }
        }

        #endregion


        #region IENUMERABLE

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
