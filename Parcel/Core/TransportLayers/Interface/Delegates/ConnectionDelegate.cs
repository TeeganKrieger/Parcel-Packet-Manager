namespace Parcel.Networking
{
    /// <summary>
    /// A delegate defining the signature for connection events.
    /// </summary>
    /// <param name="peer">The <see cref="Peer"/> associated with the connection event.</param>
    public delegate void ConnectionDelegate(Peer peer);
}
