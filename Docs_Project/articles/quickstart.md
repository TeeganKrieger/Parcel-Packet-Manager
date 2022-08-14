<style>
    h1.title {
        text-align: center;
        font-size: 3rem;
    }
    h3 {
        font-size: 1.5rem;
    }
</style>

<h1 class="title">Getting Started</h1>

<h2>Installation</h2>

Coming soon...

<h2>Creating a Peer</h2>

Before initializing a client or server, a Peer object must be created. Peers contain a GUID, which is typically assigned by the server and used to uniquely identify Peers, even if they originate from the same network. Peers also hold onto the IP address and the Port of a user, as well as any read-only information (called properties) you decide to include with the peer. Examples of this might be a person's username.

If you desire to have non-read-only properties for a Peer, consider creating a SyncedObject owned by that Peer that is specific to your use-case.

Peers are constructed using the PeerBuilder class. The PeerBuilder class has the following options:

* UsePublicAddress() - Set the address of the Peer being built to your network's public IP address.
* SetAddress(string address) - Set the address of the Peer being built.
* SetPort(int port) - Set the port of the Peer being built.
* AddProperty(string key, object value) - Add a property to the Peer being built.
* SetProperties(Dictionary<string, object> properties) - Set all the properties of the Peer being built. 

NOTE: If you are hosting both a client and server within the same application, you will need to create two Peers and ensure their ports are not the same.

```cs
Peer clientPeer = new PeerBuilder()
.UsePublicAddress()
.SetPort(7777)
.AddProperty("username", "MyUsername"); 

Peer serverPeer = new PeerBuilder()
.UsePublicAddress()
.SetPort(7778);
```
<h2>Settings</h2>

After creating a Peer, the final step before creating a client or server is to create a ParcelSettings object. The ParcelSettings object holds settings to be utilized by a client or server. ParcelSettings objects can only be used by a single client or server, and will be bound to that instance, so if you are hosting a server and a client on the same application, you will need to create two ParcelSettings objects.

ParcelSettings are constructed using the ParcelSettingsBuilder class. The ParcelSettingsBuilder class has the following options:

* SetPeer(Peer peer) - Set the Peer for the client or server to use.
* SetNetworkAdapter<NetworkAdapterType>() - Set the network adapter for the client or server to use.
* SetConnectionTimeout(int milliseconds) - Set the amount of time allowed for connection events to occur within. If time is exceeded, a connection event fails.
* SetDisconnectionTimeout(int milliseconds) - Set the amount of time allowed since last receiving a packet from a user before they are considered disconnected.
* SetUpdatesPerSecond(int updates) - Set the number of iterations of a client or server's main loop that will be performed per second.
* PerformUpdatesAutomatically(bool should) - Set whether a client or server should automatically or manually perform updates.
* SetUnreliablePacketGroupSize(int groupSize) - Set the number unreliable packets that will be grouped together during serialization.
* SetReliablePacketGroupSize(int groupSize) - Set the number of reliable packets that will be grouped together during serialization.
* AddNetworkDebugger(NetworkDebugger debugger) - Attached a NetworkDebugger to the client or server.

```cs
ParcelSettings clientSettings = new ParcelSettingsBuilder()
.SetPeer(clientPeer)
.SetNetworkAdapter<UdpNetworkAdapter>()
.SetConnectionTimeout(5000)
.SetDisconnectionTimeout(7500)
.SetUpdatesPerSecond(20)
.SetUnreliablePacketGroupSize(5)
.SetReliablePacketGroupSize(10);

ParcelSettings serverSettings = new ParcelSettingsBuilder()
.SetPeer(serverPeer)
.SetNetworkAdapter<UdpNetworkAdapter>()
.SetConnectionTimeout(5000)
.SetDisconnectionTimeout(7500)
.SetUpdatesPerSecond(60)
.SetUnreliablePacketGroupSize(3)
.SetReliablePacketGroupSize(10);
```

<h2>(Optional) Attaching a NetworkDebugger</h2>

It may be beneficial while working on your project to attach a NetworkDebugger to your client and server. By design, all exceptions that occur during serialization or within any virtual methods of a Packet or SyncedObject will be caught and handled, to prevent your client or server for quitting upon an exception. This safety feature can make it hard to diagnose issues. Attaching a debugger will allow you to log any exceptions encountered.

```cs
NetworkDebugger debugger = new NetworkDebugger(new ConsoleLogger());

//Then, when creating your ParcelSettings call:
.AddNetworkDebugger(debugger)
```

<h2>Creating a Client</h2>

Finally, with a ParcelSettings object created, a new ParcelClient can be initialized. The ParcelClient class is responsible for establishing connections, send and receiving Packets, and managing SyncedObjects. 

It is recommended that you store you client(s) in some singleton object within your project. Alternatively, you can pass your ParcelClient object to places that need it if you want to avoid the singleton pattern.

```cs
ParcelClient client = new ParcelClient(clientSettings);
```

<h3>Connecting to a server</h3>

To establish a connection with a server, a ConnectionToken is required. A ConnectionToken contains the Address and Port of the server to connect to.

Connections are performed asynchronously and should be awaited. They will return a ConnectionResult struct that will contain a status, the server's Peer object, or any rejection reasons should the connection be rejected by the server.

```cs
//Create the connection token
ConnectionToken serverToken = new ConnectionToken("IP of Server", 7778);

//Await the connection
ConnectionResult results = await client.ConnectTo(serverToken);

//Read the results
ConnectionStatus status = results.Status;
Peer serverPeer = results.RemotePeer;
string[] rejectionReasons = results.RejectionReasons;
```

There is also an event that is invoked upon a successful connection.

```cs
client.OnConnected += (Peer server) => { your logic here };
```

<h3>Disconnecting from a server</h3>

Disconnecting from a server is incredibly simple. Call Disconnect will perform a final disconnection handshake. This can take a few iterations of the network loop, so it is recommended that you prevent your application from closing at least until the disconnection has been completed.

```cs
//To disconnect from the server
client.Disconnect();
```
A good way to ensure the disconnection handshake has been completed is to listen for a disconnection event, which will be invoked when the connection is fully closed. It will also invoke in the event of a forced disconnection or timeout.

```cs
//To listen for a disconnection event
client.OnDisconnected += (Peer server, DisconnectionReason reason, string message) => { your logic here };
```

<h3>Sending Packets</h3>

Sending Packets to the server is also incredibly simple. There is also an overload that allows for you to send a Packet to another Peer connected to the server.

```cs
//Sending to the server
client.Send(myPacket);

//Sending to another Peer
client.Send(myPacket, otherPeer);
```

<h3>Synced Objects</h3>

SyncedObjects are a topic complex enough to warrant their own article. Please check out that article for more information.

<h3>Remote Procedure Calls</h3>

Remote procedure calls have not been implemented yet. This article will be updated when they are implemented to reflect how they work.

<h2>Creating a Server</h2>

Creating a server is the same as creating a client, instead using the ParcelServer class.

```cs
ParcelServer server = new ParcelServer(serverSettings);
```

<h3>Handling Connections</h3>

Servers cannot make connections, instead a connection must be made to them. Servers can listen for connections and perform logic when a connection event occurs.

```cs
server.OnRemoteConnected += (Peer client) => { your logic here };
```

Servers can also intercept connections before they are fully connected and optionally reject the connection. Each subscription to this event should return an InitialConnectionResult struct with either a true or false value and a message. The results of all subscriptions will be merged and if any result is false, the connection will be rejected. All false messages will be sent to the client being rejected.

```cs
server.OnInitialConnection += (Peer connecting) => { return new InitialConnectionResult(false, "You are not allowed to join this server!"); };
```

<h3>Handling Disconnections</h3>

Servers can force disconnect users that are connected to them.

```cs
server.ForceDisconnect(peer);
```

Servers can also listen for disconnection events and act accordingly.

```cs
server.OnRemoteDisconnection += (Peer client, DisconnectionReason reason, string message) => { your logic here };
```

<h3>Sending Packets</h3>

Sending Packets to clients is simple. The base Send method will send a Packet to all connected Peers. There is an overload that allows for a Packet to be sent to a group of specified Peers.

```cs
//Sending to all Peers
server.Send(myPacket);

//Sending to specific Peers
server.Send(myPacket, peer1, peer2, peer3, etc...);
```

<h3>Synced Objects</h3>

SyncedObjects are a topic complex enough to warrant their own article. Please check out that article for more information.

<h3>Remote Procedure Calls</h3>

Remote procedure calls have not been implemented yet. This article will be updated when they are implemented to reflect how they work.

<h2>Complete Code</h2>

<h3>Client</h3>

```cs
Peer clientPeer = new PeerBuilder()
.UsePublicAddress()
.SetPort(7777)
.AddProperty("username", "MyUsername"); 

ParcelSettings clientSettings = new ParcelSettingsBuilder()
.SetPeer(clientPeer)
.SetNetworkAdapter<UdpNetworkAdapter>()
.SetConnectionTimeout(5000)
.SetDisconnectionTimeout(7500)
.SetUpdatesPerSecond(20)
.SetUnreliablePacketGroupSize(5)
.SetReliablePacketGroupSize(10);

ParcelClient client = new ParcelClient(clientSettings);

ConnectionToken serverToken = new ConnectionToken("IP of Server", 7778);

bool success = await client.ConnectTo(serverToken);
```

<h3>Server</h3>

```cs
Peer serverPeer = new PeerBuilder()
.UsePublicAddress()
.SetPort(7778);

ParcelSettings serverSettings = new ParcelSettingsBuilder()
.SetPeer(serverPeer)
.SetNetworkAdapter<UdpNetworkAdapter>()
.SetConnectionTimeout(5000)
.SetDisconnectionTimeout(7500)
.SetUpdatesPerSecond(60)
.SetUnreliablePacketGroupSize(3)
.SetReliablePacketGroupSize(10);

ParcelServer server = new ParcelServer(clientSettings);
```