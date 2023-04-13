using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parcel.Serialization.Binary;
using Parcel.Serialization.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parcel.Serialization.Tests
{
    [TestClass]
    public class ObjectArraySerializerTests : BaseSerializerTest
    {
        [TestMethod]
        public override void CanSerializeTest()
        {
            ObjectArraySerializer serializer = new ObjectArraySerializer();

            Assert.IsTrue(serializer.CanSerialize(typeof(object[])));
            Assert.IsTrue(serializer.CanSerialize(typeof(string[])));
            Assert.IsTrue(serializer.CanSerialize(typeof(int[])));
            Assert.IsFalse(serializer.CanSerialize(typeof(object)));
            Assert.IsFalse(serializer.CanSerialize(typeof(bool)));
            Assert.IsFalse(serializer.CanSerialize(typeof(string)));
        }

        [TestMethod]
        public override void SerializeAndDeserializeTest()
        {
            SADObjectArray(null);
            SADObjectArray(new object[] { 1, 2, 3, "Hello", 5f, true });
            SADObjectArray(new object[] { null, 8l, (short)3, null, false, true });
            SADObjectArray(new string[] { "Hello", "World", "Whats", "Up" });
        }

        private void SADObjectArray(object[] value)
        {
            ObjectArraySerializer serializer = new ObjectArraySerializer();
            serializer.ObjectCache = ObjectCache.FromType(typeof(object[]));

            BinaryWriter writer = new BinaryWriter(BinarySerializerResolver.Default);
            serializer.Serialize(writer, value);

            BinaryReader reader = new BinaryReader(BinarySerializerResolver.Default, writer.Data);
            object[] compareVal = reader.ReadObjectArray();

            if (value == null)
            {
                if (compareVal == null)
                    return;
                else
                    Assert.Fail();
            }

            Assert.AreEqual(value.Length, compareVal.Length);

            for (int i = 0; i < compareVal.Length; i++)
            {
                Assert.AreEqual(value[i], compareVal[i]);
            }
        }

    }
}
