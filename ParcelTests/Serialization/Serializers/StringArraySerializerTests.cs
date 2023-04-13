using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parcel.Serialization.Binary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parcel.Serialization.Tests
{
    [TestClass]
    public class StringArraySerializerTests : BaseSerializerTest
    {
        [TestMethod]
        public override void CanSerializeTest()
        {
            StringArraySerializer serializer = new StringArraySerializer();

            Assert.IsTrue(serializer.CanSerialize(typeof(string[])));
            Assert.IsFalse(serializer.CanSerialize(typeof(bool)));
            Assert.IsFalse(serializer.CanSerialize(typeof(string)));
        }

        [TestMethod]
        public override void SerializeAndDeserializeTest()
        {
            SADStringArray(null);
            SADStringArray(new string[] { "Hello", "World", "!"});
            SADStringArray(new string[] { "Hello", null, "!", null, null});
        }

        private void SADStringArray(string[] value)
        {
            StringArraySerializer serializer = new StringArraySerializer();
            serializer.ObjectCache = ObjectCache.FromType(typeof(string[]));

            BinaryWriter writer = new BinaryWriter(BinarySerializerResolver.Default);
            serializer.Serialize(writer, value);

            BinaryReader reader = new BinaryReader(BinarySerializerResolver.Default, writer.Data);
            string[] compareVal = reader.ReadStringArray();

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
