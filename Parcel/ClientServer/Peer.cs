using System.Collections;
using System.Collections.Generic;
using System.Text;
using Parcel.Serialization;

namespace Parcel
{
    /// <summary>
    /// Represents a local or remote user.
    /// </summary>
    /// <remarks>
    /// Peers are used for everything from establishing connections, specifying who to send a packet to, or for associating objects with a user.<br/>
    /// Note that Peers are always compared by their <see cref="Peer.GUID">GUID</see> and not by their other properties.<br/>
    /// Peers can optionally contain an <see cref="Peer.Address">Address</see> and a <see cref="Peer.Port">Port</see> depending on the chosen network adapter's implementation.<br/>
    /// Peers can also contain optional properties, which are <see cref="System.String">string</see>-<see cref="System.Object">object</see>
    /// KeyValuePairs. Properties cannot be changed once the Peer has been constructed.
    /// </remarks>
    [OptIn]
    public sealed class Peer : IEnumerable<KeyValuePair<string, object>>
    {
        private const string EXCP_KEY_NOT_FOUND = "Key not found. The peer does not contain a property with the key \"{0}\"";

        /// <summary>
        /// The unique identifying string of the Peer.
        /// </summary>
        [Serialize]
        public string GUID { get; private set; }

        /// <summary>
        /// The address of the Peer.
        /// </summary>
        /// <remarks>
        /// This property may be <see langword="null"/> depending upon the chosen network adapter's implementation.
        /// </remarks>
        [Ignore]
        public string Address { get; private set; }

        /// <summary>
        /// The port of the Peer.
        /// </summary>
        /// <remarks>
        /// This property's value may be <see langword="0"/> depending upon the chosen network adapter's implementation.
        /// </remarks>
        [Ignore]
        public int Port { get; private set; }

        private Dictionary<string, object> _properties;


        #region CONSTRUCTOR

        /// <summary>
        /// Default constructor for usage by <see cref="Parcel.Lib.Create">Create</see>.
        /// </summary>
        private Peer() { }

        /// <summary>
        /// Construct a new instance of Peer.
        /// </summary>
        /// <param name="guid">The guid of the peer.</param>
        /// <param name="address">The address of the peer.</param>
        /// <param name="port">The port of the peer.</param>
        /// <param name="properties">Additional properties of the peer.</param>
        /// <remarks>
        /// This constructor will exclusively be called by the <see cref="Parcel.PeerBuilder">PeerBuilder</see> utility.
        /// </remarks>
        internal Peer(string guid, string address, int port, Dictionary<string, object> properties)
        {
            this.GUID = guid;
            this.Address = address;
            this.Port = port;
            this._properties = properties;
        }

        #endregion


        #region METHODS

        /// <summary>
        /// Update's the <see cref="Peer.GUID">GUID</see> of this Peer.
        /// </summary>
        /// <param name="guid">The new GUID.</param>
        /// <remarks>
        /// Warning: This method was made public only for use by user-made network adapters. Calling this method improperly can cause unexpected
        /// behaviour.
        /// </remarks>
        public void UpdateGUID(string guid)
        {
            this.GUID = guid;
        }

        /// <summary>
        /// Get a <see cref="Parcel.ConnectionToken">ConnectionToken</see> representation the this Peer's <see cref="Peer.Address">Address</see> and <see cref="Peer.Port">Port</see>.
        /// </summary>
        /// <returns>A new <see cref="Parcel.ConnectionToken">ConnectionToken</see>.</returns>
        public ConnectionToken GetConnectionToken()
        {
            return new ConnectionToken(this.Address, this.Port);
        }

        #endregion


        #region PROPERTY DICTIONARY METHODS


        /// <summary>
        /// Get a collection of property keys from this Peer.
        /// </summary>
        /// <returns>A collection of property keys.</returns>
        public ICollection<string> GetPropertyKeys()
        {
            return _properties.Keys;
        }

        /// <summary>
        /// Get a collection of property values from this Peer.
        /// </summary>
        /// <returns>A collection of property values.</returns>
        public ICollection<object> GetPropertyValues()
        {
            return _properties.Values;
        }

        /// <summary>
        /// Get a property value using <paramref name="key"/> from this Peer.
        /// </summary>
        /// <param name="key">The key to get the value of.</param>
        /// <returns>The value of <paramref name="key"/>.</returns>
        public object this[string key]
        {
            get
            {
                if (!this._properties.ContainsKey(key))
                    throw new KeyNotFoundException(string.Format(EXCP_KEY_NOT_FOUND, key));
                return _properties[key];
            }
        }

        #endregion


        #region IENUMERABLE IMPLEMENTATION

        ///<inheritdoc/>
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _properties.GetEnumerator();
        }

        ///<inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _properties.GetEnumerator();
        }

        #endregion


        #region OVERRIDES

        ///<inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is Peer peer && GUID == peer.GUID;
        }

        ///<inheritdoc/>
        public override int GetHashCode()
        {
            return GUID.GetHashCode();//HashCode.Combine(GUID, Address, Port);
        }

        ///<inheritdoc/>
        public static bool operator ==(Peer left, Peer right)
        {
            return left.Equals(right);
        }

        ///<inheritdoc/>
        public static bool operator !=(Peer left, Peer right)
        {
            return !left.Equals(right);
        }

        ///<inheritdoc/>
        public override string ToString()
        {
            StringBuilder propertiesString = new StringBuilder();
            foreach (string key in this._properties.Keys)
                propertiesString.AppendFormat("{{{0}: {1}}} ", key, this._properties[key]);

            return $"{nameof(Peer)}(GUID={GUID}, IpAddress={Address}, Port={Port}, Properties={propertiesString})";
        }

        #endregion

    }
}
