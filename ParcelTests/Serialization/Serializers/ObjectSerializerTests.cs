using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parcel.Serialization.Binary;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parcel.Serialization.Tests
{
    [TestClass]
    public class ObjectSerializerTests
    {

        [TestMethod]
        public void ObjectTest()
        {
            BinaryWriter writer = new BinaryWriter(BinarySerializerResolver.Default);
            ObjectSerializer serializer = new ObjectSerializer();
            ParcelTestObject testObj = ParcelTestObject.Random();

            serializer.ObjectCache = ObjectCache.FromType(typeof(ParcelTestObject));
            serializer.Serialize(writer, testObj);

            BinaryReader reader = new BinaryReader(BinarySerializerResolver.Default, writer.Data);
            ParcelTestObject compareObj = (ParcelTestObject)serializer.Deserialize(reader);

            Assert.IsTrue(testObj.Equals(compareObj));
        }
    }
}
