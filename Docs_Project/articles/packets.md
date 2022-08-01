<style>
    h1.title {
        text-align: center;
        font-size: 3rem;
    }
    h3 {
        font-size: 1.5rem;
    }
</style>

<h1 class="title">Packets</h1>

In Parcel, Packets are standard c# object that inherit from the Packet class. Packets can contain any type of data from primitives to complex objects. It is recommended to keep most Packets lightweight, especially those that are sent very often. 

<h2>Defining a Packet</h2>

When defining a Packet in your project, you should create a new class that extends from the Packet class. It is highly recommended that you implement a default parameterless constructor into your Packet on top of any other constructors you create, especially if your Packet has fields that need to be initialized in the constructor. Packets lacking a default parameterless constructor will still be constructed, however, their behavior is not predictable and a Warning will be thrown when one is created.

```cs
public class MyPacket : Packet
{
    public MyPacket() { }
}
```

<h2>Serialization</h2>

The data within a packet that you wish to have sent must be in the form of a c# Property with a getter and a setter. Fields will not be serialized.

example:
```cs
public string MyProperty { get; set; }
```

By default, properties will be serialized when a packet is sent. If you wish for a property to not be sent, you can add the Ignore attribute to the property.

```cs
[Ignore]
public string MyProperty { get; set; }
```

You can also flip the default behavior of the serializer so that properties need to be explicitly marked for serialization using the OptIn attribute on the Packet itself and the Serialize attribute on the property.

```cs
[OptIn]
public class MyPacket : Packet
{
    [Serialize]
    public string MyProperty { get; set;}
}
```

<h2>Included Properties</h2>

Packets contain five included properties that are in place to support any logic that could be included in the Packet. These properties do not serialize with each Packet.

* Peer Sender - The Peer that sent this Packet.
* IsServer - Whether this Packet was received by a server or client.
* IsClient - Whether this Packet was received by a client or server.
* Client - The ParcelClient that received this Packet. Null if IsClient is false.
* Server - The ParcelServer that received this Packet. Null if IsServer is false.

<h2>Virtual Methods</h2>

Packets also provide a few virtual methods that you can override to add logic to your packets.

* bool CanSend() - Allows for logic to be performed before serialization to determine if the Packet should be sent. Return true to send this Packet.
* void OnSend() - Allows for logic to be performed shortly before serialization. This is only called if CanSend returned true.
* void OnReceive() - Allows for logic to be performed shortly after deserialization.

A Note of thread-safety: 

By default, these methods are called from a worker thread. Ensure any code you write within these methods is thread-safe. If you want these methods to be called on a thread of your choosing, there is a option available in the ParcelSettings class that disables automatic updates for the client or server.

<h2>Built-in Packet Types</h2>

There are two additional built-in Packet types for you to use. These Packets simply enforce sending a Packet in a single direction.

* ServerToClientPacket - A Packet that can only travel from a Server to a Client.
* ClientToServerPacket - A Packet that can only travel from a Client to a Server.

If you wish to implement your own logic in the CanSend() virtual method within these Packets, ensure you call base.CanSend() or they will lose their functionality.

```cs
protected override void CanSend() 
{
    return base.CanSend() && MyCondition;
}
```
<h2>Example</h2>

An example of a Packet that performs a weapon firing action on the server:
```cs
public sealed class FireWeaponPacket : ClientToServerPacket
{
    public Vector3 Ray { get; private set; }
    public string WeaponID { get; private set; }

    public FireWeaponPacket() { }

    public FireWeaponPacket(Vector3 ray, string weaponID)
    {
        this.Ray = ray;
        this.WeaponID = weaponID;
    }

    protected override void OnReceive()
    {
        if (WeaponsManifest.TryGetWeapon(this.WeaponID, out Weapon weapon))
        {
            if (World.PerformHitscan(this.Ray, out Entity hit))
            {
                hit.Damage(weapon);
            }
        }
    }
}
```