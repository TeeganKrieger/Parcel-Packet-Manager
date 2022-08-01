using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parcel.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Parcel.Lib.Tests
{
    [TestClass]
    public class TypeHashCodeTests
    {

        [TestMethod]
        public void ArrayTypeHashCodeTest()
        {
            TypeHashCode thc = typeof(string[,,,,,]).GetTypeHashCode();

            Assert.IsTrue(thc.IsArrayType);
            Assert.AreEqual(thc.ArrayRank, 6);

            typeof(TypeHashCode).GetField("TypeLookupTable", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, new Dictionary<TypeHashCode, Type>());

            Type parsed = TypeHashCode.ParseType(thc);

            Assert.AreEqual(typeof(string[,,,,,]), parsed);
        }

        [TestMethod]
        public void NestedGenericTypeHashCodeTest()
        {
            TypeHashCode thc = typeof(Dictionary<int, List<string>>).GetTypeHashCode();

            Assert.IsTrue(thc.IsGenericType);
            Assert.AreEqual(thc.GenericArgumentCount, 2);

            typeof(TypeHashCode).GetField("TypeLookupTable", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, new Dictionary<TypeHashCode, Type>());

            Type parsed = TypeHashCode.ParseType(thc);

            Assert.AreEqual(typeof(Dictionary<int, List<string>>), parsed);
            
        }
    }
}
