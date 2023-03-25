namespace Parcel.Networking
{
    /// <summary>
    /// Represents the results of a InitialConnectionEvent.
    /// </summary>
    /// <remarks>
    /// Return this struct within a server's Before Connection Event to allow or reject a connection. 
    /// Additionally a rejection object can included.
    /// </remarks>
    public struct InitialConnectionResult
    {
        /// <summary>
        /// Whether the connection is allowed or not.
        /// </summary>
        public bool AllowConnection { get; private set; }

        /// <summary>
        /// The rejection object to be sent to a client.
        /// </summary>
        public object RejectionObject { get; private set; }

        /// <summary>
        /// Construct a new InitialConnectionResult struct.
        /// </summary>
        /// <param name="allowConnection">Whether the connection is allowed or not.</param>
        /// <param name="rejectionObject">The rejection object to be sent to a client.</param>
        public InitialConnectionResult(bool allowConnection, string rejectionObject = null)
        {
            this.AllowConnection = allowConnection;
            this.RejectionObject = rejectionObject;
        }
    }
}
