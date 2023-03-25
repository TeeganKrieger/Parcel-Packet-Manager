using Parcel.Networking;
using Parcel.Serialization;
using System;
using System.Reflection;

namespace Parcel.Packets
{
    [OptIn]
    [Reliable]
    internal class RPCCallPacket : Packet
    {

        [Serialize]
        public ProcedureHashCode HashCode { get; private set; }

        [Serialize]
        public SyncedObjectID SyncedObjectReference { get; private set; }

        [Serialize]
        public int Handle { get; private set; }

        [Serialize]
        public object[] Parameters { get; private set; }

        public RPCCallPacket() { }

        public RPCCallPacket(ProcedureHashCode hashCode, SyncedObjectID syncedObjectReference, int handle, object[] parameters)
        {
            HashCode = hashCode;
            SyncedObjectReference = syncedObjectReference;
            Handle = handle;
            Parameters = parameters;
        }

        protected internal override void OnReceive()
        {
            MethodInfo procedureMethodInfo = ProcedureHashCode.ParseMethodInfo(this.HashCode);
            ObjectCache objectCache = ObjectCache.FromType(procedureMethodInfo.DeclaringType);
            ObjectProcedure objectProcedure = objectCache.GetProcedure((ulong)this.HashCode);

            if ((this.IsClient && PacketCacheHelper.GetRPCDirection(objectProcedure) == RPCDirection.ClientToServer)
                || (this.IsServer && PacketCacheHelper.GetRPCDirection(objectProcedure) == RPCDirection.ServerToClient))
                return;

            //Setup RPC Context
            RPCContext.Sender = this.Sender;

            //Execute the RPC
            object returnValue;
            bool executedSuccessfully = false;
            if (objectProcedure.IsStatic)
            {
                try
                {
                    returnValue = objectProcedure.Delegate.Invoke(null, Parameters);
                    executedSuccessfully = true;
                }
                catch (Exception ex)
                {
                    returnValue = null;
                    executedSuccessfully = false;
                }
            }
            else
            {
                if ((this.IsClient && this.Client.TryGetSyncedObject(SyncedObjectReference, out SyncedObject syncedObject))
                    || (this.IsServer && this.Server.TryGetSyncedObject(SyncedObjectReference, out syncedObject)))
                {
                    try
                    {
                        returnValue = objectProcedure.Delegate.Invoke(syncedObject, Parameters);
                        executedSuccessfully = true;
                    }
                    catch (Exception ex)
                    {
                        returnValue = null;
                        executedSuccessfully = false;
                    }
                }
                else
                {
                    returnValue = null;
                    executedSuccessfully = false;
                }
            }

            //Cleanup RPC Context
            RPCContext.Sender = null;

            RPCResultPacket resultPacket = new RPCResultPacket(Handle, returnValue, executedSuccessfully);

            Console.WriteLine($"Sending results packet with handle: {this.Handle}, returnValue: {returnValue}, success: {executedSuccessfully}");

            //Send RPC Results Packet
            if (this.IsClient)
                this.Client.Send(resultPacket);
            else if (this.IsServer)
                this.Server.Send(resultPacket, Sender);
        }
    }
}
