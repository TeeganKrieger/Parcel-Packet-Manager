using Parcel.Debug;
using Parcel.Serialization;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Parcel
{
    /// <summary>
    /// Network Adapter implementation using the UDP protocol.
    /// </summary>
    public sealed partial class UdpNetworkAdapter : INetworkAdapter, IDisposable
    {
        private bool _isServer;
        private Peer _self;
        private ParcelSettings _settings;

        private ConcurrentDictionary<ConnectionToken, PeerChannel> _channels;
        private ConcurrentQueue<RawPacket> _rawPackets;

        private UdpClient _client;
        private CancellationTokenSource _adapterTaskCancellationSource;

        private NetworkDebugger _debugger;

        /// <inheritdoc/>
        public event ConnectionEvent OnRecievedConnection;

        /// <inheritdoc/>
        public event ConnectionEvent OnRecievedDisconnection;


        #region CONSTRUCTOR AND DISPOSE

        /// <summary>
        /// Construct a new instance of UdpNetworkAdapter.
        /// </summary>
        public UdpNetworkAdapter()
        {
            this._rawPackets = new ConcurrentQueue<RawPacket>();
            this._channels = new ConcurrentDictionary<ConnectionToken, PeerChannel>();

            this._adapterTaskCancellationSource = new CancellationTokenSource();

            Task.Run(ReceptionTask, this._adapterTaskCancellationSource.Token);
            Task.Run(ResendTask, this._adapterTaskCancellationSource.Token);
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
        public async Task<Peer> ConnectTo(ConnectionToken connectionToken)
        {
            Peer remote = new PeerBuilder().SetAddress(connectionToken.Address)
                .SetPort(connectionToken.Port);

            PeerChannel remoteChannel = new PeerChannel(remote, this);

            this._channels.TryAdd(connectionToken, remoteChannel);

            bool failed = false;

            CancellationTokenSource globalCancellationSource = new CancellationTokenSource();
            CancellationToken globalCancellationToken = globalCancellationSource.Token;

            async Task Timeout()
            {
                await Task.Delay(this._settings.ConnectionTimeout);
                failed = true;
                globalCancellationSource.Cancel();
            }

            async Task Connected()
            {
                while (remoteChannel.ConnectionState != ConnectionState.Connected)
                    await Task.Delay(1);
                failed = false;
                globalCancellationSource.Cancel();
            }

            remoteChannel.ConnectToRemote();

            await Task.WhenAny(Task.Run(Timeout, globalCancellationToken), Task.Run(Connected, globalCancellationToken));

            if (failed)
            {
                this._channels.TryRemove(connectionToken, out _);
                return null;
            }
            else
            {
                return remoteChannel.Remote;
            }
        }

        /// <inheritdoc/>
        public void DisconnectFrom(Peer peer)
        {

        }

        /// <inheritdoc/>
        public bool GetNextPacket(out ByteReader reader, out Peer sender)
        {
            if (this._rawPackets.TryDequeue(out RawPacket next))
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
                channel.SendPacket(writer, reliability);
            }
            else
                throw new InvalidOperationException(); //TODO: Create better exception
        }

        #endregion


        #region TASKS

        /// <summary>
        /// Asynchronous Task that will occasionally attempt to resend reliable packets that were lost.
        /// </summary>
        private async Task ResendTask()
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
                        channel.TryResend();
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

            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                byte[] packet = this._client.Receive(ref remoteEP);
                string address = remoteEP.Address.ToString();
                int port = remoteEP.Port;

                ConnectionToken remote = new ConnectionToken(address, port);

                //Known Sender
                if (_channels.TryGetValue(remote, out PeerChannel channel))
                {
                    channel.RecievePacket(packet);
                }
                //Unknown Sender
                else
                {
                    channel = new PeerChannel(new PeerBuilder().SetAddress(address).SetPort(port), this);
                    this._channels.TryAdd(remote, channel);
                    channel.RecievePacket(packet);
                }
            }
        }

        #endregion


        #region NESTED CLASSES

        /// <summary>
        /// Represents a packet in its raw state and additional information about the packet.
        /// </summary>
        private sealed class RawPacket
        {
            public DateTimeOffset TimeRecievedAt { get; private set; }
            public ByteReader ByteReader { get; private set; }
            public Peer Sender { get; private set; }

            /// <summary>
            /// Construct a new instance of RawPacket.
            /// </summary>
            /// <param name="timeRecievedAt">The time the packet was received at.</param>
            /// <param name="byteReader">A <see cref="Parcel.Serialization.ByteReader"/> with the packet payload.</param>
            /// <param name="sender">The <see cref="Peer"/> that sent the packet.</param>
            public RawPacket(DateTimeOffset timeRecievedAt, ByteReader byteReader, Peer sender)
            {
                TimeRecievedAt = timeRecievedAt;
                ByteReader = byteReader;
                Sender = sender;
            }
        }

        #endregion

    }
}
