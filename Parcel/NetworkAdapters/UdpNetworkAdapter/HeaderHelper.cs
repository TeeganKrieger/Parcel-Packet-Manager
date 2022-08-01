using Parcel.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parcel
{
    public sealed partial class UdpNetworkAdapter
    {
        /// <summary>
        /// Helps with constucting and intepreting packet headers.
        /// </summary>
        private static class HeaderHelper
        {
            private const byte UNRELIABLE_BIT = 0b0000_0000;
            private const byte RELIABLE_BIT = 0b1000_0000;

            private const byte CONNECTION_BIT = 0b0010_0000;
            private const byte DISCONNECTION_BIT = 0b0001_0000;

            private const byte NOACK_BIT = 0b0000_0000;
            private const byte ACK_BIT = 0b0100_0000;

            /// <summary>
            /// Creates a <see cref="ByteWriter"/> containing an unreliable packet header with <paramref name="sequenceNumber"/>.
            /// </summary>
            /// <param name="sequenceNumber">The sequence number of the unreliable packet.</param>
            /// <returns>A <see cref="ByteWriter"/> instance.</returns>
            public static ByteWriter CreateUnreliableHeader(uint sequenceNumber)
            {
                ByteWriter writer = new ByteWriter();
                writer.Write((byte)(UNRELIABLE_BIT | NOACK_BIT));
                writer.Write(sequenceNumber);
                return writer;
            }

            /// <summary>
            /// Creates a <see cref="ByteWriter"/> containing a reliable packet header with <paramref name="sequenceNumber"/>.
            /// </summary>
            /// <param name="sequenceNumber">The sequence number of the reliable packet.</param>
            /// <returns>A <see cref="ByteWriter"/> instance.</returns>
            public static ByteWriter CreateReliableHeader(uint sequenceNumber)
            {
                ByteWriter writer = new ByteWriter();
                writer.Write((byte)(RELIABLE_BIT | NOACK_BIT));
                writer.Write(sequenceNumber);
                return writer;
            }

            /// <summary>
            /// Creates a <see cref="ByteWriter"/> containing an acknowledgement packet header with <paramref name="ackNumber"/>.
            /// </summary>
            /// <param name="ackNumber">The acknowledgement number of the reliable packet.</param>
            /// <returns>A <see cref="ByteWriter"/> instance.</returns>
            public static ByteWriter CreateAcknowledgementHeader(uint ackNumber)
            {
                ByteWriter writer = new ByteWriter();
                writer.Write((byte)(RELIABLE_BIT | ACK_BIT));
                writer.Write(ackNumber);
                return writer;
            }

            /// <summary>
            /// Creates a <see cref="ByteWriter"/> containing a reliable connection packet header with <paramref name="sequenceNumber"/>.
            /// </summary>
            /// <param name="sequenceNumber">The sequence number of the connection packet.</param>
            /// <returns>A <see cref="ByteWriter"/> instance.</returns>
            public static ByteWriter CreateConnectionHeader(uint sequenceNumber)
            {
                ByteWriter writer = new ByteWriter();
                writer.Write((byte)(RELIABLE_BIT | CONNECTION_BIT));
                writer.Write(sequenceNumber);
                return writer;
            }

            /// <summary>
            /// Creates a <see cref="ByteWriter"/> containing a reliable disconnection packet header with <paramref name="sequenceNumber"/>.
            /// </summary>
            /// <param name="sequenceNumber">The sequence number of the disconnection packet.</param>
            /// <returns>A <see cref="ByteWriter"/> instance.</returns>
            public static ByteWriter CreateDisconnectionHeader(uint sequenceNumber)
            {
                ByteWriter writer = new ByteWriter();
                writer.Write((byte)(RELIABLE_BIT | DISCONNECTION_BIT));
                writer.Write(sequenceNumber);
                return writer;
            }

            /// <summary>
            /// Parses a header from a <see cref="ByteReader"/>.
            /// </summary>
            /// <param name="reader">The <see cref="ByteReader"/> to parse the header from.</param>
            /// <param name="isAck">Indicates the packet is an acknowledgement packet.</param>
            /// <param name="isConnection">Indicates the packet is a connection packet.</param>
            /// <param name="isDisconnection">Indicates the packet is a disconnection packet.</param>
            /// <param name="value">Catch all number for either a sequence number or acknowledgement number, depending on the packet type.</param>
            /// <returns>The <see cref="Reliability"/> of the packet.</returns>
            public static Reliability ParseHeader(ByteReader reader, out bool isAck, out bool isConnection, out bool isDisconnection, out uint value)
            {
                byte flags = reader.ReadByte();
                Reliability result;

                if ((flags & RELIABLE_BIT) == RELIABLE_BIT)
                    result = Reliability.Reliable;
                else
                    result = Reliability.Unreliable;

                if ((flags & ACK_BIT) == ACK_BIT)
                    isAck = true;
                else
                    isAck = false;

                if ((flags & CONNECTION_BIT) == CONNECTION_BIT)
                    isConnection = true;
                else
                    isConnection = false;

                if ((flags & DISCONNECTION_BIT) == DISCONNECTION_BIT)
                    isDisconnection = true;
                else
                    isDisconnection = false;

                value = reader.ReadUInt();

                return result;
            }
        }


    }
}
