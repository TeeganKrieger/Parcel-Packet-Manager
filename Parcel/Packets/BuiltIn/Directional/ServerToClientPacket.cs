namespace Parcel.Packets
{
    /// <summary>
    /// Packet that can only travel from a <see cref="ParcelServer"/> to a <see cref="ParcelClient"/>.
    /// </summary>
    public class ServerToClientPacket : Packet
    {
        /// <inheritdoc/>
        protected internal override bool CanSend()
        {
            return this.IsServer;
        }
    }
}
