using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parcel.Serialization.Binary;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parcel.Serialization.Tests
{
    [TestClass]
    public class PrimitiveArraySerializerTests
    {

        [TestMethod]
        public void ByteArrayTest()
        {
            Random r = new Random();

            byte[] byteArray = new byte[r.Next(25, 260)];

            for (int i = 0; i < byteArray.Length; i++)
                byteArray[i] = (byte)r.Next(byte.MinValue, byte.MaxValue);

            GenericTest(byteArray);
        }

        [TestMethod]
        public void SByteArrayTest()
        {
            Random r = new Random();

            sbyte[] sbyteArray = new sbyte[r.Next(25, 260)];

            for (int i = 0; i < sbyteArray.Length; i++)
                sbyteArray[i] = (sbyte)r.Next(sbyte.MinValue, sbyte.MaxValue);

            GenericTest(sbyteArray);
        }

        [TestMethod]
        public void ShortArrayTest()
        {
            Random r = new Random();

            short[] shortArray = new short[r.Next(25, 260)];

            for (int i = 0; i < shortArray.Length; i++)
                shortArray[i] = (short)r.Next(short.MinValue, short.MaxValue);

            GenericTest(shortArray);
        }

        [TestMethod]
        public void UShortArrayTest()
        {
            Random r = new Random();

            ushort[] ushortArray = new ushort[r.Next(25, 260)];

            for (int i = 0; i < ushortArray.Length; i++)
                ushortArray[i] = (ushort)r.Next(ushort.MinValue, ushort.MaxValue);

            GenericTest(ushortArray);
        }

        [TestMethod]
        public void IntArrayTest()
        {
            Random r = new Random();

            int[] intArray = new int[r.Next(25, 260)];

            for (int i = 0; i < intArray.Length; i++)
                intArray[i] = (int)r.Next(int.MinValue, int.MaxValue);

            GenericTest(intArray);
        }

        [TestMethod]
        public void UIntArrayTest()
        {
            Random r = new Random();

            uint[] uintArray = new uint[r.Next(25, 260)];

            for (int i = 0; i < uintArray.Length; i++)
                uintArray[i] = (uint)r.Next(int.MinValue, int.MaxValue);

            GenericTest(uintArray);
        }

        [TestMethod]
        public void LongArrayTest()
        {
            Random r = new Random();

            long[] longArray = new long[r.Next(25, 260)];

            for (int i = 0; i < longArray.Length; i++)
                longArray[i] = (long)r.Next(int.MinValue, int.MaxValue);

            GenericTest(longArray);
        }

        [TestMethod]
        public void ULongArrayTest()
        {
            Random r = new Random();

            ulong[] ulongArray = new ulong[r.Next(25, 260)];

            for (int i = 0; i < ulongArray.Length; i++)
                ulongArray[i] = (ulong)r.Next(int.MinValue, int.MaxValue);

            GenericTest(ulongArray);
        }

        [TestMethod]
        public void FloatArrayTest()
        {
            Random r = new Random();

            float[] floatArray = new float[r.Next(25, 260)];

            for (int i = 0; i < floatArray.Length; i++)
                floatArray[i] = (float)r.NextDouble() * float.MaxValue;

            GenericTest(floatArray);
        }

        [TestMethod]
        public void DoubleArrayTest()
        {
            Random r = new Random();

            double[] doubleArray = new double[r.Next(25, 260)];

            for (int i = 0; i < doubleArray.Length; i++)
                doubleArray[i] = (double)r.NextDouble() * double.MaxValue;

            GenericTest(doubleArray);
        }

        [TestMethod]
        public void DecimalArrayTest()
        {
            Random r = new Random();

            decimal[] decimalArray = new decimal[r.Next(25, 260)];

            for (int i = 0; i < decimalArray.Length; i++)
                decimalArray[i] = (decimal)r.NextDouble() * decimal.MaxValue;

            GenericTest(decimalArray);
        }

        [TestMethod]
        public void CharArrayTest()
        {
            Random r = new Random();

            char[] charArray = new char[r.Next(25, 260)];

            for (int i = 0; i < charArray.Length; i++)
                charArray[i] = (char)r.Next(ushort.MinValue, ushort.MaxValue);

            GenericTest(charArray);
        }

        [TestMethod]
        public void BoolArrayTest()
        {
            Random r = new Random();

            bool[] boolArray = new bool[r.Next(25, 260)];

            for (int i = 0; i < boolArray.Length; i++)
                boolArray[i] = r.Next(0, 2) == 1 ? true : false;

            GenericTest(boolArray);
        }

        private void GenericTest<T>(T[] testArr) where T : struct
        {
            BinaryWriter writer = new BinaryWriter(BinarySerializerResolver.Default);
            PrimitiveArraySerializer<T> serializer = new PrimitiveArraySerializer<T>();
            serializer.Serialize(writer, testArr);

            BinaryReader reader = new BinaryReader(BinarySerializerResolver.Default, writer.Data);
            T[] compareArr = (T[])serializer.Deserialize(reader);

            Console.WriteLine($"TestArr Length: {testArr.Length}");
            Console.WriteLine($"CompareArr Length: {compareArr.Length}");

            Assert.AreEqual(testArr.Length, compareArr.Length);

            Console.WriteLine("----------------------------------------");

            for (int i = 0; i < compareArr.Length; i++)
            {
                Console.WriteLine($"TestArr[{i}]: {testArr[i]}");
                Console.WriteLine($"CompareArr[{i}]: {compareArr[i]}");
                Assert.AreEqual(testArr[i], compareArr[i]);
            }

            Assert.AreEqual(writer.Position, reader.Position);
        }
    }
}
