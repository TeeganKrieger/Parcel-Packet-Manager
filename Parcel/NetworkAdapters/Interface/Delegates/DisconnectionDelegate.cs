namespace Parcel.Networking
{
    /// <summary>
    /// A delegate defining the signature for disconnection events.
    /// </summary>
    /// <param name="peer">The <see cref="Peer"/> associated with the connection event.</param>
    /// <param name="reason">The reason for the disconnection.</param>
    /// <param name="disconnectionObject">The object sent by the server.</param>
    public delegate void DisconnectionDelegate(Peer peer, DisconnectionReason reason, object disconnectionObject);
}
