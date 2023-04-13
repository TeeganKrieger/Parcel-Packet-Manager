
namespace Parcel.Networking
{
    public delegate void ConnectionCallback(bool success, Peer self, Peer remote, object rejectionData);
}