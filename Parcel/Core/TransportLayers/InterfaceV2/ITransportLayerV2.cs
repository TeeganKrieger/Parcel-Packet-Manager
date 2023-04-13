
namespace Parcel.Networking
{
    public interface ITransportLayerV2
    {
        event ConnectionAttemptEvent HandleRemotePeerConnectionAttempt;
        event ConnectionEvent OnRemotePeerConnection;
        event DisconnectionEvent OnRemotePeerDisconnection;

        void Initialize(bool isServer, ParcelSettings settings);
        void Connect(ConnectionToken connectionToken, object connectionData = null, ConnectionCallback callback = null);
        void Disconnect(ConnectionToken connectionToken, object disconnectionData = null, DisconnectionCallback callback = null);
        void SendReliablePacket(ConnectionToken sendTo, byte[] data, PacketAcknowledgedCallback callback = null);
        void SendUnreliablePacket(ConnectionToken sendTo, byte[] data);
        bool GetNextInboundPacket(out byte[] data, out ConnectionToken sender);
        int GetPing(ConnectionToken remote);
    }
}
