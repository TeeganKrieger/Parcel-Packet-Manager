using Parcel.Serialization;

namespace Parcel.Packets
{
    /// <summary>
    /// Base class for all Packets.
    /// </summary>
    public abstract class Packet
    {
        /// <summary>
        /// The <see cref="Peer"/> who sent this Packet.
        /// </summary>
        [Ignore]
        [DontPatch]
        public Peer Sender { get; set; }

        /// <summary>
        /// Whether this Packet is a server instance or not.
        /// </summary>
        [Ignore]
        [DontPatch]
        protected internal bool IsServer { get; internal set; }

        /// <summary>
        /// Whether this Packet is client instance of not.
        /// </summary>
        [Ignore]
        [DontPatch]
        protected internal bool IsClient => !IsServer;

        /// <summary>
        /// The client associated to this Packet; null if this Packet is a server instance.
        /// </summary>
        [Ignore]
        [DontPatch]
        protected internal ParcelClient Client { get; internal set; }

        /// <summary>
        /// The server associated to this Packet; null if this Packet is a client instance.
        /// </summary>
        [Ignore]
        [DontPatch]
        protected internal ParcelServer Server { get; internal set; }


        #region VIRTUAL METHODS

        /// <summary>
        /// Override this method to implement logic for determining if this Packet can be sent.
        /// </summary>
        /// <remarks>
        /// This can be used to validate parameters before sending or any other logic.
        /// </remarks>
        /// <returns><see langword="true"/> if the packet should be sent; otherwise <see langword="false"/>.</returns>
        protected internal virtual bool CanSend()
        {
            return true;
        }


        /// <summary>
        /// Override this method to perform logic shortly before this Packet is serialized and sent.
        /// </summary>
        protected internal virtual void OnSend()
        {

        }

        /// <summary>
        /// Override this method to perform logic shortly after this Packet is deserialized and received.
        /// </summary>
        protected internal virtual void OnReceive()
        {

        }

        #endregion

    }
}
