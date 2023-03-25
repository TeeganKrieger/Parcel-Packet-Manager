using Parcel.DataStructures;
using Parcel.Lib;
using Parcel.Networking;
using Parcel.Packets;
using Parcel.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Parcel
{

    /// <summary>
    /// Provides server-side network services.
    /// </summary>
    /// <remarks>
    /// The ParcelServer handles serialization, deserialization, sending, and receiving of <see cref="Packet">Packets</see>
    /// and <see cref="SyncedObject">SyncedObjects</see>.<br/>
    /// The ParcelServer also provides utilities working with SyncedObjects such as <see cref="ParcelClient.CreateSyncedObject">Creation</see>,
    /// <see cref="ParcelClient.DestroySyncedObject">Destruction</see>, and <see cref="ParcelClient.TransferSyncedObjectOwnership">Ownership Transfer</see>
    /// calls.
    /// </remarks>
    public class ParcelServer
    {
        private const string EXCP_NO_SUBSCRIBERS = "Failed to perform operation because no subscribers were provided. Include at least 1 subscriber.";
        private const string EXCP_ASSIGN_FROM = "failed to create SyncedObject. The type {0} does not inherit SyncedObject.";
        private const string EXCP_SETTINGS = "Failed to create ParcelClient. ParcelSettings instance is already bound to another ParcelClient or ParcelServer.";
        private const string EXCP_PEER_NOT_CONNECTED = "Failed to perform operation because the specified Peer is not connected to this server.";
        private const string MSG_ALREADY_CONNECTED = "You are already connected to this Server.";
        private const string EXCP_DISPOSED = "Failed to perform operation because the server has already been disposed.";

        private ITransportLayer _networkAdapter;

        private ConcurrentDictionary<SyncedObjectID, SyncedObjectSubscriptions> _syncedObjectDict;
        private ConcurrentHashSet<SyncedObjectID> _syncedObjectsToUpdate;

        private ConcurrentQueue<PacketAndRemotes> _scheduledPackets;
        private ConcurrentHashSet<Peer> _connectedPeers;

        private SerializerResolver _serializerResolver;
        private SyncedObjectSerializer _syncedObjectSerializer;

        private CancellationTokenSource _loopTaskCancellationSource;

        private int _loopCounter = 0;

        private ConcurrentDictionary<int, TargetedDynamicDelegate> _rpcCallbacks;
        private int _rpcCounter = 0;

        private bool _disposed;

        /// <summary>
        /// Invoked when a connection to the server is being processed.
        /// </summary>
        /// <remarks>
        /// This event allows for rejecting connections. When this event is invoked, 
        /// all subscribed expressions will be evaluated and if all return true, the connection will be allowed.
        /// </remarks>
        public event InitialConnectionDelegate OnInitialConnection
        {
            add { this._networkAdapter.OnInitialConnection += value; }
            remove { this._networkAdapter.OnInitialConnection -= value; }
        }

        /// <summary>
        /// Invoked when a connection is made to the server.
        /// </summary>
        public event ConnectionDelegate OnRemoteConnected
        {
            add { this._networkAdapter.OnConnection += value; }
            remove { this._networkAdapter.OnConnection -= value; }
        }

        /// <summary>
        /// Invoked when a disconnection is made to the server.
        /// </summary>
        public event DisconnectionDelegate OnRemoteDisconnected
        {
            add { this._networkAdapter.OnDisconnection += value; }
            remove { this._networkAdapter.OnDisconnection -= value; }
        }

        /// <summary>
        /// The <see cref="ParcelSettings">Network Settings</see> used by this client.
        /// </summary>
        public ParcelSettings NetworkSettings { get; private set; }

        /// <summary>
        /// The <see cref="Peer"/> that represents this client.
        /// </summary>
        public Peer Self { get; private set; }

        /// <summary>
        /// An array of <see cref="Peer">Peers</see> who are connected to this server.
        /// </summary>
        public Peer[] RemotePeers => this._connectedPeers.ToArray();


        #region CONSTRUCTOR

        /// <summary>
        /// Construct a new instance of ParcelServer using <paramref name="settings"/>.
        /// </summary>
        /// <param name="settings">The <see cref="ParcelSettings">Network Settings</see> to use.</param>
        public ParcelServer(ParcelSettings settings)
        {
            //Try to lock settings
            if (settings.Locked)
                throw new ArgumentException(EXCP_SETTINGS, nameof(settings));

            settings.Locked = true;

            //initialize fields
            this._syncedObjectDict = new ConcurrentDictionary<SyncedObjectID, SyncedObjectSubscriptions>();
            this._syncedObjectsToUpdate = new ConcurrentHashSet<SyncedObjectID>();
            this._scheduledPackets = new ConcurrentQueue<PacketAndRemotes>();
            this._syncedObjectSerializer = new SyncedObjectSerializer(this);
            this._connectedPeers = new ConcurrentHashSet<Peer>();
            this._loopTaskCancellationSource = new CancellationTokenSource();
            this._serializerResolver = settings.SerializerResolver;
            this._networkAdapter = settings.CreateNewNetworkAdapter();
            this._rpcCallbacks = new ConcurrentDictionary<int, TargetedDynamicDelegate>();

            //initialize properties 
            this.NetworkSettings = settings;
            this.Self = settings.Peer;

            //setup events
            this.OnRemoteConnected += (Peer connected) => { this._connectedPeers.TryAdd(connected); };

            this.OnRemoteDisconnected += DisconnectionCleanup;

            //perform setup
            this._serializerResolver.RegisterSerializer(new SyncedObjectReferenceSerializer(this));
            this._networkAdapter.Start(true, settings);
            if (settings.PerformUpdatesAutomatically)
                Task.Run(AutoLoop, this._loopTaskCancellationSource.Token);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!this._disposed)
            {
                this._loopTaskCancellationSource.Cancel();

                foreach (Peer peer in this._connectedPeers)
                {
                    ForceDisconnect(peer);
                }

                this.NetworkSettings.Debugger?.Dispose();
                this._disposed = true;
            }
        }

        #endregion


        #region LOOP

        /// <summary>
        /// Perform a single loop iteration that that serializes, sends, deserializes, and receives <see cref="Packet">Packets</see>. 
        /// </summary>
        /// <returns>Returns the number of milliseconds the loop took to complete.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the server has been disposed.</exception>
        public int Loop()
        {
            if (this._disposed)
                throw new InvalidOperationException(EXCP_DISPOSED);

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
            while (!this._disposed)
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
            Dictionary<Peer, WriterAndCount> peerReliableWriters = new Dictionary<Peer, WriterAndCount>();
            Dictionary<Peer, WriterAndCount> peerUnreliableWriters = new Dictionary<Peer, WriterAndCount>();

            PacketAndRemotes packetAndRemote;

            //Loop until either a time limit is exceeded or until no more packets are left to be processed.
            while (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start < (0.45f * this.NetworkSettings.MillisecondsPerUpdate)
                && this._scheduledPackets.TryDequeue(out packetAndRemote))
            {
                Packet outgoingPacket = packetAndRemote.Packet;
                Peer[] sendTo = packetAndRemote.Remotes;

                //Ensure packet state
                outgoingPacket.IsServer = true;
                outgoingPacket.Client = null;
                outgoingPacket.Server = this;

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

                //Ensure a writer exists for each peer in SendTo
                foreach (Peer peer in sendTo)
                {
                    if (!this._connectedPeers.Contains(peer))
                        continue;
                    if (!peerUnreliableWriters.ContainsKey(peer))
                        peerUnreliableWriters.Add(peer, new WriterAndCount(new ByteWriter(this._serializerResolver)));
                    if (!peerReliableWriters.ContainsKey(peer))
                        peerReliableWriters.Add(peer, new WriterAndCount(new ByteWriter(this._serializerResolver)));
                }

                //Handle Synced Objects
                if (outgoingPacket is SyncedObject so)
                {
                    _syncedObjectsToUpdate.TryRemove(so.ID);

                    SerializeSyncedObject(so, Reliability.Reliable, peerReliableWriters, sendTo);
                    SerializeSyncedObject(so, Reliability.Unreliable, peerUnreliableWriters, sendTo);
                }
                //Handle Packets
                else
                {
                    ObjectCache cache = ObjectCache.FromType(outgoingPacket.GetType());

                    SerializePacket(outgoingPacket, PacketCacheHelper.GetReliability(cache) == Reliability.Reliable ? peerReliableWriters : peerUnreliableWriters, sendTo);
                }

                TrySendPeersPackets(peerReliableWriters, Reliability.Reliable, this.NetworkSettings.ReliablePacketGroupSize);
                TrySendPeersPackets(peerUnreliableWriters, Reliability.Unreliable, this.NetworkSettings.UnreliablePacketGroupSize);
            }

            //Finalize and send any remaining packets.
            TrySendPeersPackets(peerReliableWriters, Reliability.Reliable, 0);
            TrySendPeersPackets(peerUnreliableWriters, Reliability.Unreliable, 0);

            void SerializePacket(Packet packet, Dictionary<Peer, WriterAndCount> peersDict, Peer[] sendTo)
            {
                ByteWriter localWriter = new ByteWriter(this._serializerResolver);

                try
                {
                    lock (packet)
                        localWriter.Write(packet);
                }
                catch (Exception ex)
                {
                    this.NetworkSettings.Debugger?.AddExceptionEvent(ex);
                    return;
                }

                foreach (Peer peer in sendTo)
                {
                    if (peersDict.TryGetValue(peer, out WriterAndCount wac))
                    {
                        int restorePosition = wac.Writer.Position;

                        try
                        {
                            wac.Writer.Write((byte)1); //Hint

                            int skipPosition = wac.Writer.Position;
                            wac.Writer.Write(0); //Skip Distance

                            wac.Writer.Write(localWriter);

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
            }

            void SerializeSyncedObject(SyncedObject so, Reliability reliability, Dictionary<Peer, WriterAndCount> peersDict, Peer[] sendTo)
            {
                if (this._syncedObjectSerializer.WillSerialize(so, reliability))
                {
                    ByteWriter localWriter = new ByteWriter(this._serializerResolver);

                    try
                    {
                        lock (so)
                            this._syncedObjectSerializer.Serialize(localWriter, so, reliability);
                    }
                    catch (Exception ex)
                    {
                        this.NetworkSettings.Debugger?.AddExceptionEvent(ex);
                        return;
                    }

                    foreach (Peer peer in sendTo)
                    {
                        if (peersDict.TryGetValue(peer, out WriterAndCount wac))
                        {
                            int restorePosition = wac.Writer.Position;

                            try
                            {
                                wac.Writer.Write((byte)2); //Hint

                                int skipPosition = wac.Writer.Position;
                                wac.Writer.Write(0); //Skip Distance

                                wac.Writer.Write(localWriter);

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
                }
            }

            void TrySendPeersPackets(Dictionary<Peer, WriterAndCount> peersDict, Reliability reliability, int countThreshold)
            {
                foreach (Peer peer in peersDict.Keys)
                {
                    WriterAndCount wac = peersDict[peer];

                    if (wac.Count >= countThreshold && wac.Writer.Length > 0)
                    {
                        wac.Writer.Write((byte)0);
                        this._networkAdapter.SendPacketTo(peer, reliability, wac.Writer);
                        this.NetworkSettings.Debugger?.AddSendPacketEvent(wac.Writer.Length);
                        wac.Reset();
                    }
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

            //Loop until either a time limit is exceeded or until no more packets are left to be processed.
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
                            packet.IsServer = true;
                            packet.Server = this;
                            packet.Client = null;

                            if (packet is SyncedObject so && changes.Count > 0 && this._syncedObjectDict.TryGetValue(so.ID, out SyncedObjectSubscriptions sos))
                            {
                                so.OnPropertiesChanged(changes);
                                Send(so, sos.Subscriptions.Where(x => x != so.Owner).ToArray()); //Send to all subscribers other than the owner
                            }
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
                //Catch any exceptions that may occur between packet deserializations
                catch (Exception ex)
                {
                    this.NetworkSettings.Debugger?.AddExceptionEvent(ex);
                }
            }
        }

        #endregion


        #region DISCONNECTION

        /// <summary>
        /// Forcibly disconnect a <see cref="Peer"/> from this server.
        /// </summary>
        /// <param name="peer">The <see cref="Peer"/> to disconnect.</param>
        /// <param name="message">The message to include with the forced disconnection.</param>
        /// <exception cref="InvalidOperationException">Thrown if the server has been disposed or if the <paramref name="peer"/> is not currently connected to the server.</exception>
        public void ForceDisconnect(Peer peer, string message = null)
        {
            if (this._disposed)
                throw new InvalidOperationException(EXCP_DISPOSED);
            if (!this._connectedPeers.Contains(peer))
                throw new InvalidOperationException(EXCP_PEER_NOT_CONNECTED);

            this._networkAdapter.DisconnectFrom(peer, message);
        }

        /// <summary>
        /// Performs cleanup logic when a client disconnects from the server.
        /// </summary>
        /// <param name="client">The <see cref="Peer"/> representing the server.</param>
        /// <param name="reason">The reason for the disconnection.</param>
        /// <param name="disconnectionObject">An object included with the disconnection.</param>
        private void DisconnectionCleanup(Peer client, DisconnectionReason reason, object disconnectionObject)
        {
            if (this._connectedPeers.Contains(client))
            {
                //Cleanup
                if (this.NetworkSettings.ServerDisconnectionBehavior == ServerDisconnectionBehavior.DestroySyncedObjects)
                {
                    List<SyncedObjectID> clientsSyncedObjects = new List<SyncedObjectID>();

                    foreach (SyncedObjectID syncedObjectID in this._syncedObjectDict.Keys)
                    {
                        if (this._syncedObjectDict.TryGetValue(syncedObjectID, out SyncedObjectSubscriptions sos))
                        {
                            if (sos.SyncedObject.Owner == client)
                                clientsSyncedObjects.Add(syncedObjectID);
                            else if (sos.Subscriptions.Contains(client))
                                sos.Subscriptions.TryRemove(client);
                        }
                    }
                    foreach (SyncedObjectID syncedObjectID in clientsSyncedObjects)
                        DestroySyncedObject(syncedObjectID);
                }

                this._connectedPeers.TryRemove(client);
            }
        }

        #endregion


        #region PING

        /// <summary>
        /// Get the ping of a <see cref="Peer"/> connected to the server.
        /// </summary>
        /// <param name="peer">The <see cref="Peer"/> to get the ping of.</param>
        /// <returns>The ping to <see cref="Peer"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the server has been disposed or if the <paramref name="peer"/> is not currently connected to the server.</exception>
        public int GetPingOfPeer(Peer peer)
        {
            if (this._disposed)
                throw new InvalidOperationException(EXCP_DISPOSED);
            if (!this._connectedPeers.Contains(peer))
                throw new InvalidOperationException(EXCP_PEER_NOT_CONNECTED);

            return this._networkAdapter.GetPing(peer);
        }

        #endregion


        #region SYNCED OBJECTS


        /// <summary>
        /// Create a new SyncedObject of <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of <see cref="SyncedObject"/> to create.</param>
        /// <param name="owner">The <see cref="Peer"/> who will own the new <see cref="SyncedObject"/>.</param>
        /// <returns>The instance of the new <see cref="SyncedObject"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if either <paramref name="type"/> or <paramref name="owner"/> are <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="type"/> does not inherit from <see cref="SyncedObject"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the server has been disposed or if the <paramref name="owner"/> is not currently connected to the server.</exception>
        public SyncedObject CreateSyncedObject(Type type, Peer owner)
        {
            if (this._disposed)
                throw new InvalidOperationException(EXCP_DISPOSED);
            if (!this._connectedPeers.Contains(owner) && owner != this.Self)
                throw new InvalidOperationException(EXCP_PEER_NOT_CONNECTED);
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (!typeof(SyncedObject).IsAssignableFrom(type))
                throw new ArgumentException(string.Format(EXCP_ASSIGN_FROM, type.FullName), nameof(type));

            SyncedObject syncedObject = (SyncedObject)Create.New(type);
            syncedObject.ID = SyncedObjectID.Next();
            syncedObject.Owner = owner;
            syncedObject.IsServer = true;
            syncedObject.Server = this;

            SyncedObjectSubscriptions sos = new SyncedObjectSubscriptions(syncedObject);
            sos.Subscriptions.TryAdd(owner);

            while (!this._syncedObjectDict.TryAdd(syncedObject.ID, sos))
                syncedObject.ID = SyncedObjectID.Next();

            syncedObject.OnCreated();

            Send(new CreateSyncedObjectPacket(syncedObject), owner);
            return syncedObject;
        }

        /// <summary>
        /// Create a new SyncedObject of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="SyncedObject"/> to create.</typeparam>
        /// <param name="owner">The <see cref="Peer"/> who will own the new <see cref="SyncedObject"/>.</param>
        /// <returns>The instance of the new <see cref="SyncedObject"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="owner"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the server has been disposed or if the <paramref name="owner"/> is not currently connected to the server.</exception>
        public T CreateSyncedObject<T>(Peer owner) where T : SyncedObject, new()
        {
            if (this._disposed)
                throw new InvalidOperationException(EXCP_DISPOSED);
            if (!this._connectedPeers.Contains(owner) && owner != this.Self)
                throw new InvalidOperationException(EXCP_PEER_NOT_CONNECTED);
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            T syncedObject = new T();
            syncedObject.ID = SyncedObjectID.Next();
            syncedObject.Owner = owner;
            syncedObject.IsServer = true;
            syncedObject.Server = this;

            SyncedObjectSubscriptions sos = new SyncedObjectSubscriptions(syncedObject);
            sos.Subscriptions.TryAdd(owner);

            while (!this._syncedObjectDict.TryAdd(syncedObject.ID, sos))
                syncedObject.ID = SyncedObjectID.Next();

            syncedObject.OnCreated();

            Send(new CreateSyncedObjectPacket(syncedObject), owner);
            return syncedObject;
        }

        /// <summary>
        /// Destroy a <see cref="SyncedObject"/> with <paramref name="syncedObjectID"/>.
        /// </summary>
        /// <param name="syncedObjectID">The <see cref="SyncedObjectID">ID</see> of the <see cref="SyncedObject"/> to destroy.</param>
        /// <returns><see langword="false"/> if the <see cref="SyncedObject"/> has already been destroyed or is non-existant; otherwise, <see langword="true"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the server has been disposed.</exception>
        public bool DestroySyncedObject(SyncedObjectID syncedObjectID)
        {
            if (this._disposed)
                throw new InvalidOperationException(EXCP_DISPOSED);
            if (!this._syncedObjectDict.TryRemove(syncedObjectID, out SyncedObjectSubscriptions sos))
                return false;

            sos.SyncedObject.OnDestroyed();
            Send(new DestroySyncedObjectPacket(syncedObjectID), sos.Subscriptions.ToArray());
            return true;
        }

        /// <summary>
        /// Try to get a local instance of a <see cref="SyncedObject"/> using its <see cref="SyncedObjectID">ID</see>.
        /// </summary>
        /// <param name="syncedObjectID">The <see cref="SyncedObjectID">ID</see> of the <see cref="SyncedObject"/>.</param>
        /// <param name="syncedObject">The <see cref="SyncedObject"/> if one was found; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the <see cref="SyncedObject"/> was found; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the server has been disposed.</exception>
        public bool TryGetSyncedObject(SyncedObjectID syncedObjectID, out SyncedObject syncedObject)
        {
            if (this._disposed)
                throw new InvalidOperationException(EXCP_DISPOSED);
            bool result = this._syncedObjectDict.TryGetValue(syncedObjectID, out SyncedObjectSubscriptions sos);
            syncedObject = sos?.SyncedObject;
            return result;
        }

        /// <summary>
        /// Try to get a local <see cref="SyncedObject"/> of type <typeparamref name="T"/> using its <see cref="SyncedObjectID">ID</see>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="SyncedObject"/> to try to get.</typeparam>
        /// <param name="syncedObjectID">The <see cref="SyncedObjectID">ID</see> of the <see cref="SyncedObject"/>.</param>
        /// <param name="syncedObject">The <see cref="SyncedObject"/> as type <typeparamref name="T"/> if one was found; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if a <see cref="SyncedObject"/> of type <typeparamref name="T"/> was found; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the server has been disposed.</exception>
        public bool TryGetSyncedObject<T>(SyncedObjectID syncedObjectID, out T syncedObject) where T : SyncedObject, new()
        {
            if (this._disposed)
                throw new InvalidOperationException(EXCP_DISPOSED);
            bool result = this._syncedObjectDict.TryGetValue(syncedObjectID, out SyncedObjectSubscriptions sos);
            syncedObject = (T)sos?.SyncedObject;
            return result;
        }

        /// <summary>
        /// Try to get the <see cref="Peer">subscribers</see> of a <see cref="SyncedObject"/> using its <see cref="SyncedObjectID">ID</see>.
        /// </summary>
        /// <param name="syncedObjectID">The <see cref="SyncedObjectID">ID</see> of the <see cref="SyncedObject"/>.</param>
        /// <param name="subscribers">An array of <see cref="Peer">Peers</see> who are subscribed to this <see cref="SyncedObject"/>; otherwise, an empty array.</param>
        /// <returns><see langword="true"/> if the <see cref="SyncedObject"/> was found; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the server has been disposed.</exception>
        public bool TryGetSyncedObjectSubscribers(SyncedObjectID syncedObjectID, out Peer[] subscribers)
        {
            if (this._disposed)
                throw new InvalidOperationException(EXCP_DISPOSED);

            if (!this._syncedObjectDict.TryGetValue(syncedObjectID, out SyncedObjectSubscriptions sos))
            {
                subscribers = new Peer[0];
                return false;
            }
            subscribers = sos.Subscriptions.ToArray();
            return true;
        }

        /// <summary>
        /// Try to transfer the ownership of a <see cref="SyncedObject"/> using its <see cref="SyncedObjectID">ID</see>.
        /// </summary>
        /// <param name="syncedObjectID">The <see cref="SyncedObjectID">ID</see> of the <see cref="SyncedObject"/>.</param>
        /// <param name="newOwner">The <see cref="Peer"/> who will now own the <see cref="SyncedObject"/>.</param>
        /// <returns><see langword="true"/> if the <see cref="SyncedObject"/> was found and owner was changed; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="newOwner"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the server has been disposed or if the <paramref name="newOwner"/> is not currently connected to the server.</exception>
        public bool TryTransferSyncedObjectOwnership(SyncedObjectID syncedObjectID, Peer newOwner)
        {
            if (this._disposed)
                throw new InvalidOperationException(EXCP_DISPOSED);
            if (!this._connectedPeers.Contains(newOwner))
                throw new InvalidOperationException(EXCP_PEER_NOT_CONNECTED);
            if (newOwner == null)
                throw new ArgumentNullException(nameof(newOwner));
            if (!this._syncedObjectDict.TryGetValue(syncedObjectID, out SyncedObjectSubscriptions sos))
                return false;

            SyncedObject so = sos.SyncedObject;
            Peer prevOwner = so.Owner;

            if (prevOwner == newOwner)
                return false;

            so.Owner = newOwner;
            AddSyncedObjectSubscriptions(syncedObjectID, newOwner);
            so.OnOwnerChange(prevOwner, newOwner);

            this.Send(new UpdateSyncedObjectOwnerPacket(syncedObjectID, newOwner));
            return true;
        }

        /// <summary>
        /// Try to add subscriptions to a <see cref="SyncedObject"/> using its <see cref="SyncedObjectID">ID</see>.
        /// </summary>
        /// <param name="syncedObjectID">The <see cref="SyncedObjectID">ID</see> of the <see cref="SyncedObject"/>.</param>
        /// <param name="subscribers">The new subscribers to add.</param>
        /// <returns><see langword="true"/> if the <see cref="SyncedObject"/> was found; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="subscribers"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="subscribers"/> is empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the server has been disposed.</exception>
        public bool AddSyncedObjectSubscriptions(SyncedObjectID syncedObjectID, params Peer[] subscribers)
        {
            if (this._disposed)
                throw new InvalidOperationException(EXCP_DISPOSED);
            if (subscribers == null)
                throw new ArgumentNullException(nameof(subscribers));
            if (subscribers.Length == 0)
                throw new ArgumentException(EXCP_NO_SUBSCRIBERS, nameof(subscribers));
            if (!this._syncedObjectDict.TryGetValue(syncedObjectID, out SyncedObjectSubscriptions sos))
                return false;

            //Remove nulls, not connected, and existing subscribers from subscribers
            subscribers = subscribers.Where(x => x != null && this._connectedPeers.Contains(x) && !sos.Subscriptions.Contains(x)).ToArray();

            foreach (Peer peer in subscribers)
                sos.Subscriptions.TryAdd(peer);

            this.Send(new CreateSyncedObjectPacket(sos.SyncedObject), subscribers);
            return true;
        }

        /// <summary>
        /// Try to remove subscriptions from a <see cref="SyncedObject"/> using its <see cref="SyncedObjectID">ID</see>.
        /// </summary>
        /// <param name="syncedObjectID">The <see cref="SyncedObjectID">ID</see> of the <see cref="SyncedObject"/>.</param>
        /// <param name="subscribers">The subscribers to remove.</param>
        /// <returns><see langword="true"/> if the <see cref="SyncedObject"/> was found; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="subscribers"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="subscribers"/> is empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the server has been disposed.</exception>
        public bool RemoveSyncedObjectSubscriptions(SyncedObjectID syncedObjectID, params Peer[] subscribers)
        {
            if (this._disposed)
                throw new InvalidOperationException(EXCP_DISPOSED);
            if (subscribers == null)
                throw new ArgumentNullException(nameof(subscribers));
            if (subscribers.Length == 0)
                throw new ArgumentException(EXCP_NO_SUBSCRIBERS, nameof(subscribers));
            if (!this._syncedObjectDict.TryGetValue(syncedObjectID, out SyncedObjectSubscriptions sos))
                return false;

            //Remove nulls and non-subscribers from subscribers
            subscribers = subscribers.Where(x => x != null && sos.Subscriptions.Contains(x)).ToArray();

            foreach (Peer peer in subscribers)
                sos.Subscriptions.TryRemove(peer);

            this.Send(new DestroySyncedObjectPacket(sos.SyncedObject.ID), subscribers);
            return true;
        }


        #endregion


        #region PACKETS

        /// <summary>
        /// Send a <see cref="Packet"/> to all valid remote clients.
        /// </summary>
        /// <param name="packet">The <see cref="Packet"/> to send.</param>
        /// <remarks>
        /// In the event that <paramref name="packet"/> is an instance of <see cref="SyncedObject"/>, <paramref name="packet"/> will
        /// be sent only to those <see cref="Peer">Peers</see> that are subscribed to the <see cref="SyncedObject"/>.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if the server has been disposed.</exception>
        public void Send(Packet packet)
        {
            if (this._disposed)
                throw new InvalidOperationException(EXCP_DISPOSED);

            if (packet is SyncedObject so)
            {
                if (this._syncedObjectDict.TryGetValue(so.ID, out SyncedObjectSubscriptions sos) && this._syncedObjectsToUpdate.TryAdd(so.ID))
                {
                    Peer[] subscribers = sos.Subscriptions.Where(x => x != null && this._connectedPeers.Contains(x)).ToArray();
                    PacketAndRemotes par = new PacketAndRemotes(packet, subscribers);
                    this._scheduledPackets.Enqueue(par);
                }
            }
            else
            {
                Peer[] allPeers = this._connectedPeers.ToArray();
                PacketAndRemotes par = new PacketAndRemotes(packet, allPeers);
                this._scheduledPackets.Enqueue(par);
            }
        }

        /// <summary>
        /// Send a <see cref="Packet"/> to selected remote clients.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="sendTo"></param>
        /// <remarks>
        /// In the event that a <see langword="null"/> value or disconnected <see cref="Peer"/> are passed into the <paramref name="sendTo"/> parameter,
        /// these values will be automatically removed and the <paramref name="packet"/> will be sent to any valid <see cref="Peer">Peers</see>.<br/>
        /// If <paramref name="packet"/> is an instance of a <see cref="SyncedObject"/>, an additional check will prevent <paramref name="packet"/> from
        /// being sent to <see cref="Peer">Peers</see> that are not subscribed to the <see cref="SyncedObject"/>.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if the server has been disposed.</exception>
        public void Send(Packet packet, params Peer[] sendTo)
        {
            if (this._disposed)
                throw new InvalidOperationException(EXCP_DISPOSED);

            if (packet is SyncedObject so)
            {
                if (this._syncedObjectDict.TryGetValue(so.ID, out SyncedObjectSubscriptions sos) && this._syncedObjectsToUpdate.TryAdd(so.ID))
                {
                    Peer[] subscribers = sos.Subscriptions.Intersect(sendTo.Where(x => x != null && this._connectedPeers.Contains(x))).ToArray();
                    PacketAndRemotes par = new PacketAndRemotes(packet, subscribers);
                    this._scheduledPackets.Enqueue(par);
                }
            }
            else
            {
                PacketAndRemotes par = new PacketAndRemotes(packet, this.RemotePeers.Intersect(sendTo.Where(x => x != null && this._connectedPeers.Contains(x))).ToArray());
                this._scheduledPackets.Enqueue(par);
            }
        }

        #endregion


        #region RPC

        private bool Call(SyncedObject caller, MethodInfo procedure, TargetedDynamicDelegate callback, Peer[] sendTo, params object[] parameters)
        {
            if (procedure == null)
                return false;

            Type declaringType = procedure.DeclaringType;
            ProcedureHashCode procedureHash = procedure.GetProcedureHash();

            ObjectCache objectCache = ObjectCache.FromType(declaringType);
            ObjectProcedure objectProcedure = objectCache.GetProcedure((ulong)procedureHash);

            //Validate direction this procedure would be traveling.
            RPCDirection rpcDirection = PacketCacheHelper.GetRPCDirection(objectProcedure);

            if (rpcDirection == RPCDirection.ClientToServer)
                return false;

            //Get RPC handle
            int rpcHandle = Interlocked.Increment(ref this._rpcCounter);

            //Store the callback.
            if (!this._rpcCallbacks.TryAdd(rpcHandle, callback))
                return false;

            //Create an RPCCall Packet
            RPCCallPacket rpcPacket = new RPCCallPacket(objectProcedure.HashCode, caller != null ? caller.ID : default(SyncedObjectID), rpcHandle, parameters);

            //Send RPCCall Packet

            if (sendTo == null)
                sendTo = this._connectedPeers.ToArray();

            if (caller != null)
            {
                if (this._syncedObjectDict.TryGetValue(caller.ID, out SyncedObjectSubscriptions sos))
                    this.Send(rpcPacket, sos.Subscriptions.Intersect(sendTo.Where(x => x != null && this._connectedPeers.Contains(x))).ToArray());
                else
                    return false;
            }
            else
            {
                this.Send(rpcPacket, sendTo);
            }

            return true;
        }


        #region PARAMETERLESS PROCEDURES

        public bool Call(Action procedure, Action callback = null)
        {
            return this.Call(null, procedure?.Method, callback?.Bind(), null);
        }

        public bool Call(Action procedure, Action callback = null, params Peer[] sendTo)
        {
            return this.Call(null, procedure?.Method, callback?.Bind(), sendTo);
        }

        public bool Call(SyncedObject caller, Action procedure, Action callback = null)
        {
            return this.Call(caller, procedure?.Method, callback?.Bind(), null);
        }

        public bool Call(SyncedObject caller, Action procedure, Action callback = null, params Peer[] sendTo)
        {
            return this.Call(caller, procedure?.Method, callback?.Bind(), sendTo);
        }

        public bool Call<TResult>(Func<TResult> procedure, Action<TResult> callback = null)
        {
            return this.Call(null, procedure?.Method, callback?.Bind(), null);
        }

        public bool Call<TResult>(Func<TResult> procedure, Action<TResult> callback = null, params Peer[] sendTo)
        {
            return this.Call(null, procedure?.Method, callback?.Bind(), sendTo);
        }

        public bool Call<TResult>(SyncedObject caller, Func<TResult> procedure, Action<TResult> callback = null)
        {
            return this.Call(caller, procedure?.Method, callback?.Bind(), null);
        }

        public bool Call<TResult>(SyncedObject caller, Func<TResult> procedure, Action<TResult> callback = null, params Peer[] sendTo)
        {
            return this.Call(caller, procedure?.Method, callback?.Bind(), sendTo);
        }


        #endregion


        #region SINGLE PARAMETER PROCEDURES

        public bool Call<T1>(Action<T1> procedure, T1 param1, Action callback = null)
        {
            return this.Call(null, procedure?.Method, callback?.Bind(), null, param1);
        }

        public bool Call<T1>(Action<T1> procedure, T1 param1, Action callback = null, params Peer[] sendTo)
        {
            return this.Call(null, procedure?.Method, callback?.Bind(), sendTo, param1);
        }

        public bool Call<T1>(SyncedObject caller, Action<T1> procedure, T1 param1, Action callback = null)
        {
            return this.Call(caller, procedure?.Method, callback?.Bind(), null, param1);
        }

        public bool Call<T1>(SyncedObject caller, Action<T1> procedure, T1 param1, Action callback = null, params Peer[] sendTo)
        {
            return this.Call(caller, procedure?.Method, callback?.Bind(), sendTo, param1);
        }

        public bool Call<T1, TResult>(Func<T1, TResult> procedure, T1 param1, Action<TResult> callback = null)
        {
            return this.Call(null, procedure?.Method, callback?.Bind(), null, param1);
        }

        public bool Call<T1, TResult>(Func<T1, TResult> procedure, T1 param1, Action<TResult> callback = null, params Peer[] sendTo)
        {
            return this.Call(null, procedure?.Method, callback?.Bind(), sendTo, param1);
        }

        public bool Call<T1, TResult>(SyncedObject caller, Func<T1, TResult> procedure, T1 param1, Action<TResult> callback = null)
        {
            return this.Call(caller, procedure?.Method, callback?.Bind(), null, param1);
        }

        public bool Call<T1, TResult>(SyncedObject caller, Func<T1, TResult> procedure, T1 param1, Action<TResult> callback = null, params Peer[] sendTo)
        {
            return this.Call(caller, procedure?.Method, callback?.Bind(), sendTo, param1);
        }

        #endregion


        internal void TryExecuteCallback(int rpcHandle, bool wasRPCSuccessful, object returnValue = null)
        {
            if (this._rpcCallbacks.Remove(rpcHandle, out TargetedDynamicDelegate callback) && wasRPCSuccessful)
            {
                callback?.Invoke((returnValue != null) ? new object[] { returnValue } : new object[0]);
            }
        }


        #endregion


        #region NESTED CLASSES

        /// <summary>
        /// Represents a <see cref="Parcel.Packets.SyncedObject"/> and its Subscribers.
        /// </summary>
        private class SyncedObjectSubscriptions
        {
            /// <summary>
            /// The <see cref="Parcel.Packets.SyncedObject"/>.
            /// </summary>
            public SyncedObject SyncedObject { get; set; }

            /// <summary>
            /// A set of <see cref="Peer">Peers</see> that are subscribed to the <see cref="Parcel.Packets.SyncedObject"/>.
            /// </summary>
            public ConcurrentHashSet<Peer> Subscriptions { get; set; }

            /// <summary>
            /// Construct a new instance of SyncedObjectSubscriptions.
            /// </summary>
            /// <param name="syncedObject">The <see cref="Parcel.Packets.SyncedObject"/> to use.</param>
            public SyncedObjectSubscriptions(SyncedObject syncedObject)
            {
                this.SyncedObject = syncedObject;
                this.Subscriptions = new ConcurrentHashSet<Peer>();
            }
        }

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

        /// <summary>
        /// Represents a <see cref="Parcel.Packets.Packet"/> and the remote <see cref="Peer">Peers</see> it is being sent to.
        /// </summary>
        private struct PacketAndRemotes
        {
            /// <summary>
            /// The <see cref="Parcel.Packets.Packet"/>.
            /// </summary>
            public Packet Packet { get; private set; }

            /// <summary>
            /// The <see cref="Peer">Peers</see> that the <see cref="Parcel.Packets.Packet"/> is being sent to.
            /// </summary>
            public Peer[] Remotes { get; private set; }

            /// <summary>
            /// Construct a new instance of PacketAndRemotes.
            /// </summary>
            /// <param name="packet">The <see cref="Parcel.Packets.Packet"/> to use.</param>
            /// <param name="remotes">The <see cref="Peer">Peers</see> being sent to.</param>
            public PacketAndRemotes(Packet packet, Peer[] remotes)
            {
                this.Packet = packet;
                this.Remotes = remotes;
            }
        }

        #endregion
    }
}
