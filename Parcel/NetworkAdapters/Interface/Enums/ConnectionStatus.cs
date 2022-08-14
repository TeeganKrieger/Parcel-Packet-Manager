namespace Parcel.Networking
{
    /// <summary>
    /// Represents the status of a <see cref="ConnectionResult"/> object.
    /// </summary>
    public enum ConnectionStatus
    {
        /// <summary>
        /// Indicates that the connection failed due to a time out.
        /// </summary>
        Timeout,
        /// <summary>
        /// Indicates that a connection failed due to being rejected.
        /// </summary>
        Rejected,
        /// <summary>
        /// Indicates that a connection failed due to an error occurring.
        /// </summary>
        Error,
        /// <summary>
        /// Indicates that a connection succeeded.
        /// </summary>
        Success
    }
}
