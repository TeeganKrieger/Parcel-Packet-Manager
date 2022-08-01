using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            ByteWriter writer = new ByteWriter();
            ObjectSerializer serializer = new ObjectSerializer();
            ParcelTestObject testObj = ParcelTestObject.Random();

            serializer.ObjectCache = ObjectCache.FromType(typeof(ParcelTestObject));
            serializer.Serialize(writer, testObj);

            ByteReader reader = new ByteReader(writer.Data);
            ParcelTestObject compareObj = (ParcelTestObject)serializer.Deserialize(reader);

            Assert.IsTrue(testObj.Equals(compareObj));
        }
    }
}
