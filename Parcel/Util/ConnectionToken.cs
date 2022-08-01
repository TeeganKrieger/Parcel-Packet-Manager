using System;

namespace Parcel
{
    /// <summary>
    /// Represents a token used for establishing connections.
    /// </summary>
    /// <remarks>
    /// The value set for the <see cref="Address"/> parameter will be dependent upon which <see cref="INetworkAdapter"> Network Adapter</see>
    /// you have chosen to use.
    /// </remarks>
    public sealed class ConnectionToken
    {
        private const string EXCP_PORT_RANGE = "The provided port {0} is not within the valid range of ports 1-65535!";

        /// <summary>
        /// The address of the connection.
        /// </summary>
        public string Address { get; private set; }

        /// <summary>
        /// The port of the connection.
        /// </summary>
        public int Port { get; private set; }


        #region CONSTRUCTOR

        /// <summary>
        /// Construct a new instance of ConnectionToken.
        /// </summary>
        /// <param name="address">The address to use.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="address"/> is <see langword="null"/>.</exception>
        public ConnectionToken(string address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            if (address.ToLower() == "localhost")
                address = "127.0.0.1";

            this.Address = address;
            this.Port = 0;
        }

        /// <summary>
        /// Construct a new instance of ConnectionToken.
        /// </summary>
        /// <param name="address">The address to use.</param>
        /// <param name="port">The port to use.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="address"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="port"/> is not within the valid port range. (1-65535)</exception>
        public ConnectionToken(string address, int port)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            if (port < 1 || port > ushort.MaxValue)
                throw new ArgumentException(string.Format(EXCP_PORT_RANGE, port));
            if (address.ToLower() == "localhost")
                address = "127.0.0.1";

            this.Address = address;
            this.Port = port;
        }

        #endregion


        #region OVERRIDES

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is ConnectionToken token &&
                   Address == token.Address &&
                   Port == token.Port;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(Address, Port);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Address}:{Port}";
        }
        #endregion

    }
}
