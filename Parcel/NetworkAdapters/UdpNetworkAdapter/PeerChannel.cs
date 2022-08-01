using Parcel.DataStructures;
using Parcel.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Parcel
{
    public sealed partial class UdpNetworkAdapter
    {
        /// <summary>
        /// Represents the state of a connection with a remote user.
        /// </summary>
        private enum ConnectionState
        {
            /// <summary>
            /// Indicates that a remote user is disconnected.
            /// </summary>
            Disconnected,
            /// <summary>
            /// Indicates that a remote user is in the process of connecting.
            /// </summary>
            Connecting,
            /// <summary>
            /// Indicates that a remote user is connected.
            /// </summary>
            Connected
        }

        /// <summary>
        /// Handles logic for connecting to, disconnecting from, sending packets to, and receiving packets from a single remote user.
        /// </summary>
        private sealed class PeerChannel
        {
            private uint _outgoingReliableSequenceNumber = 0;
            private uint _incomingReliableSequenceNumber = 0;

            private uint _outgoingUnreliableSequenceNumber = 0;
            private uint _incomingUnreliableSequenceNumber = 0;

            private ConcurrentDictionary<uint, ReliablePacket> _outgoingReliablePackets;

            private ConcurrentDictionary<uint, RawPacket> _incomingReliablePackets;

            private UdpNetworkAdapter _adapter;
            private RollingAverage _resendDelay;
            private long _lastReceptionTime;

            /// <summary>
            /// The ConnectionState of this PeerChannel.
            /// </summary>
            public ConnectionState ConnectionState { get; private set; }

            /// <summary>
            /// The <see cref="Peer"/> this PeerChannel is for.
            /// </summary>
            public Peer Remote { get; private set; }


            #region CONSTRUCTOR

            /// <summary>
            /// Construct a new instance of PeerChannel.
            /// </summary>
            /// <param name="remote">The <see cref="Peer"/> instance this PeerChannel is for.</param>
            /// <param name="adapter">The <see cref="UdpNetworkAdapter"/> this PeerChannel is owned by.</param>
            public PeerChannel(Peer remote, UdpNetworkAdapter adapter)
            {
                this.Remote = remote;
                this._adapter = adapter;
                this._resendDelay = new RollingAverage(100);
                this._resendDelay.Add(50); //Default resend time of 50ms
                this._outgoingReliablePackets = new ConcurrentDictionary<uint, ReliablePacket>();
                this._incomingReliablePackets = new ConcurrentDictionary<uint, RawPacket>();
                this.ConnectionState = ConnectionState.Connecting;
            }

            #endregion


            #region CONNECT AND DISCONNECT

            /// <summary>
            /// Sends a connection packet to the <see cref="Remote">Remote Peer</see>.
            /// </summary>
            public void ConnectToRemote()
            {
                Dictionary<string, object> selfProperties = new Dictionary<string, object>();
                foreach (KeyValuePair<string, object> kv in this._adapter._self)
                    selfProperties.Add(kv.Key, kv.Value);

                ByteWriter writer = new ByteWriter();

                if (this._adapter._isServer)
                {
                    writer.Write(Remote.GUID);
                    writer.Write(this._adapter._self.GUID);
                }

                writer.Write(selfProperties);

                ByteWriter header = HeaderHelper.CreateConnectionHeader(this._outgoingReliableSequenceNumber);
                byte[] packet = header.MergeData(writer);

                this._outgoingReliablePackets.TryAdd(this._outgoingReliableSequenceNumber, new ReliablePacket(packet));
                this._outgoingReliableSequenceNumber++;

                this._adapter._client.Send(packet, packet.Length, this.Remote.Address, this.Remote.Port);
            }

            /// <summary>
            /// Performs logic when the <see cref="Remote">Remote Peer</see> attempts to connect to the local adapter.
            /// </summary>
            /// <param name="reader">The <see cref="ByteReader">ByteReader</see> containing the connection packet.</param>
            private void RecieveRemoteConnection(ByteReader reader)
            {
                string remoteGUID;

                if (!this._adapter._isServer)
                {
                    string selfGUID = reader.ReadString();
                    remoteGUID = reader.ReadString();
                    this._adapter._self.UpdateGUID(selfGUID);
                }
                else
                {
                    remoteGUID = Guid.NewGuid().ToString();
                }

                Dictionary<string, object> remoteProperties = reader.ReadObject<Dictionary<string, object>>();

                this.Remote = new PeerBuilder().SetGUID(remoteGUID)
                        .SetAddress(this.Remote.Address)
                        .SetPort(this.Remote.Port)
                        .SetProperties(remoteProperties);

                if (this._adapter._isServer)
                    ConnectToRemote();

                ConnectionState = ConnectionState.Connected;
                this._adapter.OnRecievedConnection?.Invoke(Remote);
            }

            /// <summary>
            /// Sends a disconnection packet to the <see cref="Remote">Remote Peer</see>.
            /// </summary>
            public void DisconnectFromRemote()
            {

            }

            #endregion


            #region INCOMING PACKETS

            /// <summary>
            /// Performs logic when a packet is received from the <see cref="Remote">Remote Peer</see>.
            /// </summary>
            /// <param name="packet">The packet that was received.</param>
            public void RecievePacket(byte[] packet)
            {
                _lastReceptionTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                ByteReader reader = new ByteReader(packet);
                Reliability reliability = HeaderHelper.ParseHeader(reader, out bool isAck, out bool isConnection, out bool isDisconnection, out uint sequence);

                switch (reliability)
                {
                    case Reliability.Unreliable:
                        {
                            if (sequence >= _incomingUnreliableSequenceNumber)
                            {
                                this._adapter._rawPackets.Enqueue(new RawPacket(DateTimeOffset.UtcNow, reader, Remote));
                                this._incomingUnreliableSequenceNumber++;
                            }
                        }
                        break;
                    case Reliability.Reliable:
                        {
                            if (isConnection)
                            {
                                if (ConnectionState == ConnectionState.Connecting && sequence == 0)
                                {
                                    RecieveRemoteConnection(reader);
                                    SendAcknowledgementPacket(sequence);
                                    this._incomingReliableSequenceNumber++;
                                }
                                else
                                    SendAcknowledgementPacket(sequence);
                            }
                            else if (isAck)
                            {
                                if (this._outgoingReliablePackets.TryRemove(sequence, out ReliablePacket tp))
                                {
                                    this._resendDelay.Add((int)(DateTimeOffset.Now.ToUnixTimeMilliseconds() - tp.LastTimeSent));
                                }
                            }
                            else
                            {
                                if (sequence >= this._incomingReliableSequenceNumber)
                                {
                                    this._incomingReliablePackets.TryAdd(sequence, new RawPacket(DateTimeOffset.UtcNow, reader, Remote));
                                    SendAcknowledgementPacket(sequence);

                                    while (this._incomingReliablePackets.TryRemove(this._incomingReliableSequenceNumber, out RawPacket raw))
                                    {
                                        this._adapter._rawPackets.Enqueue(raw);
                                        this._incomingReliableSequenceNumber++;
                                    }
                                }
                                else
                                    SendAcknowledgementPacket(sequence); //Already recieved this packet at some point
                            }
                        }
                        break;
                }
            }

            #endregion


            #region OUTGOING PACKETS

            /// <summary>
            /// Performs logic to send a packet to the <see cref="Remote">Remote Peer</see>.
            /// </summary>
            /// <param name="writer">A <see cref="ByteWriter"/> containing the packet payload.</param>
            /// <param name="reliability">The <see cref="Reliability"/> of this packet.</param>
            public void SendPacket(ByteWriter writer, Reliability reliability)
            {
                switch (reliability)
                {
                    case Reliability.Unreliable:
                        {
                            ByteWriter header = HeaderHelper.CreateUnreliableHeader(this._outgoingUnreliableSequenceNumber);
                            this._outgoingUnreliableSequenceNumber++;

                            byte[] packet = header.MergeData(writer);
                            this._adapter._client.Send(packet, packet.Length, this.Remote.Address, this.Remote.Port);
                        }
                        break;
                    case Reliability.Reliable:
                        {
                            ByteWriter header = HeaderHelper.CreateReliableHeader(this._outgoingReliableSequenceNumber);

                            byte[] packet = header.MergeData(writer);

                            this._outgoingReliablePackets.TryAdd(this._outgoingReliableSequenceNumber, new ReliablePacket(packet));
                            this._outgoingReliableSequenceNumber++;

                            this._adapter._client.Send(packet, packet.Length, this.Remote.Address, this.Remote.Port);
                        }
                        break;
                }
            }

            #endregion


            #region ACKNOWLEDGEMENT

            /// <summary>
            /// Performs logic to send a acknowledgement packet to the <see cref="Remote">Remote Peer</see>.
            /// </summary>
            /// <param name="ackNumber">The acknowledgement number.</param>
            private void SendAcknowledgementPacket(uint ackNumber)
            {
                ByteWriter header = HeaderHelper.CreateAcknowledgementHeader(ackNumber);
                byte[] packet = header.Data;
                this._adapter._client.Send(packet, packet.Length, this.Remote.Address, this.Remote.Port);
            }

            #endregion


            #region RESEND

            /// <summary>
            /// Trys to resend any unacknowledged packets.
            /// </summary>
            public void TryResend()
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                foreach (uint sequenceNumber in this._outgoingReliablePackets.Keys)
                {
                    if (this._outgoingReliablePackets.TryGetValue(sequenceNumber, out ReliablePacket packet) && now - packet.LastTimeSent >= this._resendDelay.Average)
                    {
                        //Manual Send
                        this._adapter._debugger?.AddPacketResentEvent(sequenceNumber);
                        this._adapter._client.Send(packet.Packet, packet.Packet.Length, Remote.Address, Remote.Port);
                        packet.LastTimeSent = now;
                    }
                }
            }

            #endregion


            #region NEST CLASSES

            /// <summary>
            /// Represents a reliable packet's raw data and the last time the packet was sent.
            /// </summary>
            private sealed class ReliablePacket
            {
                public long LastTimeSent { get; set; }
                public byte[] Packet { get; private set; }

                /// <summary>
                /// Construct a new instance of ReliablePacket
                /// </summary>
                /// <param name="packet">The packet's raw data.</param>
                public ReliablePacket(byte[] packet)
                {
                    this.Packet = packet;
                    this.LastTimeSent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                }

            }

            #endregion
        }
    }
}
