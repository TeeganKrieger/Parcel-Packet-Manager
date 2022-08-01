using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parcel.Serialization.Tests
{
    [TestClass]
    public class IGenericCollectionSerializerTests : BaseSerializerTest
    {
        [TestMethod]
        public override void CanSerializeTest()
        {
            IGenericCollectionSerializer serializer = new IGenericCollectionSerializer();

            Assert.IsTrue(serializer.CanSerialize(typeof(List<int>)));
            Assert.IsFalse(serializer.CanSerialize(typeof(Queue<string>)));
            Assert.IsTrue(serializer.CanSerialize(typeof(HashSet<object>)));
            Assert.IsFalse(serializer.CanSerialize(typeof(Stack<ParcelTestObject>)));
            Assert.IsFalse(serializer.CanSerialize(typeof(Dictionary<int, string>)));
            Assert.IsFalse(serializer.CanSerialize(typeof(int[])));
        }

        [TestMethod]
        public override void SerializeAndDeserializeTest()
        {
            List<string> testList = new List<string>() { "Hello", "World", "this", "is", "a", "test" };
            SADObject(testList);

            HashSet<int> testQueue = new HashSet<int>(new int[] { 86, 72, 56, 420, 17, 9999 });
            SADObject<int>(testQueue);
        }

        private void SADObject<T>(ICollection<T> collection)
        {
            IGenericCollectionSerializer serializer = new IGenericCollectionSerializer();
            serializer.ObjectCache = ObjectCache.FromType(collection.GetType());

            ByteWriter writer = new ByteWriter();
            writer.Write(collection);

            ByteReader reader = new ByteReader(writer.Data);
            ICollection<T> compareObject = (ICollection<T>)reader.ReadObject();
            Assert.AreEqual(collection.Count, compareObject.Count);
            
            IEnumerator<T> collectionEnumerator = collection.GetEnumerator();
            IEnumerator<T> compareEnumerator = compareObject.GetEnumerator();

            while (collectionEnumerator.MoveNext() && compareEnumerator.MoveNext())
                Assert.AreEqual(collectionEnumerator.Current, compareEnumerator.Current);
        }
    }
}
