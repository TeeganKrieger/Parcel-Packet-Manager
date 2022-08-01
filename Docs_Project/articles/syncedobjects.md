<style>
    h1.title {
        text-align: center;
        font-size: 3rem;
    }
    h3 {
        font-size: 1.5rem;
    }
</style>

<h1 class="title">Synced Objects</h1>

In Parcel, Synced Objects are standard c# object that inherit from the SyncedObject class. SyncedObjects are Packets that will automatically or manually (depending on preference) keep their Properties in sync between multiple clients and the server. Servers can additionally enforce verification rules on properties to further increase server-side safety.

Synced Objects follow a subscription based model where only Peers that are subscribed to a Synced Object will have access to their data. Each SyncedObject will also have an Owner, who is automatically subscribed to the Synced Object and is the only individual (aside from the server itself) that can make changes to that Synced Object. 

<h2>Defining a Synced Object</h2>

When defining a Synced Object in your project, you should create a new class that extends from the SyncedObject class. In the current version of Parcel, Synced Objects must have a default parameterless constructor. They also cannot be constructed using their constructor. This will change in a future version and the Synced Object creation workflow will change as well.

```cs
public class MySyncedObject : SyncedObject
{
    public MySyncedObject() { }
}
```

<h2>Serialization</h2>

The data within a Synced Object that you wish to have synchronized must be in the form of a c# Property with a getter and a setter. Fields will not be serialized.

example:
```cs
public string MyProperty { get; set; }
```

By default, properties will only be serialized when their value changes. To keep track of these changes, Synced Objects have two options avaliable to them. The first is automated synchronization. This is accomplished by patching the setter method of any serializable property within a Synced Object. To enable automated synchronization, run the following code at the startup on your project.

```cs
SyncedObjectPatcher.Patch();
```

If you choose not to use automated synchronization, you can also use manual synchronization. To do this, you need to add a call to the SyncProperty() method in the setters of any properties you want to synchronize.

```cs
private string _myProperty;
public string MyProperty
{
    get => this._myProperty;
    set
    {
        this._myProperty = value;
        SyncProperty();
    }
}
```

There is also an option to force a property to be serialized even if it didn't change any time the Synced Object would be serialized. This can be done by applying the AlwaysSerialize attribute to the property.

```cs
[AlwaysSerialize]
public string MyProperty { get; set; }
```

If you wish for a property to not be serialized under any condition, you can add the Ignore attribute to the property.

```cs
[Ignore]
public string MyProperty { get; set; }
```

You can also flip the default behavior of the serializer so that properties need to be explicitly marked for serialization using the OptIn attribute on the Synced Object itself and the Serialize attribute on the property.

```cs
[OptIn]
public class MySyncedObject : SyncedObject
{
    [Serialize]
    public string MyProperty { get; set;}
}
```

<h2>Included Properties</h2>

Synced Objects contain seven included properties that are in place to support any logic that could be included in the Packet. These properties do not serialize with each Packet.

* SyncedObjectID ID - The ID of this Synced Object.
* Peer Owner - The Peer that owns this Synced Object.
* Peer Sender - The Peer that last sent changes for this Synced Object. (this will only ever be the server on clients and the owner on servers).
* IsServer - Whether this Synced Object is a server or client instance.
* IsClient - Whether this Synced Object is a client or server instance.
* Client - The ParcelClient that this Synced Object is associated to. Null if IsClient is false.
* Server - The ParcelServer that this Synced Object is associated to. Null if IsServer is false.

<h2>Virtual Methods</h2>

Synced Objects also provide virtual methods that you can override to add logic to your objects.

* bool CanSend() - Allows for logic to be performed before serialization to determine if the Synced Object should be synchronized. Return true to send perform synchronization.
* void OnSend() - Allows for logic to be performed shortly before serialization. This is only called if CanSend returned true.
* void OnReceive() - Allows for logic to be performed shortly after deserialization.
* void OnCreated() - Allows for logic to be performed shortly after the Synced Object is created on both the client and server.
* void OnDestroyed() - Allows for logic to be performed immediately after the Synced Object is destroyed on both the client and the server.
* void OnOwnerChange(Peer previousOwner, Peer newOwner) - Allows for logic to be performed when the owner of a Synced Object changes.
* void OnPropertiesChanged(Dictionary<string, PropertyChanges> changes) - Allows for logic to be performed when any property within the Synced Object is updated on the client or server.

<h2>Creating a Synced Object</h2>

As stated before, the creation workflow for a Synced Object will be changing in a future version. For now, the process is fairly simple but limited. Synced Objects can only be created on the server.

```cs
//Non-Generic overload
MySyncedObject mso = (MySyncedObject)myServer.CreateSyncedObject(typeof(MySyncedObject), owner);
//Generic overload
MySyncedObject mso = myServer.CreateSyncedObject<MySyncedObject>(owner);
```

<h2>Server Methods</h2>

The ParcelServer class contains some methods designed to support Synced Objects.

* SyncedObject CreateSyncedObject(Type type, Peer owner) - Creates a new Synced Object using the type provided.
* T CreateSyncedObject<T>(Peer owner) - Creates a new Synced Object using the generic type provided.
* bool DestroySyncedObject(SyncedObjectID syncedObjectID) - Destroys a Synced Object with a matching ID. Returns true if successful or false if unsuccessful.
* bool TryGetSyncedObject(SyncedObjectID syncedObjectID, out SyncedObject syncedObject) - Attempts to get an instance of a Synced Object with a matching ID. Returns true if successful or false if unsuccessful.
* bool TryGetSyncedObject<T>(SyncedObjectID syncedObjectID, out T syncedObject) - Attempts to get an instance of a Synced Object with the provided type and a matching ID. Returns true if successful or false if unsuccessful.
* bool TryGetSyncedObjectSubscribers(SyncedObjectID syncedObjectID, out Peer[] subscribers) - Attempts to get an array of Peers subscribed to the Synced Object with a matching ID. Returns true if successful or false if unsuccessful.
* bool TryTransferSyncedObjectOwnership(SyncedObjectID syncedObjectID, Peer newOwner) - Attempts to transfer ownership of a Synced Object with a matching ID to a new Peer. Returns true if successful or false if unsuccessful.
* bool AddSyncedObjectSubscriptions(SyncedObjectID syncedObjectID, params Peer[] subscribers) - Adds Peers to the subscription list of a Synced Object with a matching ID. Returns true if successful or false if unsuccessful.
* bool RemoveSyncedObjectSubscriptions(SyncedObjectID syncedObjectID, params Peer[] subscribers) - Removes Peers from the subscriptions list of a Synced Object with a matching ID. Returns true if successful or false if unsuccessful.

<h2>Client Methods</h2>

The ParcelClient class also provides some methods supporting Synced Object.

* bool TryGetSyncedObject(SyncedObjectID syncedObjectID, out SyncedObject syncedObject) - Attempts to get an instance of a Synced Object with a matching ID. Returns true if successful or false if unsuccessful.
* bool TryGetSyncedObject<T>(SyncedObjectID syncedObjectID, out T syncedObject) - Attempts to get an instance of a Synced Object with the provided type and a matching ID. Returns true if successful or false if unsuccessful.

<h2>Shortcuts</h2>

Synced Objects also contain shortcuts for a few of the server methods listed above. These include the following.

* void UpdateOwner(Peer newOwner) - Transfers the ownership of the Synced Object to another Peer.
* void AddSubscriptions(params Peer[] subscribers) - Add subscribers to this Synced Object's subscribers list.
* Peer[] GetSubscriptions() - Get an array of Peers who are subscribed to this Synced Object.
* void RemoveSubscriptions(params Peer[] subscribers) - Remove subscribers from this Synced Objects's subscribers list.

<h2>Example</h2>

```cs
public class Player : SyncedObject
{
    public int Health { get; set; }

    public Player() { }

    private void Kill()
    {
        this.Server.DestroySyncedObject(this.ID);
    }

    protected override void OnCreated() 
    {
        if (this.IsClient)
        {
            World.RegisterPlayer(this);
        }
    }

    protected override void OnDestroyed()
    {
        if (this.IsClient)
        {
            World.UnregisterPlayer(this);
        }
    }

    protected override void OnRecieved()
    {
        if (this.IsServer && this.Health <= 0)
        {
            Kill();
        }
    }
}
```