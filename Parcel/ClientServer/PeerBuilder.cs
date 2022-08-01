using System;
using System.Collections.Generic;

namespace Parcel
{
    /// <summary>
    /// Facilitates construction of a new <see cref="Peer"/> instance.
    /// </summary>
    /// <remarks>
    /// The PeerBuilder class helps ensure proper construction of a <see cref="Peer"/> instance.
    /// </remarks>
    /// <example>
    /// Construct a new <see cref="Peer"/> using the PeerBuilder.
    /// <code>
    /// Peer examplePeer = new PeerBuilder().SetAddress("localhost").SetPort(8181)
    /// .AddProperty("name", "myUsername").AddProperty("class", CharacterClass.Mage);
    /// </code>
    /// </example>
    public sealed class PeerBuilder
    {
        private const string EXCP_PORT_RANGE = "The provided port {0} is not within the valid range of ports 1-65535!";
        private const string EXCP_UNSP_ADDR = "Could not create Peer because no address was specified! Please specify an address using SetAddress().";
        private const string EXCP_UNSP_PORT = "Could not create Peer because no port was specified! Please specify an address using SetPort().";

        private string _guid;
        private string _address;
        private int _port;
        private Dictionary<string, object> _properties;


        #region CONSTRUCTOR

        /// <summary>
        /// Construct a new instance of PeerBuilder.
        /// </summary>
        public PeerBuilder()
        {
            this._guid = Guid.NewGuid().ToString();
            this._address = null;
            this._port = -1;
            this._properties = new Dictionary<string, object>();
        }

        #endregion


        #region SETTINGS

        /// <summary>
        /// Set the <see cref="Peer.GUID">GUID</see> of the <see cref="Peer"/> being constructed.
        /// </summary>
        /// <param name="guid">The GUID to use.</param>
        /// <returns>The current PeerBuilder instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="guid"/> is <see langword="null"/>.</exception>
        public PeerBuilder SetGUID(string guid)
        {
            if (guid == null)
                throw new ArgumentNullException(nameof(guid));

            this._guid = guid;
            return this;
        }

        /// <summary>
        /// Set the Address of the <see cref="Peer"/> being constructed to the current network's public IP address.
        /// </summary>
        /// <returns>The current PeerBuilder instance.</returns>
        public PeerBuilder UsePublicAddress()
        {
            this._address = GetPublicIP();
            return this;
        }

        /// <summary>
        /// Set the Address of the <see cref="Peer"/> being constructed.
        /// </summary>
        /// <param name="address">The Address to use.</param>
        /// <returns>The current PeerBuilder instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="address"/> is <see langword="null"/>.</exception>
        public PeerBuilder SetAddress(string address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            if (address.ToLower() == "localhost")
                address = "127.0.0.1";
            this._address = address;
            return this;
        }

        /// <summary>
        /// Set the Port of the <see cref="Peer"/> being constructed.
        /// </summary>
        /// <param name="port">The Port to use.</param>
        /// <returns>The current PeerBuilder instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="port"/> is not within the valid range of ports (1-65535).</exception>
        public PeerBuilder SetPort(int port)
        {
            if (port < 1 || port > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(port), string.Format(EXCP_PORT_RANGE, port));

            this._port = port;
            return this;
        }

        /// <summary>
        /// Add a property to the <see cref="Peer"/> bring constructed.
        /// </summary>
        /// <param name="key">The key of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <returns>The current PeerBuilder instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="key"/> is <see langword="null"/>.</exception>
        public PeerBuilder AddProperty(string key, object value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            this._properties.Add(key, value);
            return this;
        }

        /// <summary>
        /// Set the properties of the <see cref="Peer"/> being constructed.
        /// </summary>
        /// <param name="properties">The properties to use.</param>
        /// <returns>The current PeerBuilder instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="properties"/> is <see langword="null"/>.</exception>
        public PeerBuilder SetProperties(Dictionary<string, object> properties)
        {
            if (this._properties == null)
                throw new ArgumentNullException(nameof(properties));
            this._properties = properties;
            return this;
        }

        #endregion


        #region OPERATORS

        /// <summary>
        /// Implicitly convert a <see cref="PeerBuilder"/> instance to a <see cref="Peer"/> instance.
        /// </summary>
        /// <param name="builder">The PeerBuilder to convert.</param>
        public static implicit operator Peer(PeerBuilder builder)
        {
            if (builder._address == null)
                throw new InvalidOperationException(EXCP_UNSP_ADDR);
            if (builder._port == -1)
                throw new InvalidOperationException(EXCP_UNSP_PORT);

            return new Peer(builder._guid, builder._address, builder._port, builder._properties);
        }

        #endregion


        #region HELPERS

        /// <summary>
        /// Get the current network's public IP address.
        /// </summary>
        /// <returns>The current network's public IP address.</returns>
        /// Snippet provided by r.zarei on StackOverflow/
        /// https://stackoverflow.com/a/16109156
        private static string GetPublicIP()
        {
            string url = "http://checkip.dyndns.org";
            System.Net.WebRequest req = System.Net.WebRequest.Create(url);
            System.Net.WebResponse resp = req.GetResponse();
            System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());
            string response = sr.ReadToEnd().Trim();
            string[] a = response.Split(':');
            string a2 = a[1].Substring(1);
            string[] a3 = a2.Split('<');
            string a4 = a3[0];
            return a4;
        }

        #endregion
    }
}
