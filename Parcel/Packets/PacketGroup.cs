using Parcel.Serialization;

namespace Parcel.Packets
{
    [OptIn]
    internal class PacketGroup
    {
        [Ignore]
        public int Count => Packets.Length;
        [Serialize]
        public Packet[] Packets { get; private set; }

        public PacketGroup(Packet[] packets)
        {
            this.Packets = packets;
        }

    }
}
