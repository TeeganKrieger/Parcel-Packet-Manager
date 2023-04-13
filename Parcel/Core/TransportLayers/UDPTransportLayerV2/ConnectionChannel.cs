using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Parcel.Networking
{
    public partial class UDPTransportLayerV2
    {
        private class ConnectionChannel
        {
            private UDPTransportLayerV2 _udpTransportLayer;
            private UdpClient _udpClient;
            private ConnectionToken _connectionToken;

            private SequenceNumber _currentReliableOutgoingSQN;
            private SequenceNumber _currentReliableIncomingSQN;

            private SequenceNumber _currentUnreliableOutgoingSQN;
            private SequenceNumber _currentUnreliableIncomingSQN;



            public ConnectionChannel(UDPTransportLayerV2 udpTransportLayer, ConnectionToken connectionToken)
            {
                this._udpTransportLayer = udpTransportLayer;
                this._connectionToken = connectionToken;
                this._udpClient = udpTransportLayer._udpClient;
            }

            private void Loop()
            {

            }

            private void ProcessInboundPacket(byte[] data)
            {

            }

            public void SendReliablePacket(byte[] data, CallbackHandle callbackHandle)
            {
                this._currentReliableOutgoingSQN.Increment();



            }

            public void SendUnreliablePacket(byte[] data)
            {

            }


            #region NESTED CLASSES

            //Types of packets:
            /* Connection: Includes Connection Data Serialized
             * 
             */

            private static class HeaderHelper
            {
                public static byte[] CreateReliableDataPacket(int sequenceNumber, byte[] data)
                {
                    byte[] newData = new byte[data.Length + sizeof(int) + sizeof(byte)];
                    newData[0] = 0b1000_0001;
                    
                }
            }

            pr


            /// <summary>
            /// Represents a thread safe incrementing integer used for tracking sequence numbers.
            /// </summary>
            private sealed class SequenceNumber
            {
                private int _sequenceNumber;

                /// <summary>
                /// Construct a new instance of SequenceNumber.
                /// </summary>
                /// <param name="startingValue">The starting value.</param>
                public SequenceNumber(int startingValue) { this._sequenceNumber = startingValue; }

                /// <summary>
                /// Set the value.
                /// </summary>
                /// <param name="sequenceNumber">The new value to set.</param>
                public void Set(int sequenceNumber) { Interlocked.Exchange(ref _sequenceNumber, sequenceNumber); }

                /// <summary>
                /// Increment the value.
                /// </summary>
                /// <returns>The incremented value.</returns>
                public int Increment() { return Interlocked.Increment(ref _sequenceNumber); }

                /// <summary>
                /// Decrement the value.
                /// </summary>
                /// <returns>The decremented value.</returns>
                public int Decrement() { return Interlocked.Decrement(ref _sequenceNumber); }

                public static implicit operator int(SequenceNumber sequenceNumber) { return Interlocked.CompareExchange(ref sequenceNumber._sequenceNumber, 0, 0); }
            }

            #endregion

        }
    }
}
