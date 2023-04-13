using Parcel.Lib;
using Parcel.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Parcel.Networking
{
    public partial class UDPTransportLayerV2 : ITransportLayerV2
    {
        private Thread _transportThread;
        private UdpClient _udpClient;
        
        private ConcurrentDictionary<ConnectionToken, ConnectionChannel> _channels;
        private ConcurrentDictionary<CallbackHandle, TargetedDynamicDelegate> _callbacks;
        private ConcurrentQueue<InboundPacket> _inboundPackets;

        public event ConnectionAttemptEvent HandleRemotePeerConnectionAttempt;
        public event ConnectionEvent OnRemotePeerConnection;
        public event DisconnectionEvent OnRemotePeerDisconnection;

        public bool IsServer { get; private set; }
        public ParcelSettings Settings { get; private set; }
        public ConnectionToken Self { get; private set; }

        public UDPTransportLayerV2()
        {
            this._channels = new ConcurrentDictionary<ConnectionToken, ConnectionChannel>();
            this._callbacks = new ConcurrentDictionary<CallbackHandle, TargetedDynamicDelegate>();
            this._inboundPackets = new ConcurrentQueue<InboundPacket>();

            this._transportThread = new Thread(new ThreadStart(this.Loop));
            this._transportThread.Name = "UDP Transport Layer Thread";
            this._transportThread.Start();
        }

        public void Initialize(bool isServer, ParcelSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            this.IsServer = isServer;
            this.Settings = settings;
            this.Self = settings.Peer.GetConnectionToken();
            this._udpClient = new UdpClient(settings.Peer.Port);
        }

        public void Connect(ConnectionToken connectionToken, object connectionData = null, ConnectionCallback callback = null)
        {
            throw new NotImplementedException();
        }

        public void Disconnect(ConnectionToken connectionToken, object disconnectionData = null, DisconnectionCallback callback = null)
        {
            throw new NotImplementedException();
        }

        public bool GetNextInboundPacket(out byte[] data, out ConnectionToken sender)
        {
            bool hasPacket = this._inboundPackets.TryDequeue(out InboundPacket inboundPacket);
            data = hasPacket ? inboundPacket.Bytes : null;
            sender = hasPacket ? inboundPacket.Sender : null;
            return hasPacket;
        }

        public int GetPing(ConnectionToken remote)
        {
            throw new NotImplementedException();
        }

        public void SendReliablePacket(ConnectionToken sendTo, byte[] data, PacketAcknowledgedCallback callback = null)
        {
            if (!this._channels.TryGetValue(sendTo, out ConnectionChannel channel))
            {
                throw new ArgumentException("Blah Blah Blah");
            }
            else
            {
                CallbackHandle callbackHandle = this.StoreCallback(callback);
                channel.SendReliablePacket(data, callbackHandle);
            }
        }

        public void SendUnreliablePacket(ConnectionToken sendTo, byte[] data)
        {
            if (!this._channels.TryGetValue(sendTo, out ConnectionChannel channel))
            {
                throw new ArgumentException("Blah Blah Blah");
            }
            else
            {
                channel.SendUnreliablePacket(data);
            }
        }


        #region LOOP

        private void Loop()
        {
            
        }

        #endregion


        #region CALLBACKS


        private CallbackHandle StoreCallback(ConnectionCallback connectionCallback)
        {
            CallbackHandle callbackHandle = new CallbackHandle();
            if (this._callbacks.TryAdd(callbackHandle, new Action<bool, Peer, Peer, object>(connectionCallback).Bind()))
                return callbackHandle;
            else
                return default(CallbackHandle);
        }

        private CallbackHandle StoreCallback(DisconnectionCallback disconnectionCallback)
        {
            CallbackHandle callbackHandle = new CallbackHandle();
            if (this._callbacks.TryAdd(callbackHandle, new Action<bool, object>(disconnectionCallback).Bind()))
                return callbackHandle;
            else
                return default(CallbackHandle);
        }

        private CallbackHandle StoreCallback(PacketAcknowledgedCallback packetAcknowledgedCallback)
        {
            CallbackHandle callbackHandle = new CallbackHandle();
            if (this._callbacks.TryAdd(callbackHandle, new Action(packetAcknowledgedCallback).Bind()))
                return callbackHandle;
            else
                return default(CallbackHandle);
        }

        private bool ExecuteAndDestroyConnectionCallback(CallbackHandle callbackHandle, bool success, Peer self, Peer remote, object rejectionData)
        {
            if (this._callbacks.TryRemove(callbackHandle, out TargetedDynamicDelegate tdd))
            {
                tdd.Invoke(new object[] { success, self, remote, rejectionData });
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool ExecuteAndDestroyDisconnectionCallback(CallbackHandle callbackHandle, bool success, object disconnectionData)
        {
            if (this._callbacks.TryRemove(callbackHandle, out TargetedDynamicDelegate tdd))
            {
                tdd.Invoke(new object[] { success, disconnectionData });
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool ExecuteAndDestroyPacketAcknowledgedCallback(CallbackHandle callbackHandle)
        {
            if (this._callbacks.TryRemove(callbackHandle, out TargetedDynamicDelegate tdd))
            {
                tdd.Invoke(new object[0]);
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion


        #region NESTED CLASSES

        private class InboundPacket
        {
            public byte[] Bytes { get; private set; }
            public ConnectionToken Sender { get; private set; }

            public InboundPacket(byte[] bytes, ConnectionToken sender)
            {
                this.Bytes = bytes;
                this.Sender = sender;
            }
        }

        private struct CallbackHandle
        {
            private static int NextID = 0;

            public int Value { get; private set; }

            public CallbackHandle()
            {
                this.Value = Interlocked.Increment(ref NextID);
            }
        }

        #endregion


    }
}
