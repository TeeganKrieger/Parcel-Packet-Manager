using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parcel.Serialization.Binary;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parcel.Serialization.Tests
{
    [TestClass]
    public class IGenericDictionarySerializerTests : BaseSerializerTest
    {
        [TestMethod]
        public override void CanSerializeTest()
        {
            IGenericDictionarySerializer serializer = new IGenericDictionarySerializer();
            Assert.IsTrue(serializer.CanSerialize(typeof(Dictionary<string, int>)));
            Assert.IsFalse(serializer.CanSerialize(typeof(List<KeyValuePair<string, int>>)));
            Assert.IsTrue(serializer.CanSerialize(typeof(Dictionary<object, ParcelTestObject>)));
            Assert.IsFalse(serializer.CanSerialize(typeof(int)));
            Assert.IsTrue(serializer.CanSerialize(typeof(Dictionary<int, object>)));
            Assert.IsFalse(serializer.CanSerialize(typeof(HashSet<string>)));
        }

        [TestMethod]
        public override void SerializeAndDeserializeTest()
        {
            Dictionary<int, ParcelTestObject> referenceDict = new Dictionary<int, ParcelTestObject>()
            {
                { 0, ParcelTestObject.Random() },
                { 1, ParcelTestChild.Random() },
                { 2, ParcelTestObject.Random() }
            };
            SADObject(referenceDict);

            Dictionary<int, float> valueDict = new Dictionary<int, float>()
            {
                {0, 72.6758f },
                {1, 42.222222f },
                {3, float.PositiveInfinity }
            };
            SADObject(valueDict);
        }

        private void SADObject<T1, T2>(Dictionary<T1, T2> dict)
        {
            IGenericDictionarySerializer serializer = new IGenericDictionarySerializer();
            serializer.ObjectCache = ObjectCache.FromType(dict.GetType());

            BinaryWriter writer = new BinaryWriter(BinarySerializerResolver.Default);
            writer.Write(dict);

            BinaryReader reader = new BinaryReader(BinarySerializerResolver.Default, writer.Data);
            Dictionary<T1, T2> compareObject = (Dictionary<T1, T2>)reader.ReadObject();
            Assert.AreEqual(dict.Count, compareObject.Count);

            foreach (KeyValuePair<T1, T2> pair in dict)
            {
                Assert.IsTrue(compareObject.TryGetValue(pair.Key, out T2 value));
                Assert.AreEqual(pair.Value, value);
            }
        }

    }
}
