using Parcel.Serialization;
using System;

namespace Parcel.Networking
{
    public sealed partial class UDPTransportLayer
    {

        /// <summary>
        /// Helps with constructing and interpreting packet headers.
        /// </summary>
        private static class HeaderHelper
        {
            private const string EXCP_INVALID_RELIABLE = "Failed to create packet because the selected packet type ({0}) is not valid with reliable packets.";
            private const string EXCP_INVALID_UNRELIABLE = "Failed to create packet because the selected packet type ({0}) is not valid with unreliable packets.";

            private const byte UNRELIABLE_MASK = 0b0000_0000;
            private const byte RELIABLE_MASK = 0b1000_0000;
            private const byte PACKET_TYPE_MASK = 0b0111_1111;

            /// <summary>
            /// Create a reliable packet of the desired type.
            /// </summary>
            /// <param name="reliability">The reliability of this packet.</param>
            /// <param name="packetType">The type of packet to create.</param>
            /// <param name="sequenceNumber">The sequence number of the packet.</param>
            /// <param name="payload">A <see cref="ByteWriter"/> containing the packet payload.</param>
            /// <returns>A byte array containing the packet.</returns>
            /// <exception cref="ArgumentException">Thrown if the combination of <paramref name="reliability"/> and <paramref name="packetType"/> are incompatible.</exception>
            public static byte[] CreatePacket(Reliability reliability, PacketType packetType, int sequenceNumber, ByteWriter payload = null)
            {
                if (reliability == Reliability.Reliable && packetType == PacketType.Acknowledgment)
                    throw new ArgumentException(string.Format(EXCP_INVALID_RELIABLE, packetType), nameof(packetType));
                else if (reliability == Reliability.Unreliable && !(packetType == PacketType.Acknowledgment || packetType == PacketType.Data))
                    throw new ArgumentException(string.Format(EXCP_INVALID_UNRELIABLE, packetType), nameof(packetType));

                ByteWriter header = new ByteWriter();
                header.Write((byte)((reliability == Reliability.Reliable ? RELIABLE_MASK : UNRELIABLE_MASK) | (byte)packetType));
                header.Write(sequenceNumber);

                return payload == null ? header.Data : header.MergeData(payload);
            }

            /// <summary>
            /// Parse the header information of a packet.
            /// </summary>
            /// <param name="reader">The <see cref="ByteReader"/> to parse from.</param>
            /// <param name="packetType">The type of the packet.</param>
            /// <param name="sequenceNumber">The sequence number of the packet.</param>
            /// <returns>The reliability of the packet.</returns>
            public static Reliability ParseHeader(ByteReader reader, out PacketType packetType, out int sequenceNumber)
            {
                byte headerByte = reader.ReadByte();
                sequenceNumber = reader.ReadInt();
                packetType = (PacketType)(headerByte & PACKET_TYPE_MASK);
                return ((headerByte & RELIABLE_MASK) == RELIABLE_MASK) ? Reliability.Reliable : Reliability.Unreliable;
            }

        }
    }
}
