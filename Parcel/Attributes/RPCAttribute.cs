using Parcel.Networking;
using System;

namespace Parcel.Packets
{
    /// <summary>
    /// Specifies that a static method or instance method within a <see cref="SyncedObject">SyncedObject</see> is allowed 
    /// to be called remotely.
    /// </summary>
    /// <remarks>
    /// Under normal circumstances, a SyncedObject's properties will only serialize if they have changed. This attribute
    /// allows for a property to always be included in serialization, regardless of whether it changed or not.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class RPCAttribute : Attribute
    {
        /// <summary>
        /// The direction in which the remote procedure call this attribute is decorating is allowed to execute.
        /// </summary>
        public RPCDirection Direction { get; private set; }

        /// <summary>
        /// Construct a new instance of RPCAttribute.
        /// </summary>
        /// <param name="direction">The direction in which the remote procedure call this attribute is decorating is allowed to execute.</param>
        public RPCAttribute(RPCDirection direction)
        {
            Direction = direction;
        }
    }
}
