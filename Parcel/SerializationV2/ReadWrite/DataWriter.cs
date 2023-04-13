using System;
using System.Collections;
using System.Collections.Generic;

namespace Parcel.Serialization
{
    public abstract class DataWriter : IEnumerable<byte>
    {
        protected int _position;

        public SerializerResolverV2 SerializerResolver { get; private set; }
        public int Position => this._position;
        public int Length => this._position;
        public abstract int InternalLength { get; }
        public abstract byte[] Data { get; }


        #region CONSTRUCTOR

        public DataWriter(SerializerResolverV2 serializerResolver)
        {
            this.SerializerResolver = serializerResolver;
        }

        #endregion


        #region CONCRETE METHODS

        public void SetPosition(int position)
        {
            if (position < 0 || position > this.InternalLength)
                throw new ArgumentOutOfRangeException(nameof(position));

            this._position = position;
        }

        public void Reset()
        {
            this._position = 0;
        }

        #endregion


        #region ABSTRACT METHODS

        public abstract void Write(byte b);
        public abstract void Write(sbyte b);
        public abstract void Write(short s);
        public abstract void Write(ushort s);
        public abstract void Write(int i);
        public abstract void Write(uint i);
        public abstract void Write(long l);
        public abstract void Write(ulong l);
        public abstract void Write(float f);
        public abstract void Write(double d);
        public abstract void Write(decimal d);
        public abstract void Write(bool b);
        public abstract void Write(char c);

        public abstract void Write(byte b, int position);
        public abstract void Write(sbyte b, int position);
        public abstract void Write(short s, int position);
        public abstract void Write(ushort s, int position);
        public abstract void Write(int i, int position);
        public abstract void Write(uint i, int position);
        public abstract void Write(long l, int position);
        public abstract void Write(ulong l, int position);
        public abstract void Write(float f, int position);
        public abstract void Write(double d, int position);
        public abstract void Write(decimal d, int position);
        public abstract void Write(bool b, int position);
        public abstract void Write(char c, int position);

        public abstract void Write(byte[] b);
        public abstract void Write(sbyte[] b);
        public abstract void Write(short[] s);
        public abstract void Write(ushort[] s);
        public abstract void Write(int[] i);
        public abstract void Write(uint[] i);
        public abstract void Write(long[] l);
        public abstract void Write(ulong[] l);
        public abstract void Write(float[] f);
        public abstract void Write(double[] d);
        public abstract void Write(decimal[] d);
        public abstract void Write(bool[] b);
        public abstract void Write(char[] c);

        public abstract void Write(string s);
        public abstract void Write(string[] s);

        public abstract void Write(Enum e);
        public abstract void Write(Enum e, int position);
        public abstract void Write(Enum[] e);

        public abstract void Write(object o, bool writeTypeInformation = true);
        public abstract void Write(object[] o, bool writeTypeInformation = true);

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
