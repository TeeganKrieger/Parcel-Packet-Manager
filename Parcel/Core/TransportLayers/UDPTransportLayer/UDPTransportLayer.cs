using Parcel.Debug;
using Parcel.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Parcel.Networking
{

    //TODO: Make Disconnection totally Synchronous. I.E. Remove the disconnecting state and perform the entire handshake before setting the disconnected state.
    //Also make it happen in a single call just like connections.

    /// <summary>
    /// Network Adapter implementation using the UDP protocol.
    /// </summary>
    public sealed partial class UDPTransportLayer : ITransportLayer, IDisposable
    {
        private const string EXCP_ALREADY_CONNECTED = "Failed to perform connection operation as the adapter is already connected to the Peer.";

        private bool _isServer;
        private Peer _self;
        private ParcelSettings _settings;

        private ConcurrentDictionary<ConnectionToken, PeerChannel> _channels;
        private ConcurrentQueue<UnprocessedPacket> _unprocessedPackets;

        private UdpClient _client;
        private CancellationTokenSource _adapterTaskCancellationSource;

        private NetworkDebugger _debugger;

        /// <inheritdoc/>
        public event InitialConnectionDelegate OnInitialConnection;

        /// <inheritdoc/>
        public event ConnectionDelegate OnConnection;

        /// <inheritdoc/>
        public event DisconnectionDelegate OnDisconnection;


        #region CONSTRUCTOR AND DISPOSE

        /// <summary>
        /// Construct a new instance of UdpNetworkAdapter.
        /// </summary>
        public UDPTransportLayer()
        {
            this._unprocessedPackets = new ConcurrentQueue<UnprocessedPacket>();
            this._channels = new ConcurrentDictionary<ConnectionToken, PeerChannel>();

            this._adapterTaskCancellationSource = new CancellationTokenSource();

            Task.Run(ReceptionTask, this._adapterTaskCancellationSource.Token);
            Task.Run(ChannelLoopTask, this._adapterTaskCancellationSource.Token);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this._adapterTaskCancellationSource.Cancel();
        }

        #endregion


        #region INETWORKADAPTER IMPLEMENTATION

        /// <inheritdoc/>
        public void Start(bool isServer, ParcelSettings settings)
        {
            this._isServer = isServer;
            this._settings = settings;
            this._debugger = settings.Debugger;
            this._self = settings.Peer;
            this._client = new UdpClient(settings.Peer.Port);
        }

        /// <inheritdoc/>
        public async Task<ConnectionResult> ConnectTo(ConnectionToken connectionToken)
        {
            if (this._channels.ContainsKey(connectionToken))
                throw new InvalidOperationException(EXCP_ALREADY_CONNECTED);

            Peer remote = new PeerBuilder().SetAddress(connectionToken.Address)
                .SetPort(connectionToken.Port);

            PeerChannel remoteChannel = new PeerChannel(this, remote);

            ConnectionResult results = await remoteChannel.Connect();

            return results;
        }

        /// <inheritdoc/>
        public async Task DisconnectFrom(Peer peer, object disconnectionObject = null)
        {
            ConnectionToken key = peer.GetConnectionToken();

            if (this._channels.TryGetValue(key, out PeerChannel remoteChannel))
            {
                await remoteChannel.Disconnect(disconnectionObject);
            }
        }

        /// <inheritdoc/>
        public bool GetNextPacket(out ByteReader reader, out Peer sender)
        {
            if (this._unprocessedPackets.TryDequeue(out UnprocessedPacket next))
            {
                reader = next.ByteReader;
                sender = next.Sender;
                return true;
            }
            reader = null;
            sender = null;
            return false;
        }

        /// <inheritdoc/>
        public void SendPacketTo(Peer peer, Reliability reliability, ByteWriter writer)
        {
            ConnectionToken key = peer.GetConnectionToken();

            if (this._channels.TryGetValue(key, out PeerChannel channel))
            {
                channel.Send(writer, reliability);
            }
            else
                throw new InvalidOperationException(); //TODO: Create better exception
        }

        /// <inheritdoc/>
        public int GetPing(Peer peer)
        {
            ConnectionToken key = peer.GetConnectionToken();

            if (this._channels.TryGetValue(key, out PeerChannel channel))
            {
                return channel.Ping;
            }
            else
                throw new InvalidOperationException(); //TODO: Create better exception
        }

        #endregion


        #region TASKS

        /// <summary>
        /// Asynchronous Task that will occasionally attempt to resend reliable packets that were lost and destroy disconnected PeerChannels.
        /// </summary>
        private async Task ChannelLoopTask()
        {
            //Wait for client to start
            while (this._client == null)
                continue;

            while (true)
            {
                ConnectionToken[] keys = this._channels.Keys.ToArray();

                foreach (ConnectionToken ct in keys)
                {
                    if (this._channels.TryGetValue(ct, out PeerChannel channel))
                    {
                        channel.Loop();
                    }
                }

                await Task.Delay(this._settings.MillisecondsPerUpdate);
            }
        }

        /// <summary>
        /// Synchronous Task that will occasionally attempt to gather incoming packets from the UdpClient.
        /// </summary>
        private void ReceptionTask()
        {
            //Wait for client to start
            while (this._client == null)
                continue;

            IPEndPoint remoteIP = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                byte[] packet = this._client.Receive(ref remoteIP);
                string address = remoteIP.Address.ToString();
                int port = remoteIP.Port;

                ConnectionToken remote = new ConnectionToken(address, port);

                //Known Sender
                if (_channels.TryGetValue(remote, out PeerChannel channel))
                {
                    channel.HandleIncomingPacket(packet);
                }
                //Unknown Sender
                else
                {
                    channel = new PeerChannel(this, new PeerBuilder().SetAddress(address).SetPort(port));
                    channel.HandleIncomingPacket(packet);
                }
            }
        }

        #endregion

    }
}
