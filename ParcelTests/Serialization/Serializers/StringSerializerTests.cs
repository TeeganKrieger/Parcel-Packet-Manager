using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parcel.Serialization.Tests
{
    [TestClass]
    public class StringSerializerTests
    {
        [TestMethod]
        public void StringTest()
        {
            string testString = "Hello World!";

            ByteWriter writer = new ByteWriter();
            StringSerializer serializer = new StringSerializer();

            serializer.Serialize(writer, testString);

            ByteReader reader = new ByteReader(writer.Data);
            string compareString = reader.ReadString();

            Console.WriteLine($"TestString: {testString}");
            Console.WriteLine($"CompareString: {compareString}");
            Assert.AreEqual(testString, compareString);
        }
    }
}
