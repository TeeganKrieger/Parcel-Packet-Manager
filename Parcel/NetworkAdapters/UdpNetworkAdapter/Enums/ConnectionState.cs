namespace Parcel.Networking
{
    public sealed partial class UdpNetworkAdapter
    {
        /// <summary>
        /// Represents the state of a connection with a remote user.
        /// </summary>
        private enum ConnectionState
        {
            /// <summary>
            /// Indicates that a remote user is disconnected.
            /// </summary>
            Disconnected,
            /// <summary>
            /// Indicates that a remote user is in the process of connecting.
            /// </summary>
            Connecting,
            /// <summary>
            /// Indicates that a remote user is connected.
            /// </summary>
            Connected
        }
    }
}
