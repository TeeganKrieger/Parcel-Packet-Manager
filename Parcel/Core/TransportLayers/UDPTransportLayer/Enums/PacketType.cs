using System;
using System.Collections.Generic;
using System.Text;

namespace Parcel.Networking
{
    public sealed partial class UDPTransportLayer
    {
        /// <summary>
        /// Represents the kind of Packet the header is for.
        /// </summary>
        private enum PacketType : byte
        {
            /// <summary>
            /// Indicates that the Packet is a normal data Packet.
            /// </summary>
            Data = 0b0100_0000,
            /// <summary>
            /// Indicates that the Packet is an acknowledgment Packet.
            /// </summary>
            Acknowledgment = 0b0100_0001,
            /// <summary>
            /// Indicates that the Packet is a connection request Packet.
            /// </summary>
            ConnectionRequest = 0b0010_0000,
            /// <summary>
            /// Indicates that the Packet is a connection response Packet.
            /// </summary>
            ConnectionResponse = 0b0010_0001,
            /// <summary>
            /// Indicates that the Packet is a disconnection request Packet.
            /// </summary>
            DisconnectionRequest = 0b0001_0000,
            /// <summary>
            /// Indicates that the Packet is a disconnection response Packet.
            /// </summary>
            DisconnectionResponse = 0b0001_0001,
            /// <summary>
            /// Indicates that the Packet is a ping request Packet.
            /// </summary>
            PingRequest = 0b0000_1000,
            /// <summary>
            /// Indicates that the Packet is a ping response Packet.
            /// </summary>
            PingResponse = 0b0000_1001,
        }
    }
}
