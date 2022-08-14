using Parcel.DataStructures;
using Parcel.Networking;
using Parcel.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Parcel.Packets
{
    
    /// <summary>
    /// Base class for SyncedObjects.
    /// </summary>
    /// <remarks>
    /// SyncedObjects allow for synchronization of the object's properties across multiple clients.
    /// </remarks>
    public abstract class SyncedObject : Packet
    {
        private static string EXCP_SERVER_ONLY_OP = "This operation can only be called on server instances of this SyncedObject.";
        private static string EXCP_NO_SUBSCRIBERS = "Failed to perform operation because no subscribers were provided. Include at least 1 subscriber.";

        /// <summary>
        /// Set containing reliable properties that need to be synchronized when next sent.
        /// </summary>
        [Ignore]
        [DontPatch]
        internal ConcurrentHashSet<uint> ReliablePropertiesToSync { get; private set; } = new ConcurrentHashSet<uint>();

        /// <summary>
        /// Set containing unreliable properties that need to be synchronized when next sent.
        /// </summary>
        [Ignore]
        [DontPatch]
        internal ConcurrentHashSet<uint> UnreliablePropertiesToSync { get; private set; } = new ConcurrentHashSet<uint>();

        /// <summary>
        /// Whether the SyncedObject should track changes for synchronization or not.
        /// </summary>
        [Ignore]
        [DontPatch]
        internal bool DontSync { get; set; }

        /// <summary>
        /// The <see cref="SyncedObjectID">ID</see> of this SyncedObject.
        /// </summary>
        [Serialize]
        [DontPatch]
        public SyncedObjectID ID { get; internal set; }

        /// <summary>
        /// The <see cref="Peer"/> who owns this <see cref="SyncedObject"/>.
        /// </summary>
        [Serialize]
        [DontPatch]
        public Peer Owner { get; internal set; }


        #region SHORTCUT METHODS

        /// <summary>
        /// Transfer the ownership of this SyncedObject to <paramref name="newOwner"/>.
        /// </summary>
        /// <param name="newOwner">The <see cref="Peer"/> who will now own this SyncedObject.</param>
        /// <exception cref="InvalidOperationException">Thrown if this method is called on a client instance of the SyncedObject.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="newOwner"/> is <see langword="null"/>.</exception>
        public void TransferOwnership(Peer newOwner)
        {
            if (!this.IsServer)
                throw new InvalidOperationException(EXCP_SERVER_ONLY_OP);
            if (newOwner == null)
                throw new ArgumentNullException(nameof(newOwner));
            this.Server.TryTransferSyncedObjectOwnership(this.ID, newOwner);
        }

        /// <summary>
        /// Add <see cref="Peer">Subscribers</see> to this SyncedObject.
        /// </summary>
        /// <param name="subscribers">The <see cref="Peer">Peers</see> to subscribe.</param>
        /// <exception cref="InvalidOperationException">Thrown if this method is called on a client instance of the SyncedObject.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="subscribers"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="subscribers"/> is empty.</exception>
        public void AddSubscriptions(params Peer[] subscribers)
        {
            if (!this.IsServer)
                throw new InvalidOperationException(EXCP_SERVER_ONLY_OP);
            if (subscribers == null)
                throw new ArgumentNullException(nameof(subscribers));
            if (subscribers.Length == 0)
                throw new ArgumentException(EXCP_NO_SUBSCRIBERS, nameof(subscribers));

            this.Server.AddSyncedObjectSubscriptions(this.ID, subscribers);
        }

        /// <summary>
        /// Get an array of <see cref="Peer">Subscribers</see> to this SyncedObject.
        /// </summary>
        /// <returns>An array of <see cref="Peer">Peers</see>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if this method is called on a client instance of the SyncedObject.</exception>
        public Peer[] GetSubscriptions()
        {
            if (!this.IsServer)
                throw new InvalidOperationException(EXCP_SERVER_ONLY_OP);
            this.Server.TryGetSyncedObjectSubscribers(this.ID, out Peer[] subscribers);
            return subscribers;
        }

        /// <summary>
        /// Remove <see cref="Peer">Subscribers</see> from this SyncedObject.
        /// </summary>
        /// <param name="subscribers">The <see cref="Peer">Peers</see> to unsubscribe.</param>
        /// <exception cref="InvalidOperationException">Thrown if this method is called on a client instance of the SyncedObject.</exception>
        /// e<exception cref="ArgumentNullException">Thrown if <paramref name="subscribers"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="subscribers"/> is empty.</exception>
        public void RemoveSubscriptions(params Peer[] subscribers)
        {
            if (!this.IsServer)
                throw new InvalidOperationException(EXCP_SERVER_ONLY_OP);
            if (subscribers == null)
                throw new ArgumentNullException(nameof(subscribers));
            if (subscribers.Length == 0)
                throw new ArgumentException(EXCP_NO_SUBSCRIBERS, nameof(subscribers));

            this.Server.RemoveSyncedObjectSubscriptions(this.ID, subscribers);
        }

        #endregion


        #region PROPERTY SYNCHRONIZATION

        /// <summary>
        /// Method used by <see cref="SyncedObjectPatcher"/> to automate synchronization of properties.
        /// </summary>
        /// <param name="__instance">The instance of SyncedObject whose property changed.</param>
        /// <param name="__originalMethod">The MethodBase of the property setter.</param>
        private static void SyncProperty(SyncedObject __instance, MethodBase __originalMethod)
        {
            if (__instance.DontSync)
                return;
            if ((__instance.IsServer && __instance.Server == null) || (!__instance.IsServer && __instance.Client == null))
                return;

            string propertyName = __originalMethod.Name.Substring(4); //Remove get_ or set_ from method name to get property name
            uint nameHash = PropertyInfoHashCode.FromString(propertyName);

            ObjectCache cache = ObjectCache.FromType(__instance.GetType());
            ObjectProperty property = cache[nameHash];

            if (property != null)
            {
                if (!property.WillAlwaysSerialize())
                {
                    if (property.GetReliability() == Reliability.Reliable)
                        __instance.ReliablePropertiesToSync.TryAdd(nameHash);
                    else
                        __instance.UnreliablePropertiesToSync.TryAdd(nameHash);
                }

                if (__instance.IsServer)
                    __instance.Server.Send(__instance);
                else
                    __instance.Client.Send(__instance);
            }
        }

        /// <summary>
        /// Call this method when a properties value changes to mark it for synchronization when this SyncedObject is next sent.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        /// <remarks>
        /// This method provides a manual way of tracking changes made to a SyncedObject. It can also be used for tracking changes to complex objects
        /// such as Dictionaries where the property setter isn't necessarily being called. <br/>
        /// The <paramref name="propertyName"/> parameter is optional and will be auto-filled by the compiler if not included.
        /// </remarks>
        /// <example>
        /// Example of usage in a setter:
        /// <code>
        /// private string _myProp;
        /// public string MyProp
        /// {
        ///     get => this._myProp;
        ///     
        ///     set
        ///     {
        ///         this._myProp = value;
        ///         SyncProperty();
        ///     }
        /// }
        /// </code>
        /// <br/>
        /// Example of usage for complex data structures:
        /// <code>
        /// public Dictionary&lt;string, object&gt; MyDict { get; set; }
        /// 
        /// public void AddEntry(string key, object o)
        /// {
        ///     this.MyDict.Add(key, o);
        ///     SyncProperty(nameof(this.MyDict));
        /// }
        /// </code>
        /// </example>
        protected void SyncProperty([CallerMemberName] string propertyName = null)
        {
            if (this.DontSync)
                return;
            if ((IsServer && Server == null) || (!IsServer && Client == null))
                return;

            uint nameHash = PropertyInfoHashCode.FromString(propertyName);

            ObjectCache cache = ObjectCache.FromType(this.GetType());
            ObjectProperty property = cache[nameHash];

            if (property != null)
            {
                if (!property.WillAlwaysSerialize())
                {
                    if (property.GetReliability() == Reliability.Reliable)
                        this.ReliablePropertiesToSync.TryAdd(nameHash);
                    else
                        this.UnreliablePropertiesToSync.TryAdd(nameHash);
                }

                if (this.IsServer)
                    this.Server.Send(this);
                else
                    this.Client.Send(this);
            }
        }

        #endregion


        #region VIRTUAL METHODS

        /// <summary>
        /// Override this method to perform logic for when this SyncedObject is first created on a client or server.
        /// </summary>
        protected internal virtual void OnCreated()
        {

        }

        /// <summary>
        /// Override this method to perform logic for when this SyncedObject is destroyed on a client or server.
        /// </summary>
        protected internal virtual void OnDestroyed()
        {

        }

        /// <summary>
        /// Override this method to perform logic when this SyncedObject's owner changes.
        /// </summary>
        /// <param name="previousOwner">The <see cref="Peer"/> who previously owned this SyncedObject.</param>
        /// <param name="newOwner">The <see cref="Peer"/> who now owns this SyncedObject.</param>
        protected internal virtual void OnOwnerChange(Peer previousOwner, Peer newOwner)
        {

        }

        /// <summary>
        /// Override this method to perform logic when any properties change.
        /// </summary>
        /// <param name="changes">A Dictionary of property changes, where the key is the name of the property.</param>
        protected internal virtual void OnPropertiesChanged(Dictionary<string, PropertyChanges> changes)
        {

        }

        #endregion


        #region OVERRIDES

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is SyncedObject so && so.ID == this.ID;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.ID.GetHashCode();
        }

        #endregion


        #region NESTED STRUCTS

        /// <summary>
        /// Represents changes made to a property.
        /// </summary>
        protected internal struct PropertyChanges
        {
            /// <summary>
            /// The previous value of the property.
            /// </summary>
            public object Previous { get; private set; }

            /// <summary>
            /// The current value of the property.
            /// </summary>
            public object Current { get; private set; }

            /// <summary>
            /// Construct a new PropertyChanges struct.
            /// </summary>
            /// <param name="previous">The previous value of the property.</param>
            /// <param name="current">The current value of the property.</param>
            public PropertyChanges(object previous, object current)
            {
                this.Previous = previous;
                this.Current = current;
            }
        }

        #endregion

    }
}
