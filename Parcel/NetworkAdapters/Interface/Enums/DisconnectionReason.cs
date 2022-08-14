namespace Parcel.Networking
{
    /// <summary>
    /// Represents the reason for a disconnection.
    /// </summary>
    public enum DisconnectionReason
    {
        /// <summary>
        /// Indicates that the disconnection was manual.
        /// </summary>
        Manual,
        /// <summary>
        /// Indicates that the disconnection was forced.
        /// </summary>
        Forced,
        /// <summary>
        /// Indicates that the disconnection was caused by a time out.
        /// </summary>
        Timeout
    }
}
