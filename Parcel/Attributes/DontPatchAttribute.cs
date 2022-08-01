using System;

namespace Parcel
{
    /// <summary>
    /// Specifies that a property within a <see cref="Parcel.Packets.SyncedObject">SyncedObject</see> should not be patched when
    /// <see cref="Parcel.Packets.SyncedObjectPatcher.Patch">SyncedObjectPatcher.Patch</see> is called.
    /// </summary>
    /// <remarks>
    /// SyncedObject patching is a process that inserts a snippet of code to be called after the setter of each property in a SyncedObject.
    /// This allows for changes to the properties to be automatically tracked without implementation from the developer.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DontPatchAttribute : Attribute
    {
    }
}
