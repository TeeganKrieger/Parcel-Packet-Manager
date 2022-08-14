using Parcel.Debug;
using Parcel.Networking;
using Parcel.Serialization;
using System;

namespace Parcel
{

    /// <summary>
    /// Facilitates construction of a new <see cref="ParcelSettings"/> instance.
    /// </summary>
    /// <remarks>
    /// The PeerBuilder class helps ensure proper construction of a <see cref="ParcelSettings"/> instance.
    /// </remarks>
    /// <example>
    /// Construct a new <see cref="ParcelSettings"/> using the ParcelSettingsBuilder.
    /// <code>
    /// Peer peer = new PeerBuilder().SetAddress("localhost").SetPort(8181);
    /// 
    /// NetworkDebugger debugger = new NetworkDebugger(new ConsoleLogger());
    /// 
    /// ParcelSettings settings = new ParcelSettingsBuilder().SetPeer(peer).SetNetworkAdapter&lt;UdpNetworkAdapter&gt;()
    /// .SetConnectionTimeout(5000).AddNetworkDebugger(debugger);
    /// </code>
    /// </example>
    public sealed class ParcelSettingsBuilder
    {
        private const string EXCP_TIMEOUT_RANGE = "Timeout expected to be greater than 0. Got {0}.";
        private const string EXCP_UNSP_PEER = "Could not create ParcelSettings because no Peer was specified! Please specify a Peer using {0}.";
        private const string EXCP_UNSP_NET_ADD = "Could not create ParcelSettings because no network adapter was specified! Please specify a network adapter using SetNetworkAdapter<T>().";
        private const string EXCP_UPDATE_RANGE = "The provided updates per second ({0}) is not within the valid range of 1-1000!";
        private const string EXCP_GROUP_SIZE = "Group size cannot be 0 or negative.";

        private Peer _peer = null;
        private int _connectionTimeout = 1500;
        private int _disconnectionTimeout = 1500;
        private int _updatesPerSecond = 30;
        private bool _performUpdatesAutomatic = true;
        private int _unreliablePacketGroupSize = 5;
        private int _reliablePacketGroupSize = 8;
        private Type _networkAdapterType = null;
        private NetworkDebugger _debugger = null;
        private SerializerResolver _serializerResolver = SerializerResolver.Global;
        private ServerDisconnectionBehavior _serverDisconnectionBehavior;


        #region CONSTRUCTOR

        /// <summary>
        /// Construct a new instance of ParcelSettingsBuilder.
        /// </summary>
        public ParcelSettingsBuilder()
        {

        }

        #endregion


        #region SETTINGS

        /// <summary>
        /// Set the <see cref="Peer"/> for the <see cref="ParcelClient"/> or <see cref="ParcelServer"/> to use.
        /// </summary>
        /// <param name="peer">The <see cref="Peer"/> to use.</param>
        /// <returns>The current ParcelSettingsBuilder instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="peer"/> is null.</exception>
        public ParcelSettingsBuilder SetPeer(Peer peer)
        {
            if (peer == null)
                throw new ArgumentNullException(nameof(peer));

            this._peer = peer;
            return this;
        }

        /// <summary>
        /// Set the type of <see cref="INetworkAdapter"/> for the <see cref="ParcelClient"/> or <see cref="ParcelServer"/> to use.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="INetworkAdapter"/> to use.</typeparam>
        /// <returns>The current ParcelSettingsBuilder instance.</returns>
        public ParcelSettingsBuilder SetNetworkAdapter<T>() where T : INetworkAdapter, new()
        {
            this._networkAdapterType = typeof(T);
            return this;
        }

        /// <summary>
        /// Set the amount of time in milliseconds allowed for connecting to a remote user before connection times out.
        /// </summary>
        /// <param name="milliseconds">The number of milliseconds.</param>
        /// <returns>The current ParcelSettingsBuilder instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="milliseconds"/> is less than 1.</exception>
        public ParcelSettingsBuilder SetConnectionTimeout(int milliseconds)
        {
            if (milliseconds < 1)
                throw new ArgumentOutOfRangeException(nameof(milliseconds), string.Format(EXCP_TIMEOUT_RANGE, milliseconds));

            this._connectionTimeout = milliseconds;
            return this;
        }

        /// <summary>
        /// Set the amount of time in milliseconds allowed since last receiving a Packet from a user before considering them disconnected.
        /// </summary>
        /// <param name="milliseconds">The number of milliseconds.</param>
        /// <returns>The current ParcelSettingsBuilder instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="milliseconds"/> is less than 1.</exception>
        public ParcelSettingsBuilder SetDisconnectionTimeout(int milliseconds)
        {
            if (milliseconds < 1)
                throw new ArgumentOutOfRangeException(nameof(milliseconds), string.Format(EXCP_TIMEOUT_RANGE, milliseconds));

            this._disconnectionTimeout = milliseconds;
            return this;
        }

        /// <summary>
        /// Set the number of iterations of the main loop to run every seconds.
        /// </summary>
        /// <param name="updates">The number of updates.</param>
        /// <returns>The current ParcelSettingsBuilder instance.</returns>
        /// <exception cref="ArgumentException">Thrown is <paramref name="updates"/> is less than 1 or greater than 1000.</exception>
        public ParcelSettingsBuilder SetUpdatesPerSecond(int updates)
        {
            if (updates < 1 || updates > 1000)
                throw new ArgumentException(String.Format(EXCP_UPDATE_RANGE, updates), nameof(updates));
            this._updatesPerSecond = updates;
            return this;
        }

        /// <summary>
        /// Set the number of unreliable packets to cluster into a single real packet.
        /// </summary>
        /// <param name="groupSize">The number of packets to cluster.</param>
        /// <returns>The current ParcelSettingsBuilder instance.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="groupSize"/> is less than 1.</exception>
        public ParcelSettingsBuilder SetUnreliablePacketGroupSize(int groupSize)
        {
            if (groupSize < 1)
                throw new ArgumentException(EXCP_GROUP_SIZE, nameof(groupSize));
            this._unreliablePacketGroupSize = groupSize;
            return this;
        }

        /// <summary>
        /// Set the number of reliable packets to cluster into a single real packet.
        /// </summary>
        /// <param name="groupSize">The number of packets to cluster.</param>
        /// <returns>The current ParcelSettingsBuilder instance.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="groupSize"/> is less than 1.</exception>
        public ParcelSettingsBuilder SetReliablePacketGroupSize(int groupSize)
        {
            if (groupSize < 1)
                throw new ArgumentException(EXCP_GROUP_SIZE, nameof(groupSize));
            this._reliablePacketGroupSize = groupSize;
            return this;
        }

        /// <summary>
        /// Add a <see cref="NetworkDebugger"/> instance to use for debugging.
        /// </summary>
        /// <param name="debugger">The <see cref="NetworkDebugger"/> to use.</param>
        /// <returns>The current ParcelSettingsBuilder instance.</returns>
        public ParcelSettingsBuilder AddNetworkDebugger(NetworkDebugger debugger)
        {
            if (debugger == null)
                throw new ArgumentNullException(nameof(debugger));

            this._debugger = debugger;
            return this;
        }

        /// <summary>
        /// Set whether the <see cref="ParcelClient"/> or <see cref="ParcelServer"/> should perform updates automatically or manually.
        /// </summary>
        /// <param name="should">Whether to perform updates automatically or not.</param>
        /// <returns>The current ParcelSettingsBuilder instance.</returns>
        public ParcelSettingsBuilder PerformUpdatesAutomatically(bool should)
        {
            this._performUpdatesAutomatic = should;
            return this;
        }

        /// <summary>
        /// Set the <see cref="SerializerResolver"/> for the <see cref="ParcelClient"/> or <see cref="ParcelServer"/> to use.
        /// </summary>
        /// <param name="serializerResolver">The <see cref="SerializerResolver"/> to use.</param>
        /// <returns>The current ParcelSettingsBuilder instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="serializerResolver"/> is null.</exception>
        public ParcelSettingsBuilder SetSerializerResolver(SerializerResolver serializerResolver)
        {
            if (serializerResolver == null)
                throw new ArgumentNullException(nameof(serializerResolver));

            this._serializerResolver = serializerResolver;
            return this;
        }

        /// <summary>
        /// Set the behavior for handling <see cref="Packets.SyncedObject">SyncedObjects</see> a <see cref="ParcelServer"/> should perform when a user disconnects.
        /// </summary>
        /// <param name="serverDisconnectionBehavior">The behavior the <see cref="ParcelServer"/> should perform.</param>
        /// <returns>The current ParcelSettingsBuilder instance.</returns>
        public ParcelSettingsBuilder SetServerDisconnectionBehavior(ServerDisconnectionBehavior serverDisconnectionBehavior)
        {
            this._serverDisconnectionBehavior = serverDisconnectionBehavior;
            return this;
        }

        #endregion


        #region OPERATORS

        /// <summary>
        /// Implicitly convert a <see cref="ParcelSettingsBuilder"/> instance to a <see cref="ParcelSettings"/> instance.
        /// </summary>
        /// <param name="builder">The ParcelSettingsBuilder to convert.</param>
        public static implicit operator ParcelSettings(ParcelSettingsBuilder builder)
        {
            //Perform all final checks
            if (builder._peer == null)
                throw new InvalidOperationException(string.Format(EXCP_UNSP_PEER, "SetPort()"));
            if (builder._networkAdapterType == null)
                throw new InvalidOperationException(EXCP_UNSP_NET_ADD);

            return new ParcelSettings(builder._peer, builder._connectionTimeout, builder._disconnectionTimeout, builder._networkAdapterType,
                builder._updatesPerSecond, builder._performUpdatesAutomatic, builder._unreliablePacketGroupSize, builder._reliablePacketGroupSize,
                builder._debugger, builder._serializerResolver, builder._serverDisconnectionBehavior);
        }

        #endregion

    }
}
