using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parcel.Serialization.Tests
{

    [TestClass()]
    public class PrimitiveSerializerTests
    {
        [TestMethod]
        public void ByteTest()
        {
            GenericTest<byte>((byte)new Random().Next(byte.MinValue, byte.MaxValue));
        }

        [TestMethod]
        public void SByteTest()
        {
            GenericTest<sbyte>((sbyte)new Random().Next(sbyte.MinValue, sbyte.MaxValue));
        }

        [TestMethod]
        public void ShortTest()
        {
            GenericTest<short>((short)new Random().Next(short.MinValue, short.MaxValue));
        }

        [TestMethod]
        public void UShortTest()
        {
            GenericTest<ushort>((ushort)new Random().Next(ushort.MinValue, ushort.MaxValue));
        }

        [TestMethod]
        public void IntTest()
        {
            GenericTest<int>(new Random().Next(int.MinValue, int.MaxValue));
        }

        [TestMethod]
        public void UIntTest()
        {
            GenericTest<uint>((uint)new Random().Next(int.MinValue, int.MaxValue));
        }

        [TestMethod]
        public void LongTest()
        {
            GenericTest<long>(new Random().Next(int.MinValue, int.MaxValue));
        }

        [TestMethod]
        public void ULongTest()
        {
            GenericTest<ulong>((ulong)new Random().Next(int.MinValue, int.MaxValue));
        }

        [TestMethod]
        public void FloatTest()
        {
            GenericTest<float>((float)new Random().NextDouble() * float.MaxValue);
        }

        [TestMethod]
        public void DoubleTest()
        {
            GenericTest<double>(new Random().NextDouble() * double.MaxValue);
        }

        [TestMethod]
        public void DecimalTest()
        {
            GenericTest<decimal>((decimal)new Random().NextDouble() * decimal.MaxValue);
        }

        [TestMethod]
        public void CharTest()
        {
            GenericTest<char>((char)new Random().Next(ushort.MinValue, ushort.MaxValue));
        }

        [TestMethod]
        public void BoolTest()
        {
            GenericTest<bool>(new Random().Next(0, 2) == 1);
        }

        private void GenericTest<T>(T testVal) where T : struct
        {
            ByteWriter writer = new ByteWriter();
            PrimitiveSerializer<T> serializer = new PrimitiveSerializer<T>();
            serializer.Serialize(writer, testVal);

            ByteReader reader = new ByteReader(writer.Data);
            T compareVal = (T)serializer.Deserialize(reader);
            Console.WriteLine($"TestVal: {testVal}");
            Console.WriteLine($"CompareVal: {compareVal}");
            Assert.AreEqual(testVal, compareVal);
            Assert.AreEqual(writer.Position, reader.Position);
        }

    }
}
