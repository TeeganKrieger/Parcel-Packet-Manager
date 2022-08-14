namespace Parcel.Networking
{

    /// <summary>
    /// Represents the result of an attempt to connect to a remote user.
    /// </summary>
    public struct ConnectionResult
    {
        /// <summary>
        /// The status of the connection attempt.
        /// </summary>
        public ConnectionStatus Status { get; private set; }

        /// <summary>
        /// The remote <see cref="Peer"/> that the connection attempt was made with.
        /// </summary>
        public Peer RemotePeer { get; private set; }

        /// <summary>
        /// An object that can optionally be included with a rejection event.
        /// </summary>
        public object RejectionObject { get; private set; }

        /// <summary>
        /// Construct a new ConnectionResult struct.
        /// </summary>
        /// <param name="status">Whether the connection was successful or not.</param>
        /// <param name="remotePeer">The remote <see cref="Peer"/> that the connection attempt was made with.</param>
        /// <param name="rejectionObject">The rejection object to include.</param>
        public ConnectionResult(ConnectionStatus status, Peer remotePeer, object rejectionObject)
        {
            this.Status = status;
            this.RemotePeer = remotePeer;
            this.RejectionObject = rejectionObject;
        }
    }
}
