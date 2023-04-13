using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parcel.Serialization.Binary;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parcel.Serialization.Tests
{
    [TestClass]
    public class StringSerializerTests : BaseSerializerTest
    {
        [TestMethod]
        public override void CanSerializeTest()
        {
            StringSerializer serializer = new StringSerializer();

            Assert.IsTrue(serializer.CanSerialize(typeof(string)));
            Assert.IsFalse(serializer.CanSerialize(typeof(bool)));
            Assert.IsFalse(serializer.CanSerialize(typeof(string[])));
        }

        [TestMethod]
        public override void SerializeAndDeserializeTest()
        {
            SADString(null);
            SADString(string.Empty);
            SADString("Hello World!");
        }

        private void SADString(string value)
        {
            StringSerializer serializer = new StringSerializer();
            serializer.ObjectCache = ObjectCache.FromType(typeof(string));

            BinaryWriter writer = new BinaryWriter(BinarySerializerResolver.Default);
            serializer.Serialize(writer, value);

            BinaryReader reader = new BinaryReader(BinarySerializerResolver.Default, writer.Data);
            string compareVal = reader.ReadString();

            Assert.AreEqual(value, compareVal);
        }
    }
}
