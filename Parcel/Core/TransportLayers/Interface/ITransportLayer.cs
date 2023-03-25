using Parcel.Serialization;
using System.Threading.Tasks;

namespace Parcel.Networking
{

    /// <summary>
    /// Defines implementation contract for all Network Adapters.
    /// </summary>
    /// <remarks>
    /// Network adapters are responsible for establishing connections and handling data transmission between <see cref="Peer">Peers</see>.<br/>
    /// When implementing a Network Adapter, it is essential to support reliable data transmission. Unreliable data transmission can be optionally
    /// implemented.
    /// <br/><br/>
    /// Parcel comes with a few built in adapters:<br/>
    /// The <see cref="Parcel.UdpNetworkAdapter">UDP Network Adapter</see>: Handles packets using the udp protocol.<br/>
    /// The <see cref="">Steam Adapter</see>: Handles packets using the Steam API's networking solution.
    /// </remarks>
    public interface ITransportLayer
    {

        /// <summary>
        /// Event triggered when a connection event is being processed.
        /// </summary>
        event InitialConnectionDelegate OnInitialConnection;

        /// <summary>
        /// Event triggered when a Peer connects to the current device.
        /// </summary>
        event ConnectionDelegate OnConnection;

        /// <summary>
        /// Event triggered when a Peer disconnects from the current device.
        /// </summary>
        event DisconnectionDelegate OnDisconnection;

        /// <summary>
        /// Initiate the Network Adapter.
        /// </summary>
        /// <param name="isServer">Whether the network adapter is associated to a <see cref="ParcelServer"/> or <see cref="ParcelClient"/> instance.</param>
        /// <param name="settings">The <see cref="ParcelSettings"/> instance used by the <see cref="ParcelServer"/> or <see cref="ParcelClient"/> that owns the network adapter.</param>
        void Start(bool isServer, ParcelSettings settings);

        /// <summary>
        /// Open a connection with a remote user.
        /// </summary>
        /// <param name="connectionToken">The <see cref="ConnectionToken"/> used to open a connection to the remote user.</param>
        /// <returns>A <see cref="ConnectionResult"/> struct containing the results of the connection.</returns>
        Task<ConnectionResult> ConnectTo(ConnectionToken connectionToken);

        /// <summary>
        /// Close a connection with a remote user.
        /// </summary>
        /// <param name="peer">The <see cref="Peer"/> to close the connection with.</param>
        /// <param name="disconnectionObject">The object to send to the disconnected <see cref="Peer"/>.</param>
        Task DisconnectFrom(Peer peer, object disconnectionObject = null);
        
        /// <summary>
        /// Send a packet to a remote user.
        /// </summary>
        /// <param name="peer">The <see cref="Peer"/> to send the packet to.</param>
        /// <param name="reliability">The <see cref="Reliability"/> of the packet.</param>
        /// <param name="writer">A <see cref="Parcel.Serialization.ByteWriter">ByteWriter</see> containing the packet payload.</param>
        void SendPacketTo(Peer peer, Reliability reliability, ByteWriter writer);
        
        /// <summary>
        /// Get the next incoming packet.
        /// </summary>
        /// <param name="reader">A <see cref="Parcel.Serialization.ByteReader">ByteReader</see> containing the entirety of the packet, advanced to the start of the payload.</param>
        /// <param name="sender">The <see cref="Peer"/> the packet came from.</param>
        /// <returns><see langword="true"/> if a packet was found; otherwise, <see langword="false"/>.</returns>
        bool GetNextPacket(out ByteReader reader, out Peer sender);

        /// <summary>
        /// Get the ping of a remote user.
        /// </summary>
        /// <param name="peer">The <see cref="Peer"/> to get the ping of.</param>
        /// <returns>The ping of the <see cref="Peer"/>.</returns>
        int GetPing(Peer peer);

    }
}
