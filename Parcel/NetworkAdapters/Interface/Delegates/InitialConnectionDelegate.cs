namespace Parcel.Networking
{
    /// <summary>
    /// A delegate defining the signature for initial connection events.
    /// </summary>
    /// <param name="peer">The <see cref="Peer"/> associated with the connection event.</param>
    /// <returns>An <see cref="InitialConnectionResult"/> struct.</returns>
    public delegate InitialConnectionResult InitialConnectionDelegate(Peer peer);
}
