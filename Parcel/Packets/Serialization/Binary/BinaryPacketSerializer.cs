using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parcel.Serialization;
using Parcel.Serialization.Binary;

namespace Parcel.Packets
{
    public class BinaryPacketSerializer : SerializerV2, IBinarySerializer
    {
        public override bool CanSerialize(Type type)
        {
            throw new NotImplementedException();
        }

        public override object Deserialize(DataReader reader)
        {
            throw new NotImplementedException();
        }

        public override void Serialize(DataWriter writer, object obj)
        {
            throw new NotImplementedException();
        }
    }
}
