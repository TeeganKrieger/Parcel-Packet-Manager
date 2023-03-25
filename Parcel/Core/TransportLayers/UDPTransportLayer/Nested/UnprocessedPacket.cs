using Parcel.Serialization;
using System;

namespace Parcel.Networking
{
    public sealed partial class UDPTransportLayer
    {
        /// <summary>
        /// Represents a packet in its raw state and additional information about the packet.
        /// </summary>
        private sealed class UnprocessedPacket
        {
            /// <summary>
            /// The <see cref="ByteReader"/> containing the packet.
            /// </summary>
            public ByteReader ByteReader { get; private set; }

            /// <summary>
            /// The <see cref="Peer"/> that sent the packet.
            /// </summary>
            public Peer Sender { get; private set; }

            /// <summary>
            /// Construct a new instance of RawPacket.
            /// </summary>
            /// <param name="byteReader">A <see cref="Parcel.Serialization.ByteReader"/> with the packet payload.</param>
            /// <param name="sender">The <see cref="Peer"/> that sent the packet.</param>
            public UnprocessedPacket(ByteReader byteReader, Peer sender)
            {
                ByteReader = byteReader;
                Sender = sender;
            }
        }
    }
}
