using System;
using System.Collections.Generic;
using System.Text;

namespace Parcel.Serialization.Tests
{
    
    public abstract class BaseSerializerTest
    {

        public abstract void CanSerializeTest();

        public abstract void SerializeAndDeserializeTest();

    }
}
