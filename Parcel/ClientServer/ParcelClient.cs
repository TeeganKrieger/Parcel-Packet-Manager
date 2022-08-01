using Parcel.DataStructures;
using Parcel.Packets;
using Parcel.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Parcel
{

    /// <summary>
    /// Provides client-side network services.
    /// </summary>
    /// <remarks>
    /// The ParcelClient handles serialization, deserialization, sending, and receiving of <see cref="Packet">Packets</see>
    /// and <see cref="SyncedObject">SyncedObjects</see>.<br/>
    /// The ParcelClient also provides utilities working with SyncedObjects such as <see cref="ParcelClient.CreateSyncedObject">Creation</see>,
    /// <see cref="ParcelClient.DestroySyncedObject">Destruction</see>, and <see cref="ParcelClient.TransferSyncedObjectOwnership">Ownership Transfer</see>
    /// requests.
    /// </remarks>
    public sealed class ParcelClient : IDisposable
    {
        private const string EXCP_SEND_DIR_SO = "Cannot send SyncedObject packet directly to peer. Use the SendPacket(Packet packet) overload instead.";
        private const string EXCP_SETTINGS = "Failed to create ParcelClient. ParcelSettings instance is already bound to another ParcelClient or ParcelServer.";

        private INetworkAdapter _networkAdapter;

        private ConcurrentDictionary<SyncedObjectID, SyncedObject> _syncedObjectDict;
        private ConcurrentHashSet<SyncedObjectID> _syncedObjectsToUpdate;

        private ConcurrentQueue<Packet> _scheduledPackets;

        private SerializerResolver _serializerResolver;
        private SyncedObjectSerializer _syncedObjectSerializer;

        private CancellationTokenSource _loopTaskCancellationSource;

        private int _loopCounter = 0;

        /// <summary>
        /// Invoked when disconnected from the server.
        /// </summary>
        public event ConnectionEvent DisconnectionEvent;

        /// <summary>
        /// The <see cref="ParcelSettings">Network Settings</see> used by this client.
        /// </summary>
        public ParcelSettings NetworkSettings { get; private set; }

        /// <summary>
        /// The <see cref="Peer"/> that represents this client.
        /// </summary>
        public Peer Self { get; private set; }

        /// <summary>
        /// The <see cref="Peer"/> that represents the server.
        /// </summary>
        public Peer Remote { get; private set; }


        #region CONSTRUCTOR

        /// <summary>
        /// Construct a new instance of ParcelClient using <paramref name="settings"/>.
        /// </summary>
        /// <param name="settings">The <see cref="ParcelSettings">Network Settings</see> to use.</param>
        public ParcelClient(ParcelSettings settings)
        {
            //Try to lock settings
            if (settings.Locked)
                throw new ArgumentException(EXCP_SETTINGS, nameof(settings));

            settings.Locked = true;

            //initialize fields
            this._syncedObjectDict = new ConcurrentDictionary<SyncedObjectID, SyncedObject>();
            this._syncedObjectsToUpdate = new ConcurrentHashSet<SyncedObjectID>();
            this._scheduledPackets = new ConcurrentQueue<Packet>();
            this._serializerResolver = new SerializerResolver();
            this._syncedObjectSerializer = new SyncedObjectSerializer(this);
            this._loopTaskCancellationSource = new CancellationTokenSource();
            this._networkAdapter = settings.CreateNewNetworkAdapter();

            //initialize properties
            this.NetworkSettings = settings;
            this.Self = settings.Peer;

            //perform setup
            this._networkAdapter.OnRecievedDisconnection += (Peer disconnected) => this.DisconnectionEvent(disconnected);
            this._serializerResolver.RegisterSerializer(new SyncedObjectReferenceSerializer(this));
            this._networkAdapter.Start(false, settings);
            if (settings.PerformUpdatesAutomatically)
                Task.Run(AutoLoop, this._loopTaskCancellationSource.Token);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this._loopTaskCancellationSource.Cancel();
            this.NetworkSettings.Debugger?.Dispose();
            //TODO: Perform a proper disconnect if not already disconnected
            if (this._networkAdapter is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        #endregion


        #region CONNECTION

        /// <summary>
        /// Asynchronously connect to a remote user.
        /// </summary>
        /// <param name="connectionToken">The <see cref="ConnectionToken"/> of the remote user.</param>
        /// <returns><see langword="true"/> if the connection is successful; otherwise, <see langword="false"/></returns>
        public async Task<bool> ConnectTo(ConnectionToken connectionToken)
        {
            Peer remote = await this._networkAdapter.ConnectTo(connectionToken);

            if (remote != null)
            {
                this.Remote = remote;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Disconnect from the remote user.
        /// NOTE: Unimplemented
        /// </summary>
        public void Disconnect()
        {

        }

        #endregion


        #region LOOP

        /// <summary>
        /// Perform a single loop iteration that that serializes, sends, deserializes, and receives <see cref="Packet">Packets</see>. 
        /// </summary>
        /// <returns>Returns the number of milliseconds the loop took to complete.</returns>
        public int Loop()
        {
            long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            this.NetworkSettings.Debugger?.StartFrame($"Packet Loop {this._loopCounter++}");
            try
            {
                SendScheduledPackets();
                RecieveIncomingPackets();
            }
            catch (Exception ex)
            {
                this.NetworkSettings.Debugger?.AddExceptionEvent(ex);
            }
            this.NetworkSettings.Debugger?.EndFrame();
            long end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            return (int)(end - start);
        }

        /// <summary>
        /// Runs a loop until canceled that serializes, sends, deserializes, and receives <see cref="Packet">Packets</see>.
        /// </summary>
        private async Task AutoLoop()
        {
            while (true)
            {
                int timeTook = Loop();
                //Delay start of next iteration until an appropriate timeframe has ellapsed
                int delay = Math.Max(0, this.NetworkSettings.MillisecondsPerUpdate - timeTook);
                await Task.Delay(delay);
            }

        }

        /// <summary>
        /// Performs logic for serializing and sending <see cref="Packet">Packets</see>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SendScheduledPackets()
        {
            long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            //Create byte writers for reliable and unreliable packets
            WriterAndCount unreliableWaC = new WriterAndCount(new ByteWriter(this._serializerResolver));
            WriterAndCount reliableWaC = new WriterAndCount(new ByteWriter(this._serializerResolver));

            //Loop: Serialize and send packets until either no packets are left to send or 40% of the allotted milliseconds for this tick have
            //been consumed.
            Packet outgoingPacket;
            while (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start < (0.45f * this.NetworkSettings.MillisecondsPerUpdate)
                && this._scheduledPackets.TryDequeue(out outgoingPacket))
            {
                //Ensure packet state
                outgoingPacket.IsServer = false;
                outgoingPacket.Client = this;
                outgoingPacket.Server = null;

                try
                {
                    //Skip packets that can't be sent
                    if (!outgoingPacket.CanSend())
                        continue;

                    //Perform before send logic
                    outgoingPacket.OnSend();
                }
                catch (Exception ex)
                {
                    this.NetworkSettings.Debugger?.AddExceptionEvent(ex);
                    continue;
                }

                //Handle Synced Objects
                if (outgoingPacket is SyncedObject so)
                {
                    _syncedObjectsToUpdate.TryRemove(so.ID);

                    SerializeSyncedObject(so, Reliability.Reliable, reliableWaC);
                    SerializeSyncedObject(so, Reliability.Unreliable, unreliableWaC);
                }
                //Handle Packets
                else
                {
                    ObjectCache cache = ObjectCache.FromType(outgoingPacket.GetType());

                    SerializePacket(outgoingPacket, cache.GetReliability() == Reliability.Reliable ? reliableWaC : unreliableWaC);
                }

                //When either the reliable or unreliable grouping threshold is reached, send the appropriate packet.
                TrySendPackets(reliableWaC, Reliability.Reliable, this.NetworkSettings.ReliablePacketGroupSize);
                TrySendPackets(unreliableWaC, Reliability.Unreliable, this.NetworkSettings.UnreliablePacketGroupSize);
            }

            //Perform final sends for any remaining packets.
            TrySendPackets(reliableWaC, Reliability.Reliable, 0);
            TrySendPackets(unreliableWaC, Reliability.Unreliable, 0);

            void SerializePacket(Packet packet, WriterAndCount wac)
            {
                int restorePosition = wac.Writer.Position;

                try
                {
                    wac.Writer.Write((byte)1); //Hint

                    int skipPosition = wac.Writer.Position;
                    wac.Writer.Write(0); //Skip Distance

                    lock (packet)
                        wac.Writer.Write(packet);

                    wac.Writer.Write(wac.Writer.Position - skipPosition, skipPosition);
                    wac.Count++;
                    this.NetworkSettings.Debugger?.AddSerializedPacketEvent();
                }
                catch (Exception ex)
                {
                    wac.Writer.SetPosition(restorePosition);
                    this.NetworkSettings.Debugger?.AddExceptionEvent(ex);
                }
            }

            void SerializeSyncedObject(SyncedObject so, Reliability reliability, WriterAndCount wac)
            {
                if (this._syncedObjectSerializer.WillSerialize(so, reliability))
                {
                    int restorePosition = wac.Writer.Position;
                    try
                    {
                        wac.Writer.Write((byte)2); //Hint

                        int skipPosition = wac.Writer.Position;
                        wac.Writer.Write(0); //Skip Distance

                        lock (so)
                            this._syncedObjectSerializer.Serialize(wac.Writer, so, reliability);

                        wac.Writer.Write(wac.Writer.Position - skipPosition, skipPosition);
                        wac.Count++;
                        this.NetworkSettings.Debugger?.AddSerializedPacketEvent();
                    }
                    catch (Exception ex)
                    {
                        wac.Writer.SetPosition(restorePosition);
                        this.NetworkSettings.Debugger?.AddExceptionEvent(ex);
                    }
                }
            }

            void TrySendPackets(WriterAndCount wac, Reliability reliability, int countThreshold)
            {
                if (wac.Count >= countThreshold && wac.Writer.Length > 0)
                {
                    wac.Writer.Write((byte)0);
                    this._networkAdapter.SendPacketTo(Remote, reliability, wac.Writer);
                    this.NetworkSettings.Debugger?.AddSendPacketEvent(wac.Writer.Length);
                    wac.Reset();
                }
            }

        }

        /// <summary>
        /// Performs logic for deserializing and receiving <see cref="Packet">Packets</see>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RecieveIncomingPackets()
        {
            long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            while (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start < (0.45f * this.NetworkSettings.MillisecondsPerUpdate)
                && this._networkAdapter.GetNextPacket(out ByteReader reader, out Peer sender))
            {
                this.NetworkSettings.Debugger?.AddReceivePacketEvent(reader.Length);

                try
                {
                    for (int hint = reader.ReadByte(); hint != 0; hint = reader.ReadByte())
                    {
                        int startPosition = reader.Position;
                        int skipDistance = reader.ReadInt();
                        Packet packet = null;

                        try
                        {
                            Dictionary<string, SyncedObject.PropertyChanges> changes = null;
                            packet = hint == 2 ? this._syncedObjectSerializer.Deserialize(reader, sender, out changes)
                                : reader.ReadObject<Packet>();

                            if (packet == null)
                                continue;

                            packet.Sender = sender;
                            packet.IsServer = false;
                            packet.Server = null;
                            packet.Client = this;
                            if (packet is SyncedObject so && changes.Count > 0)
                                so.OnPropertiesChanged(changes);
                            this.NetworkSettings.Debugger?.AddDeserializedPacketEvent();
                        }
                        //Catch any exceptions that may occur during packet deserialization
                        catch (Exception ex)
                        {
                            reader.SetPosition(startPosition + skipDistance);
                            this.NetworkSettings.Debugger?.AddExceptionEvent(ex);
                        }

                        try
                        {
                            packet?.OnReceive();
                        }
                        //Catch any exceptions that may occur during the packets after receive event
                        catch (Exception ex)
                        {
                            this.NetworkSettings.Debugger.AddExceptionEvent(ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.NetworkSettings.Debugger?.AddExceptionEvent(ex);
                    continue;
                }
            }
        }

        #endregion


        #region SYNCED OBJECTS


        /// <summary>
        /// Requests the creation of a <see cref="SyncedObject"/> from the remote server.<br/>
        /// NOTE: Not implemented yet.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of <see cref="SyncedObject"/> to create.</param>
        /// <returns>The newly created <see cref="SyncedObject"/>.</returns>
        /// <remarks>
        /// Due to this method needing to make a call to the server, the current thread will be blocked until a response from the server is received.<br/>
        /// If you don't want the thread to be blocked, use <see cref="CreateSyncedObjectAsync(Type)"/>.
        /// </remarks>
        public SyncedObject CreateSyncedObject(Type type)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Asynchronously requests the creation of a <see cref="SyncedObject"/> from the remote server.<br/>
        /// NOTE: Not implemented yet.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of <see cref="SyncedObject"/> to create.</param>
        /// <returns>The newly created <see cref="SyncedObject"/>.</returns>
        public Task<SyncedObject> CreateSyncedObjectAsync(Type type)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Requests the creation of a <see cref="SyncedObject"/> from the remote server.<br/>
        /// NOTE: Not implemented yet.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> of <see cref="SyncedObject"/> to create.</typeparam>
        /// <returns>The newly created <see cref="SyncedObject"/>.</returns>
        /// <remarks>
        /// Due to this method needing to make a call to the server, the current thread will be blocked until a response from the server is received.<br/>
        /// If you don't want the thread to be blocked, use <see cref="CreateSyncedObjectAsync{T}"/>.
        /// </remarks>
        public T CreateSyncedObject<T>() where T : SyncedObject, new()
        {
            //Check if this client has authority to create this packet

            //Send out a SyncedObjectCreationRequest Packet requesting the creation of a new instance of T
            //Include in the packet a request ID (int)
            //Busy Wait for a SyncedObjectCreationResponse packet
            //Parse packet to obtain a request ID and a SyncedObjectID
            //Busy Wait until a SyncedObject with the same SyncedObjectID is in this._syncedObjectDict.
            //Return it
            throw new NotImplementedException();
        }

        /// <summary>
        /// Asynchronously requests the creation of a <see cref="SyncedObject"/> from the remote server.<br/>
        /// NOTE: Not implemented yet.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> of <see cref="SyncedObject"/> to create.</typeparam>
        /// <returns>The newly created <see cref="SyncedObject"/>.</returns>
        public async Task<T> CreateSyncedObjectAsync<T>() where T : SyncedObject, new()
        {
            //Send out a SyncedObjectCreationRequest Packet requesting the creation of a new instance of T
            //Include in the packet a request ID (int)
            //Async Wait for a SyncedObjectCreationResponse packet
            //Parse packet to obtain a request ID and a SyncedObjectID
            //Async Wait until a SyncedObject with the same SyncedObjectID is in this._syncedObjectDict.
            //Return it
            throw new NotImplementedException();
        }

        /// <summary>
        /// Requests the destruction of a <see cref="SyncedObject"/> by the remote server.
        /// </summary>
        /// <param name="syncedObjectID">The <see cref="SyncedObjectID">ID</see> of the <see cref="SyncedObject"/> to destroy.</param>
        /// <returns><see langword="true"/> if the destruction was successful; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// Due to this method needing to make a call to the server, the current thread will be blocked until a response from the server is received.<br/>
        /// If you don't want the thread to be blocked, use <see cref="DestroySyncedObjectAsync(SyncedObjectID)"/>.
        /// </remarks>
        public bool DestroySyncedObject(SyncedObjectID syncedObjectID)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Asynchronously requests the destruction of a <see cref="SyncedObject"/> by the remote server.
        /// </summary>
        /// <param name="syncedObjectID">The <see cref="SyncedObjectID">ID</see> of the <see cref="SyncedObject"/> to destroy.</param>
        /// <returns><see langword="true"/> if the destruction was successful; otherwise, <see langword="false"/>.</returns>
        public async Task<bool> DestroySyncedObjectAsync(SyncedObjectID syncedObjectID)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Try to get a local instance of a <see cref="SyncedObject"/> using its <see cref="SyncedObjectID">ID</see>.
        /// </summary>
        /// <param name="syncedObjectID">The <see cref="SyncedObjectID">ID</see> of the <see cref="SyncedObject"/>.</param>
        /// <param name="syncedObject">The <see cref="SyncedObject"/> if one was found; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the <see cref="SyncedObject"/> was found; otherwise, <see langword="false"/>.</returns>
        public bool TryGetSyncedObject(SyncedObjectID syncedObjectID, out SyncedObject syncedObject)
        {
            return _syncedObjectDict.TryGetValue(syncedObjectID, out syncedObject);
        }

        /// <summary>
        /// Try to get a local <see cref="SyncedObject"/> of type <typeparamref name="T"/> using its <see cref="SyncedObjectID">ID</see>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="SyncedObject"/> to try to get.</typeparam>
        /// <param name="syncedObjectID">The <see cref="SyncedObjectID">ID</see> of the <see cref="SyncedObject"/>.</param>
        /// <param name="syncedObject">The <see cref="SyncedObject"/> as type <typeparamref name="T"/> if one was found; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if a <see cref="SyncedObject"/> of type <typeparamref name="T"/> was found; otherwise, <see langword="false"/>.</returns>
        public bool TryGetSyncedObject<T>(SyncedObjectID syncedObjectID, out T syncedObject) where T : SyncedObject, new()
        {
            if (_syncedObjectDict.TryGetValue(syncedObjectID, out SyncedObject so) && so is T t)
            {
                syncedObject = t;
                return true;
            }
            syncedObject = null;
            return false;
        }

        /// <summary>
        /// Requests for the ownership of a <see cref="SyncedObject"/> to be transfered to another <see cref="Peer"/> by the remote server.<br/>
        /// NOTE: Not implemented yet.
        /// </summary>
        /// <param name="syncedObjectID">The <see cref="SyncedObjectID">ID</see> of the <see cref="SyncedObject"/>.</param>
        /// <param name="newOwner">The <see cref="Peer"/> who will now own this <see cref="SyncedObject"/>.</param>
        /// <returns><see langword="true"/> if the ownership transfer was successful; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// Due to this method needing to make a call to the server, the current thread will be blocked until a response from the server is received.<br/>
        /// If you don't want the thread to be blocked, use <see cref="TransferSyncedObjectOwnershipAsync(SyncedObjectID, Peer)"/>.
        /// </remarks>
        public bool TransferSyncedObjectOwnership(SyncedObjectID syncedObjectID, Peer newOwner)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Asynchronously requests for the ownership of a <see cref="SyncedObject"/> to be transfered to another <see cref="Peer"/> by the remote server.<br/>
        /// NOTE: Not implemented yet.
        /// </summary>
        /// <param name="syncedObjectID">The <see cref="SyncedObjectID">ID</see> of the <see cref="SyncedObject"/>.</param>
        /// <param name="newOwner">The <see cref="Peer"/> who will now own this <see cref="SyncedObject"/>.</param>
        /// <returns><see langword="true"/> if the ownership transfer was successful; otherwise, <see langword="false"/>.</returns>
        public async Task<bool> TransferSyncedObjectOwnershipAsync(SyncedObjectID syncedObjectID, Peer newOwner)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates the <see cref="Peer">Owner</see> of the local <see cref="SyncedObject"/> with <paramref name="syncedObjectID"/>.
        /// </summary>
        /// <param name="syncedObjectID">The <see cref="SyncedObjectID">ID</see> of the <see cref="SyncedObject"/>.</param>
        /// <param name="newOwner">The <see cref="Peer"/> who will now own this <see cref="SyncedObject"/>.</param>
        internal void UpdateSyncedObjectOwner(SyncedObjectID syncedObjectID, Peer newOwner)
        {
            if (TryGetSyncedObject(syncedObjectID, out SyncedObject so))
            {
                Peer oldOwner = so.Owner;
                so.Owner = newOwner;
                so.OnOwnerChange(oldOwner, newOwner);
            }
        }

        /// <summary>
        /// Adds a new local <see cref="SyncedObject"/> to be tracked.
        /// </summary>
        /// <param name="syncedObject">The <see cref="SyncedObject"/> to add.</param>
        internal void AddSyncedObject(SyncedObject syncedObject)
        {
            if (this._syncedObjectDict.TryAdd(syncedObject.ID, syncedObject))
            {
                //Ensures locally owned SyncedObjects have the local version of the Peer.
                if (this.Self == syncedObject.Owner)
                    syncedObject.Owner = this.Self;
                syncedObject.IsServer = false;
                syncedObject.Client = this;
                syncedObject.Server = null;
                syncedObject.Sender = this.Remote;

                syncedObject.OnCreated();
            }
        }

        /// <summary>
        /// Removes a local <see cref="SyncedObject"/> from being tracked.
        /// </summary>
        /// <param name="syncedObjectID">The <see cref="SyncedObjectID">ID</see> of the <see cref="SyncedObject"/> to remove.</param>
        internal void RemoveSyncedObject(SyncedObjectID syncedObjectID)
        {
            if (this._syncedObjectDict.TryRemove(syncedObjectID, out SyncedObject so))
                so.OnDestroyed();
        }


        #endregion


        #region PACKETS

        /// <summary>
        /// Send a <see cref="Packet"/> to the remote server.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="packet"/> is <see langword="null"/>.</exception>
        /// <param name="packet">The <see cref="Packet"/> to send.</param>
        public void Send(Packet packet)
        {
            if (packet == null)
                throw new ArgumentNullException(nameof(packet));
            if (packet is SyncedObject so)
            {
                if (so.Owner != this.Self)
                    return;
                if (this._syncedObjectsToUpdate.TryAdd(so.ID))
                    _scheduledPackets.Enqueue(packet);
            }
            else
            {
                _scheduledPackets.Enqueue(packet);
            }
        }

        /// <summary>
        /// Send a relay <see cref="Packet"/> to another <see cref="Peer"/> connected to the same server.<br/>
        /// NOTE: Not implemented yet.
        /// </summary>
        /// <param name="packet">The <see cref="Packet"/> to send.</param>
        /// <param name="sendTo">The <see cref="Peer"/> to send the <see cref="Packet"/> to.</param>
        /// <exception cref="ArgumentNullException">Thrown if either <paramref name="packet"/> or <paramref name="sendTo"/> are <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="packet"/> is an instance of a <see cref="SyncedObject"/>.</exception>
        /// <remarks>
        /// Packets sent in this manner will first be sent to the server, at which point they will be relayed to the appropriate client.
        /// </remarks>
        public void Send(Packet packet, Peer sendTo)
        {
            if (sendTo == null)
                throw new ArgumentNullException(nameof(sendTo));
            if (packet is SyncedObject)
                throw new ArgumentException(EXCP_SEND_DIR_SO, nameof(packet));
            //Schedule a ForwardingPacket with the destination of sendTo and embedded packet of packet
        }

        #endregion


        #region RPC


        #endregion


        #region NESTED CLASSES

        /// <summary>
        /// Represents a <see cref="ByteWriter"/> and packet count.
        /// </summary>
        private class WriterAndCount
        {
            /// <summary>
            /// The <see cref="ByteWriter"/>.
            /// </summary>
            public ByteWriter Writer { get; private set; }

            /// <summary>
            /// The packet count.
            /// </summary>
            public int Count { get; set; }

            /// <summary>
            /// Construct a new instance of WriterAndCount.
            /// </summary>
            /// <param name="writer">The <see cref="ByteWriter"/> to use.</param>
            public WriterAndCount(ByteWriter writer)
            {
                this.Writer = writer;
                this.Count = 0;
            }

            /// <summary>
            /// Reset the <see cref="Writer"/> and <see cref="Count"/>.
            /// </summary>
            public void Reset()
            {
                this.Count = 0;
                this.Writer.Reset();
            }
        }

        #endregion

    }
}
