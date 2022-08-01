using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parcel.Serialization.Tests
{
    [TestClass]
    public class EnumSerializerTests : BaseSerializerTest
    {
        private enum TestEnumByte : byte
        {
            Hello = 0,
            World = 1,
            My = 2,
            Name = 3,
            Is = 4,
            John = 5
        }

        private enum TestEnumShort : short
        {
            Hello = 0,
            World = 1,
            My = 2,
            Name = 3,
            Is = 4,
            John = 5
        }

        private enum TestEnumInt : int
        {
            Hello = 0,
            World = 1,
            My = 2,
            Name = 3,
            Is = 4,
            John = 5
        }

        [TestMethod]
        public override void CanSerializeTest()
        {
            EnumSerializer serializer = new EnumSerializer();

            Assert.IsTrue(serializer.CanSerialize(typeof(TestEnumByte)));
            Assert.IsTrue(serializer.CanSerialize(typeof(TestEnumShort)));
            Assert.IsTrue(serializer.CanSerialize(typeof(TestEnumInt)));
            Assert.IsFalse(serializer.CanSerialize(typeof(int)));
            Assert.IsFalse(serializer.CanSerialize(typeof(string)));
            Assert.IsFalse(serializer.CanSerialize(typeof(object)));
        }

        [TestMethod]
        public override void SerializeAndDeserializeTest()
        {
            SADEnum(TestEnumByte.John);
            SADEnum(TestEnumByte.Hello);
            SADEnum(TestEnumByte.Is);
            SADEnum(TestEnumShort.John);
            SADEnum(TestEnumShort.World);
            SADEnum(TestEnumShort.My);
            SADEnum(TestEnumInt.Name);
            SADEnum(TestEnumInt.World);
            SADEnum(TestEnumInt.John);
        }

        private void SADEnum(Enum value)
        {
            EnumSerializer serializer = new EnumSerializer();
            serializer.ObjectCache = ObjectCache.FromType(value.GetType());

            ByteWriter writer = new ByteWriter();
            serializer.Serialize(writer, value);

            ByteReader reader = new ByteReader(writer.Data);
            Enum compareVal = reader.ReadEnum(value.GetType());

            Assert.AreEqual(value, compareVal);
        }
    }
}
