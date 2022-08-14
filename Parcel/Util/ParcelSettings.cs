using Parcel.Debug;
using Parcel.Lib;
using Parcel.Networking;
using Parcel.Serialization;
using System;
using System.Threading;

namespace Parcel
{
    /// <summary>
    /// Represents a collection of settings to be used by either a <see cref="ParcelClient"/> or <see cref="ParcelServer"/>.
    /// </summary>
    /// <remarks>
    /// Each instance of ParcelSettings can only be used by 1 instance of <see cref="ParcelClient"/> or <see cref="ParcelServer"/>.
    /// The settings will be bound to the first instance they are passed to.
    /// </remarks>
    public sealed class ParcelSettings
    {
        /// <summary>
        /// The <see cref="Parcel.Peer"/> for the <see cref="ParcelClient"/> or <see cref="ParcelServer"/> to use.
        /// </summary>
        public Peer Peer { get; private set; }

        /// <summary>
        /// The amount of time in milliseconds allowed for connecting to a remote user before connection times out.
        /// </summary>
        public int ConnectionTimeout { get; private set; }

        /// <summary>
        /// The amount of time in milliseconds allowed since last receiving a Packet from a user before considering them disconnected.
        /// </summary>
        public int DisconnectionTimeout { get; private set; }

        /// <summary>
        /// The type of <see cref="INetworkAdapter"/> for the <see cref="ParcelClient"/> or <see cref="ParcelServer"/> to use.
        /// </summary>
        public Type NetworkAdapterType { get; private set; }

        /// <summary>
        /// Whether updates should be performed automatically or manually.
        /// </summary>
        /// <remarks>
        /// If this value is <see langword="false"/>, updates will need to be performed manually by calling <see cref="ParcelClient.Loop"/>
        /// or <see cref="ParcelServer.Loop"/>.
        /// </remarks>
        public bool PerformUpdatesAutomatically { get; private set; }

        /// <summary>
        /// The number of iterations of the main loop to run every seconds.
        /// </summary>
        public int UpdatesPerSecond { get; private set; }

        /// <summary>
        /// The number of milliseconds in each update.
        /// </summary>
        public int MillisecondsPerUpdate => 1000 / UpdatesPerSecond;

        /// <summary>
        /// The number of unreliable packets to cluster into a single real packet.
        /// </summary>
        /// <remarks>
        /// When <see cref="Packets.Packet">Packets</see> are serializing, they will be grouped together based on <see cref="Reliability"/> 
        /// and be sent as a singular packet. <br/>
        /// It is recommended to keep this number small. Default is 5.
        /// </remarks>
        public int UnreliablePacketGroupSize { get; private set; }

        /// <summary>
        /// The number of reliable packets to cluster into a single real packet.
        /// </summary>
        /// <remarks>
        /// When <see cref="Packets.Packet">Packets</see> are serializing, they will be grouped together based on <see cref="Reliability"/> 
        /// and be sent as a singular packet.
        /// </remarks>
        public int ReliablePacketGroupSize { get; private set; }

        /// <summary>
        /// A <see cref="NetworkDebugger"/> instance to use for debugging.
        /// </summary>
        public NetworkDebugger Debugger { get; private set; }

        /// <summary>
        /// The <see cref="Serialization.SerializerResolver"/> for the <see cref="ParcelClient"/> or <see cref="ParcelServer"/> to use.
        /// </summary>
        public SerializerResolver SerializerResolver { get; private set; }

        /// <summary>
        /// The behavior for handling <see cref="Packets.SyncedObject">SyncedObjects</see> a <see cref="ParcelServer"/> should perform when a user disconnects.
        /// </summary>
        /// <remarks>
        /// This setting only applies for <see cref="ParcelServer"/> instances.
        /// </remarks>
        public ServerDisconnectionBehavior ServerDisconnectionBehavior { get; private set; }

        /// <summary>
        /// Whether this settings object has been bound to a <see cref="ParcelClient"/> or <see cref="ParcelServer"/> or not.
        /// </summary>
        internal bool Locked 
        { 
            get { return Interlocked.CompareExchange(ref _locked, 1, 1) == 1; }
            set { if (value) return; Interlocked.Exchange(ref _locked, 1); }
        }
        private int _locked;


        #region CONSTRUCTOR

        /// <summary>
        /// Construct a new instance of ParcelSettings.
        /// </summary>
        /// <param name="peer">The <see cref="Parcel.Peer"/> for the <see cref="ParcelClient"/> or <see cref="ParcelServer"/> to use.</param>
        /// <param name="firstConnectionTimeout">The amount of time in milliseconds allowed for connecting to a remote user before connection times out.</param>
        /// <param name="disconnectionTimeout">The amount of time in milliseconds allowed since last receiving a Packet from a user before considering them disconnected.</param>
        /// <param name="networkAdapterType">The type of <see cref="INetworkAdapter"/> for the <see cref="ParcelClient"/> or <see cref="ParcelServer"/> to use.</param>
        /// <param name="updatesPerSecond">The number of iterations of the main loop to run every seconds.</param>
        /// <param name="performUpdatesAutomatically">Whether updates should be performed automatically or manually.</param>
        /// <param name="unreliablePacketGroupSize">The number of unreliable packets to cluster into a single real packet.</param>
        /// <param name="reliablePacketGroupSize">The number of reliable packets to cluster into a single real packet.</param>
        /// <param name="debugger">A <see cref="NetworkDebugger"/> instance to use for debugging.</param>
        /// <param name="serializerResolver">The <see cref="Serialization.SerializerResolver"/> for the <see cref="ParcelClient"/> or <see cref="ParcelServer"/> to use.</param>
        /// <param name="serverDisconnectionBehavior">The behavior for handling <see cref="Packets.SyncedObject">SyncedObjects</see> a <see cref="ParcelServer"/> should perform when a user disconnects.</param>
        /// <remarks>
        /// This constructor will exclusively be called by the <see cref="Parcel.ParcelSettingsBuilder">ParcelSettingsBuilder</see> utility.
        /// </remarks>
        internal ParcelSettings(Peer peer, int firstConnectionTimeout, int disconnectionTimeout, Type networkAdapterType, int updatesPerSecond,
            bool performUpdatesAutomatically, int unreliablePacketGroupSize, int reliablePacketGroupSize, NetworkDebugger debugger, 
            SerializerResolver serializerResolver, ServerDisconnectionBehavior serverDisconnectionBehavior)
        {
            this.Peer = peer;
            this.ConnectionTimeout = firstConnectionTimeout;
            this.DisconnectionTimeout = disconnectionTimeout;
            this.NetworkAdapterType = networkAdapterType;
            this.UpdatesPerSecond = updatesPerSecond;
            this.PerformUpdatesAutomatically = performUpdatesAutomatically;
            this.UnreliablePacketGroupSize = unreliablePacketGroupSize;
            this.ReliablePacketGroupSize = reliablePacketGroupSize;
            this.Debugger = debugger;
            this.SerializerResolver = serializerResolver;
            this.ServerDisconnectionBehavior = serverDisconnectionBehavior;
        }

        #endregion


        #region METHODS

        /// <summary>
        /// Create a new <see cref="INetworkAdapter"/> instance using the <see cref="NetworkAdapterType"/> settings.
        /// </summary>
        /// <returns>A new <see cref="INetworkAdapter"/> instance.</returns>
        internal INetworkAdapter CreateNewNetworkAdapter()
        {
            return (INetworkAdapter)Create.New(NetworkAdapterType);
        }

        #endregion

    }
}
