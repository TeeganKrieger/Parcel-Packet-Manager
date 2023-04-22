using Parcel.Networking;
using Parcel.Packets;
using Parcel.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parcel
{
    /// <summary>
    /// Represents a <see cref="DataWriter"/> and packet count.
    /// </summary>
    internal class WriterAndCount<T>
    {
        private ParcelClient _client;

        /// <summary>
        /// The <see cref="DataWriter"/>.
        /// </summary>
        public DataWriter Writer { get; private set; }

        /// <summary>
        /// The packet count.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Construct a new instance of WriterAndCount.
        /// </summary>
        /// <param name="writer">The <see cref="DataWriter"/> to use.</param>
        public WriterAndCount(ParcelClient client)
        {
            this._client = client;
            this.Writer = client.NetworkSettings.SerializerResolver.NewDataWriter();
            this.Count = 0;
        }

        public void AddPacket(Packet packet)
        {

        }

        /// <summary>
        /// Reset the <see cref="Writer"/> and <see cref="Count"/>.
        /// </summary>
        public void Reset()
        {
            this.Count = 0;
            this.Writer.Reset();
        }

        void SerializePacket(Packet packet, WriterAndCount wac)
        {
            int restorePosition = wac.Writer.Position;

            try
            {
                wac.Writer.Write((byte)1); //Hint

                int skipPosition = wac.Writer.Position;
                wac.Writer.Write(0); //Skip Distance

                lock (packet)
                    wac.Writer.Write(packet);

                wac.Writer.Write(wac.Writer.Position - skipPosition, skipPosition);
                wac.Count++;
                this.NetworkSettings.Debugger?.AddSerializedPacketEvent();
            }
            catch (Exception ex)
            {
                wac.Writer.SetPosition(restorePosition);
                this.NetworkSettings.Debugger?.AddExceptionEvent(ex);
            }
        }

        void SerializeSyncedObject(SyncedObject so, Reliability reliability, WriterAndCount wac)
        {
            if (this._syncedObjectSerializer.WillSerialize(so, reliability))
            {
                int restorePosition = wac.Writer.Position;
                try
                {
                    wac.Writer.Write((byte)2); //Hint

                    int skipPosition = wac.Writer.Position;
                    wac.Writer.Write(0); //Skip Distance

                    lock (so)
                        this._syncedObjectSerializer.Serialize(wac.Writer, so, reliability);

                    wac.Writer.Write(wac.Writer.Position - skipPosition, skipPosition);
                    wac.Count++;
                    this.NetworkSettings.Debugger?.AddSerializedPacketEvent();
                }
                catch (Exception ex)
                {
                    wac.Writer.SetPosition(restorePosition);
                    this.NetworkSettings.Debugger?.AddExceptionEvent(ex);
                }
            }
        }

    }
}
