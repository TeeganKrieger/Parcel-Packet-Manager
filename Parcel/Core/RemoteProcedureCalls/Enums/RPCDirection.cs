
namespace Parcel.Networking
{
    /// <summary>
    /// Represents the direction in which a remote procedure call is allowed to execute.
    /// </summary>
    public enum RPCDirection
    {
        /// <summary>
        /// Indicates that a remote procedure call is only allowed to be called on a server and executed on a client.
        /// </summary>
        ServerToClient,
        /// <summary>
        /// Indicates that a remote procedure call is only allowed to be called on a client and executed on a server.
        /// </summary>
        ClientToServer,
        /// <summary>
        /// Indicates that a remote procedure call is allowed to be called on both a client or server and executed on both a client or server.
        /// </summary>
        BiDirectional
    }
}
