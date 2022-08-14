using Parcel.DataStructures;
using Parcel.Debug;
using Parcel.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Parcel.Networking
{
    public sealed partial class UdpNetworkAdapter
    {
        /// <summary>
        /// Represents a connection with a singular <see cref="Peer"/> and handles all logic for sending and receiving packets.
        /// </summary>
        private sealed class PeerChannel
        {
            private const string EXCP_CLIENT_ONLY = "Failed to perform operation. This operation can only be called on a client instance.";

            private SequenceNumber _reliableOutgoing;
            private SequenceNumber _reliableIncoming;

            private SequenceNumber _unreliableOutgoing;
            private SequenceNumber _unreliableIncoming;

            private SequenceNumber _awaitableCounter;

            private ConcurrentDictionary<int, UnacknowledgedPacket> _unacknowledgedPackets;
            private ConcurrentDictionary<int, SequencedPacket> _sequencedPackets;
            private ConcurrentDictionary<int, AwaitablePacket> _awaitablePackets;

            private ConcurrentQueue<UnprocessedPacket> _unprocessedPackets;

            private UdpNetworkAdapter _adapter;
            private ParcelSettings _settings;
            private NetworkDebugger _debugger;

            private RollingAverage _ping;

            /// <summary>
            /// The <see cref="ConnectionState"/> of this channel.
            /// </summary>
            public ConnectionState ConnectionState { get; private set; }

            /// <summary>
            /// The <see cref="Peer"/> this channel is representative of.
            /// </summary>
            public Peer Remote { get; private set; }

            /// <summary>
            /// The ping in milliseconds of the connection with <see cref="Remote"/>.
            /// </summary>
            public int Ping => (int)this._ping.Average;


            #region CONSTRUCTOR

            /// <summary>
            /// Construct a new instance of PeerChannel.
            /// </summary>
            /// <param name="adapter">The <see cref="UdpNetworkAdapter"/> that owns this channel.</param>
            /// <param name="remote">The <see cref="Peer"/> this channel will represent.</param>
            public PeerChannel(UdpNetworkAdapter adapter, Peer remote)
            {
                //Populate fields
                this._reliableIncoming = new SequenceNumber(1);
                this._reliableOutgoing = new SequenceNumber(0);

                this._unreliableIncoming = new SequenceNumber(1);
                this._unreliableOutgoing = new SequenceNumber(0);

                this._awaitableCounter = new SequenceNumber(0);

                this._unacknowledgedPackets = new ConcurrentDictionary<int, UnacknowledgedPacket>();
                this._sequencedPackets = new ConcurrentDictionary<int, SequencedPacket>();
                this._awaitablePackets = new ConcurrentDictionary<int, AwaitablePacket>();

                this._unprocessedPackets = adapter._unprocessedPackets;

                this._adapter = adapter;
                this._settings = adapter._settings;
                this._debugger = adapter._debugger;

                this._ping = new RollingAverage(this._settings.UpdatesPerSecond);


                //Populate properties
                this.Remote = remote;
                this.ConnectionState = ConnectionState.Connecting;
            }

            #endregion


            #region CONNECTION

            /// <summary>
            /// Initiate a connection with the remote <see cref="Peer"/>.
            /// </summary>
            /// <returns>A connection result struct.</returns>
            /// <exception cref="InvalidOperationException">Thrown if this method is called on a server instance.</exception>
            public async Task<ConnectionResult> Connect()
            {
                if (this._adapter._isServer)
                    throw new InvalidOperationException(EXCP_CLIENT_ONLY);

                //Store this channel
                this._adapter._channels.TryAdd(this.Remote.GetConnectionToken(), this);

                //Initialize local variables
                short awaitableIndex = (short)this._awaitableCounter.Increment();
                Peer self = this._settings.Peer;

                //Create local Peer properties dictionary
                Dictionary<string, object> selfProperties = new Dictionary<string, object>();
                foreach (string key in self.GetPropertyKeys())
                {
                    selfProperties.Add(key, self[key]);
                }

                //Try writing packet data
                ByteWriter writer = new ByteWriter(this._settings.SerializerResolver);

                try
                {
                    writer.Write(awaitableIndex);
                    writer.WriteWithoutTypeInfo(selfProperties);
                }
                catch (Exception ex)
                {
                    this.ConnectionState = ConnectionState.Disconnected;
                    this._adapter._channels.TryRemove(this.Remote.GetConnectionToken(), out _);
                    this._debugger?.AddExceptionEvent(ex);
                    return new ConnectionResult(ConnectionStatus.Error, null, null);
                }

                //Send and wait for response
                SendPacket(writer, Reliability.Reliable, PacketType.ConnectionRequest);
                ByteReader responsePacket = await AwaitPacket(awaitableIndex, this._settings.ConnectionTimeout);

                //Handle response
                if (responsePacket == null)
                {
                    this.ConnectionState = ConnectionState.Disconnected;
                    this._adapter._channels.TryRemove(this.Remote.GetConnectionToken(), out _);
                    return new ConnectionResult(ConnectionStatus.Timeout, null, null);
                }
                else
                {
                    bool rejected = false;
                    object rejectionObject = null;
                    string selfGUID = null;
                    string remoteGUID = null;
                    Dictionary<string, object> remoteProperties = null;

                    //Try reading response packet
                    try
                    {
                        rejected = responsePacket.ReadBool();

                        if (rejected)
                        {
                            rejectionObject = responsePacket.ReadObject();
                        }
                        else
                        {
                            selfGUID = responsePacket.ReadString();
                            remoteGUID = responsePacket.ReadString();
                            remoteProperties = responsePacket.ReadWithoutTypeInfo<Dictionary<string, object>>();
                        }

                    }
                    catch (Exception ex)
                    {
                        this.ConnectionState = ConnectionState.Disconnected;
                        this._adapter._channels.TryRemove(this.Remote.GetConnectionToken(), out _);
                        this._debugger?.AddExceptionEvent(ex);
                        return new ConnectionResult(ConnectionStatus.Error, null, null);
                    }

                    //Handle accept or reject
                    if (rejected)
                    {
                        this.ConnectionState = ConnectionState.Disconnected;
                        this._adapter._channels.TryRemove(this.Remote.GetConnectionToken(), out _);
                        return new ConnectionResult(ConnectionStatus.Rejected, null, rejectionObject);
                    }
                    else
                    {
                        //Update remote
                        self.UpdateGUID(selfGUID);
                        this.Remote = new PeerBuilder().SetGUID(remoteGUID).SetProperties(remoteProperties)
                            .SetAddress(this.Remote.Address).SetPort(this.Remote.Port);

                        //Update connection state and Invoke OnConnection event
                        this.ConnectionState = ConnectionState.Connected;
                        this._adapter.OnConnection?.Invoke(this.Remote);

                        return new ConnectionResult(ConnectionStatus.Success, this.Remote, null);
                    }
                }
            }

            /// <summary>
            /// Process a Connection Request packet.
            /// </summary>
            /// <param name="reader">The <see cref="ByteReader"/> containing the packet.</param>
            private void HandleConnectionRequest(ByteReader reader)
            {
                if (!this._adapter._isServer)
                    return;

                //Try read
                Dictionary<string, object> remotePeerProperties;
                short awaitableIndex;

                try
                {
                    //Read awaitableIndex and remote Peer properties
                    awaitableIndex = reader.ReadShort();
                    remotePeerProperties = reader.ReadWithoutTypeInfo<Dictionary<string, object>>();
                }
                catch (Exception ex)
                {
                    this.ConnectionState = ConnectionState.Disconnected;
                    this._debugger?.AddExceptionEvent(ex);
                    return;
                }

                //Update remote Peer properties
                this.Remote = new PeerBuilder().SetProperties(remotePeerProperties).SetAddress(this.Remote.Address)
                    .SetPort(this.Remote.Port);

                //Accept or reject connection
                bool reject = false;
                object rejectionObject = null;

                if (this._adapter.OnInitialConnection != null)
                {
                    foreach (InitialConnectionDelegate cie in this._adapter.OnInitialConnection.GetInvocationList())
                    {
                        InitialConnectionResult result = cie(this.Remote);
                        if (result.AllowConnection == false)
                        {
                            reject = true;
                            rejectionObject = result.RejectionObject;
                        }
                    }
                }

                //Get local Peer
                Peer self = this._settings.Peer;

                //Get local Peer properties
                Dictionary<string, object> selfProperties = new Dictionary<string, object>();
                foreach (string key in self.GetPropertyKeys())
                {
                    selfProperties.Add(key, self[key]);
                }

                ByteWriter writer = new ByteWriter(this._settings.SerializerResolver);

                try
                {
                    writer.Write(awaitableIndex);

                    if (reject)
                    {
                        writer.Write(true);
                        writer.Write(rejectionObject);
                    }
                    else
                    {
                        writer.Write(false);
                        writer.Write(this.Remote.GUID.ToString());
                        writer.Write(self.GUID.ToString());
                        writer.WriteWithoutTypeInfo(selfProperties);
                    }
                }
                catch (Exception ex)
                {
                    this.ConnectionState = ConnectionState.Disconnected;
                    this._debugger?.AddExceptionEvent(ex);
                    return;
                }

                //Update connection state and Invoke OnConnection event if not rejected
                if (!reject)
                {
                    this.ConnectionState = ConnectionState.Connected;
                    this._adapter._channels.TryAdd(this.Remote.GetConnectionToken(), this);
                    this._adapter.OnConnection?.Invoke(this.Remote);
                }
                //Send in form of acknowledgment packet
                SendPacket(writer, Reliability.Reliable, PacketType.ConnectionResponse);
            }
            
            /// <summary>
            /// Process a Connection Response packet.
            /// </summary>
            /// <param name="reader">The <see cref="ByteReader"/> containing the packet.</param>
            private void HandleConnectionResponse(ByteReader reader)
            {
                if (this._adapter._isServer)
                    return;

                short awaitableIndex;
                try
                {
                    awaitableIndex = reader.ReadShort();
                }
                catch (Exception ex)
                {
                    this._debugger?.AddExceptionEvent(ex);
                    return;
                }

                if (this._awaitablePackets.TryGetValue(awaitableIndex, out AwaitablePacket awaitablePacket))
                    awaitablePacket.Receive(reader);
            }

            #endregion


            #region DISCONNECTION

            /// <summary>
            /// Initiate a disconnection from the remote <see cref="Peer"/>.
            /// </summary>
            /// <param name="disconnectionObject">The object to send to the remote along with the disconnection event.</param>
            public async Task Disconnect(object disconnectionObject = null)
            {
                short awaitableIndex = (short)this._awaitableCounter.Increment();
                ByteWriter writer = new ByteWriter(this._settings.SerializerResolver);

                try
                {
                    writer.Write(awaitableIndex);
                    writer.Write(disconnectionObject);
                }
                catch (Exception ex)
                {
                    this._debugger?.AddExceptionEvent(ex);
                    return;
                }

                SendPacket(writer, Reliability.Reliable, PacketType.DisconnectionRequest);
                await AwaitPacket(awaitableIndex, this.Ping * 6);

                this.ConnectionState = ConnectionState.Disconnected;
                this._adapter._channels.TryRemove(this.Remote.GetConnectionToken(), out _);
                this._adapter.OnDisconnection?.Invoke(this.Remote, this._adapter._isServer ? DisconnectionReason.Forced : DisconnectionReason.Manual, null);
            }

            /// <summary>
            /// Process a Disconnection Request packet.
            /// </summary>
            /// <param name="reader">The <see cref="ByteReader"/> containing the packet.</param>
            private void HandleDisconnectionRequest(ByteReader reader)
            {
                //Try read
                short awaitableIndex;
                object disconnectionObject;

                try
                {
                    awaitableIndex = reader.ReadShort();
                    disconnectionObject = reader.ReadObject();
                }
                catch (Exception ex)
                {
                    this._debugger?.AddExceptionEvent(ex);
                    return;
                }

                ByteWriter writer = new ByteWriter(this._settings.SerializerResolver);

                try
                {
                    writer.Write(awaitableIndex);
                }
                catch (Exception ex)
                {
                    this._debugger?.AddExceptionEvent(ex);
                    return;
                }

                int sequenceNumber = SendPacket(writer, Reliability.Reliable, PacketType.DisconnectionResponse);

                Task.Run(FinalizeDisconnection);

                async Task FinalizeDisconnection()
                {
                    await AwaitAcknowledgement(sequenceNumber, this.Ping * 6);
                    this.ConnectionState = ConnectionState.Disconnected;
                    this._adapter._channels.TryRemove(this.Remote.GetConnectionToken(), out _);
                    this._adapter.OnDisconnection?.Invoke(this.Remote, this._adapter._isServer ? DisconnectionReason.Manual : DisconnectionReason.Forced, disconnectionObject);
                }
            }

            /// <summary>
            /// Process a Disconnection Response packet.
            /// </summary>
            /// <param name="reader">The <see cref="ByteReader"/> containing the packet.</param>
            private void HandleDisconnectionResponse(ByteReader reader)
            {
                short awaitableIndex;
                try
                {
                    awaitableIndex = reader.ReadShort();
                }
                catch (Exception ex)
                {
                    this._debugger?.AddExceptionEvent(ex);
                    return;
                }

                if (this._awaitablePackets.TryGetValue(awaitableIndex, out AwaitablePacket awaitablePacket))
                    awaitablePacket.Receive(reader);
            }

            #endregion


            #region PING

            /// <summary>
            /// Initiate a ping with the remote <see cref="Peer"/>.
            /// </summary>
            private async Task SendPing()
            {
                long start = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                short awaitableIndex = (short)this._awaitableCounter.Increment();
                ByteWriter writer = new ByteWriter(this._settings.SerializerResolver);

                try
                {
                    writer.Write(awaitableIndex);
                }
                catch (Exception ex)
                {
                    this._debugger?.AddExceptionEvent(ex);
                    return;
                }

                SendPacket(writer, Reliability.Reliable, PacketType.PingRequest);
                ByteReader reader = await AwaitPacket(awaitableIndex, this._settings.DisconnectionTimeout);

                if (reader == null)
                {
                    //Timeout
                    this.ConnectionState = ConnectionState.Disconnected;
                    this._adapter._channels.TryRemove(this.Remote.GetConnectionToken(), out _);
                    this._adapter.OnDisconnection?.Invoke(this.Remote, DisconnectionReason.Timeout, null);
                }
                else
                {
                    long end = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    this._ping.Add((int)(end - start));
                }
            }

            /// <summary>
            /// Process a Ping Request packet.
            /// </summary>
            /// <param name="reader">The <see cref="ByteReader"/> containing the packet.</param>
            private void HandlePingRequest(ByteReader reader)
            {
                short awaitableIndex;

                try
                {
                    awaitableIndex = reader.ReadShort();
                }
                catch (Exception ex)
                {
                    this._debugger?.AddExceptionEvent(ex);
                    return;
                }

                ByteWriter writer = new ByteWriter(this._settings.SerializerResolver);

                try
                {
                    writer.Write(awaitableIndex);
                }
                catch (Exception ex)
                {
                    this._debugger?.AddExceptionEvent(ex);
                    return;
                }

                SendPacket(writer, Reliability.Reliable, PacketType.PingResponse);
            }

            /// <summary>
            /// Process a Ping Response packet.
            /// </summary>
            /// <param name="reader">The <see cref="ByteReader"/> containing the packet.</param>
            private void HandlePingResponse(ByteReader reader)
            {
                short awaitableIndex;
                try
                {
                    awaitableIndex = reader.ReadShort();
                }
                catch (Exception ex)
                {
                    this._debugger?.AddExceptionEvent(ex);
                    return;
                }

                if (this._awaitablePackets.TryGetValue(awaitableIndex, out AwaitablePacket awaitablePacket))
                    awaitablePacket.Receive(reader);
            }

            #endregion


            #region DATA

            /// <summary>
            /// Send a packet to the remote <see cref="Peer"/>.
            /// </summary>
            /// <param name="writer">The <see cref="ByteWriter"/> containing the packet.</param>
            /// <param name="reliability">The reliability of the packet.</param>
            public void Send(ByteWriter writer, Reliability reliability)
            {
                SendPacket(writer, reliability, PacketType.Data);
            }

            /// <summary>
            /// Process a Data packet.
            /// </summary>
            /// <param name="reader">The <see cref="ByteReader"/> containing the packet.</param>
            private void HandleData(ByteReader reader)
            {
                this._unprocessedPackets.Enqueue(new UnprocessedPacket(reader, this.Remote));
            }

            #endregion


            #region SENDING

            /// <summary>
            /// Performs logic for building and sending a packet to the remote <see cref="Peer"/>.
            /// </summary>
            /// <param name="writer">The <see cref="ByteWriter"/> containing the packet.</param>
            /// <param name="reliability">The reliability of the packet.</param>
            /// <param name="packetType">The type of the packet.</param>
            /// <returns>The sequence number of the packet being sent.</returns>
            private int SendPacket(ByteWriter writer, Reliability reliability, PacketType packetType)
            {
                SequenceNumber sequenceNumber = reliability == Reliability.Reliable ? this._reliableOutgoing : this._unreliableOutgoing;
                sequenceNumber.Increment();

                byte[] packet;
                try
                {
                    packet = HeaderHelper.CreatePacket(reliability, packetType, sequenceNumber, writer);
                }
                catch (Exception ex)
                {
                    sequenceNumber.Decrement();
                    this._debugger?.AddExceptionEvent(ex);
                    return sequenceNumber;
                }

                this._adapter._client.Send(packet, packet.Length, this.Remote.Address, this.Remote.Port);

                return sequenceNumber;
            }

            /// <summary>
            /// Performs logic for building and sending an acknowledgment packet to the remote <see cref="Peer"/>.
            /// </summary>
            /// <param name="sequenceNumber">The sequence number of the packet being acknowledged.</param>
            private void SendAcknowledgmentPacket(int sequenceNumber)
            {
                byte[] packet;
                try
                {
                    packet = HeaderHelper.CreatePacket(Reliability.Unreliable, PacketType.Acknowledgment, sequenceNumber, null);
                }
                catch (Exception ex)
                {
                    this._debugger?.AddExceptionEvent(ex);
                    return;
                }

                this._adapter._client.Send(packet, packet.Length, this.Remote.Address, this.Remote.Port);
            }

            #endregion


            #region RECEIVING

            /// <summary>
            /// Process an incoming packet from the remote <see cref="Peer"/>.
            /// </summary>
            /// <param name="packet">The packet to process.</param>
            public void HandleIncomingPacket(byte[] packet)
            {
                try
                {
                    ByteReader reader = new ByteReader(packet, this._settings.SerializerResolver);

                    Reliability reliability = HeaderHelper.ParseHeader(reader, out PacketType packetType, out int sequenceNumber);

                    if (reliability == Reliability.Reliable)
                    {
                        SendAcknowledgmentPacket(sequenceNumber);
                        if (sequenceNumber >= this._reliableIncoming)
                        {
                            SequencedPacket sequencedPacket = new SequencedPacket(this, packetType, sequenceNumber, reader);
                            this._sequencedPackets.TryAdd(sequenceNumber, sequencedPacket);

                            while (this._sequencedPackets.TryRemove(this._reliableIncoming, out sequencedPacket))
                            {
                                sequencedPacket.Process();
                                this._reliableIncoming.Increment();
                            }
                        }
                    }
                    else
                    {
                        if (packetType == PacketType.Acknowledgment)
                        {
                            this._unacknowledgedPackets.TryRemove(sequenceNumber, out _);
                        }
                        else if (packetType == PacketType.Data && sequenceNumber >= this._unreliableIncoming)
                        {
                            this._unprocessedPackets.Enqueue(new UnprocessedPacket(reader, this.Remote));
                            this._unreliableIncoming.Set(sequenceNumber);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this._debugger?.AddExceptionEvent(ex);
                }
            }

            /// <summary>
            /// Asynchronously wait for a packet with the appropriate awaitable index to arrive on this channel.
            /// </summary>
            /// <param name="awaitableIndex">The awaitable index of the packet.</param>
            /// <param name="timeout">The time in milliseconds to wait before the packet is considered lost.</param>
            /// <returns>The <see cref="ByteReader"/> of the awaited packet.</returns>
            private async Task<ByteReader> AwaitPacket(short awaitableIndex, int timeout)
            {
                CancellationTokenSource cancellationToken = new CancellationTokenSource();
                AwaitablePacket awaitablePacket = new AwaitablePacket();
                bool timedOut = false;

                this._awaitablePackets.TryAdd(awaitableIndex, awaitablePacket);

                await Task.WhenAny(Task.Run(Check, cancellationToken.Token), Task.Run(Timeout, cancellationToken.Token));

                this._awaitablePackets.TryRemove(awaitableIndex, out _);

                if (timedOut)
                    return null;
                else
                    return awaitablePacket.Reader;

                async Task Check()
                {
                    while (!awaitablePacket.Received)
                        await Task.Delay(1);
                    cancellationToken.Cancel();
                    timedOut = false;
                }

                async Task Timeout()
                {
                    await Task.Delay(Math.Max(1, timeout));
                    cancellationToken.Cancel();
                    timedOut = true;
                }
            }

            /// <summary>
            /// Asynchronously wait for a packet with the appropriate sequence number to be acknowledged on this channel.
            /// </summary>
            /// <param name="sequenceNumber">The sequence number of the packet.</param>
            /// <param name="timeout">The time in milliseconds to wait before the packet is considered lost.</param>
            /// <returns><see langword="true"/> if the await timed out; otherwise, <see langword="false"/>.</returns>
            private async Task<bool> AwaitAcknowledgement(int sequenceNumber, int timeout)
            {
                CancellationTokenSource cancellationToken = new CancellationTokenSource();

                bool timedOut = false;

                await Task.WhenAny(Task.Run(Check, cancellationToken.Token), Task.Run(Timeout, cancellationToken.Token));

                return timedOut;

                async Task Check()
                {
                    while (this._unacknowledgedPackets.ContainsKey(sequenceNumber))
                        await Task.Delay(1);
                    cancellationToken.Cancel();
                    timedOut = false;
                }

                async Task Timeout()
                {
                    await Task.Delay(Math.Max(1, timeout));
                    cancellationToken.Cancel();
                    timedOut = true;
                }
            }

            #endregion


            #region LOOP

            /// <summary>
            /// Attempts to resend unacknowledged packets.
            /// </summary>
            public void Loop()
            {
                Task.Run(SendPing);

                long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                foreach (int sequenceNumber in this._unacknowledgedPackets.Keys)
                    if (this._unacknowledgedPackets.TryGetValue(sequenceNumber, out UnacknowledgedPacket packet))
                    {
                        this._debugger?.AddPacketResentEvent(sequenceNumber);
                        packet.TrySend(now);
                    }
            }

            #endregion


            #region NESTED CLASSES

            /// <summary>
            /// Represents an unacknowledged packet.
            /// </summary>
            private sealed class UnacknowledgedPacket
            {
                private PeerChannel _channel;
                private long _lastTimeSent;
                private byte[] _packet;

                /// <summary>
                /// Construct a new instance of UnacknowledgedPacket.
                /// </summary>
                /// <param name="channel">The <see cref="PeerChannel"/> that owns this packet.</param>
                /// <param name="packet">The raw packet.</param>
                public UnacknowledgedPacket(PeerChannel channel, byte[] packet)
                {
                    this._channel = channel;
                    this._lastTimeSent = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    this._packet = packet;
                }

                /// <summary>
                /// Attempt to resend this packet if an appropriate amount of time has passed.
                /// </summary>
                /// <param name="now">The current time.</param>
                public void TrySend(long now)
                {
                    if (now - this._lastTimeSent > this._channel._ping.Average * 1.25d)
                    {
                        Peer remote = this._channel.Remote;

                        this._channel._adapter._client.Send(this._packet, this._packet.Length, remote.Address, remote.Port);
                        this._lastTimeSent = now;
                    }
                }
            }

            /// <summary>
            /// Represents an unprocessed packet in sequenced order.
            /// </summary>
            private sealed class SequencedPacket
            {
                private PeerChannel _channel;
                private PacketType _packetType;
                private int _sequenceNumber;
                private ByteReader _reader;

                /// <summary>
                /// Construct a new instance of SequencedPacket.
                /// </summary>
                /// <param name="channel">The <see cref="PeerChannel"/> that owns this packet.</param>
                /// <param name="packetType">The type of this packet.</param>
                /// <param name="sequenceNumber">The sequence number of this packet.</param>
                /// <param name="reader">A <see cref="ByteReader"/> containing the packets data.</param>
                public SequencedPacket(PeerChannel channel, PacketType packetType, int sequenceNumber, ByteReader reader)
                {
                    this._channel = channel;
                    this._packetType = packetType;
                    this._sequenceNumber = sequenceNumber;
                    this._reader = reader;
                }

                /// <summary>
                /// Attempts to process the packet.
                /// </summary>
                public void Process()
                {
                    switch (this._packetType)
                    {
                        case PacketType.Data:
                            this._channel.HandleData(this._reader);
                            break;
                        case PacketType.ConnectionRequest:
                            this._channel.HandleConnectionRequest(this._reader);
                            break;
                        case PacketType.ConnectionResponse:
                            this._channel.HandleConnectionResponse(this._reader);
                            break;
                        case PacketType.DisconnectionRequest:
                            this._channel.HandleDisconnectionRequest(this._reader);
                            break;
                        case PacketType.DisconnectionResponse:
                            this._channel.HandleDisconnectionResponse(this._reader);
                            break;
                        case PacketType.PingRequest:
                            this._channel.HandlePingRequest(this._reader);
                            break;
                        case PacketType.PingResponse:
                            this._channel.HandlePingResponse(this._reader);
                            break;
                    }
                }
            }

            /// <summary>
            /// Represents a packet that is being awaited.
            /// </summary>
            private sealed class AwaitablePacket
            {
                private object _lock;

                /// <summary>
                /// Whether or not the packet has been received.
                /// </summary>
                public bool Received { get; private set; }

                /// <summary>
                /// The <see cref="ByteReader"/> containing the packet's data.
                /// </summary>
                public ByteReader Reader { get; private set; }

                /// <summary>
                /// Construct a new instance of AwaitablePacket.
                /// </summary>
                public AwaitablePacket()
                {
                    this._lock = new object();
                    this.Received = false;
                    this.Reader = null;
                }

                /// <summary>
                /// Mark this packet as having been received.
                /// </summary>
                /// <param name="reader">The <see cref="ByteReader"/> containing the packet's data.</param>
                public void Receive(ByteReader reader)
                {
                    lock (_lock)
                    {
                        if (!this.Received)
                        {
                            this.Received = true;
                            this.Reader = reader;
                        }
                    }
                }
            }

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
