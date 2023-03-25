using Parcel.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parcel.Packets
{
    [OptIn]
    [Reliable]
    internal class RPCResultPacket : Packet
    {
        [Serialize]
        public int Handle { get; private set; }

        [Serialize]
        public object ReturnValue { get; private set; }

        [Serialize]
        public bool Success { get; private set; }

        public RPCResultPacket() { }

        public RPCResultPacket(int handle, object returnValue, bool success)
        {
            Handle = handle;
            ReturnValue = returnValue;
            Success = success;
        }

        protected internal override void OnReceive()
        {
            if (this.IsClient)
                this.Client.TryExecuteCallback(this.Handle, this.Success, this.ReturnValue);
            else if (this.IsServer)
                this.Server.TryExecuteCallback(this.Handle, this.Success, this.ReturnValue);
        }
    }
}
