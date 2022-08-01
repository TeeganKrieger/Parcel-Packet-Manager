namespace Parcel.Packets
{
    /// <summary>
    /// Packet that can only travel from a <see cref="ParcelClient"/> to a <see cref="ParcelServer"/>.
    /// </summary>
    public class ClientToServerPacket : Packet
    {
        /// <inheritdoc/>
        protected internal override bool CanSend()
        {
            return !this.IsServer;
        }
    }
}
