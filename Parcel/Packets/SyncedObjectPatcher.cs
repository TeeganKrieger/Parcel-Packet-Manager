using HarmonyLib;
using Parcel.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Parcel.Packets
{
    internal static class SyncedObjectPatcher
    {
        private const string HARMONY_INSTANCE_NAME = "parcel.syncedobject.patch";
        private const string EXCP_ALREADY_PATCHED = "Failed to patch SyncedObjects. A patch has already been applied.";
        private const string EXCP_NOT_PATCHED = "Failed to unpatch SyncedObjects. No patches are currently applied.";

        private static bool Patched { get; set; }
        private static Harmony HarmonyInstance { get; set; }

        public static void Patch()
        {
            if (Patched)
                throw new InvalidOperationException(EXCP_ALREADY_PATCHED);

            MethodInfo updateProperty = typeof(SyncedObject).GetMethod("SyncProperty", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(SyncedObject), typeof(MethodBase) }, null);

            HarmonyInstance = new Harmony(HARMONY_INSTANCE_NAME);

            foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in ass.GetTypes())
                {
                    if (typeof(SyncedObject).IsAssignableFrom(type))
                    {
                        if (type.GetCustomAttribute<OptInAttribute>() != null)
                        {
                            foreach (PropertyInfo propInfo in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                            {
                                if (propInfo.GetCustomAttribute<DontPatchAttribute>() != null)
                                    continue;

                                if (propInfo.GetCustomAttribute<SerializableAttribute>() != null || propInfo.GetCustomAttribute<AlwaysSerializeAttribute>() != null)
                                {
                                    MethodInfo setter = propInfo.GetSetMethod(true);
                                    if (propInfo.GetGetMethod(true) != null && setter != null)
                                        HarmonyInstance.Patch(setter, postfix: new HarmonyMethod(updateProperty));
                                }
                            }
                        }
                        else //OptOut by default
                        {
                            foreach (PropertyInfo propInfo in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                            {
                                if (propInfo.GetCustomAttribute<DontPatchAttribute>() != null || propInfo.GetCustomAttribute<IgnoreAttribute>() != null)
                                    continue;
                                MethodInfo setter = propInfo.GetSetMethod(true);
                                if (propInfo.GetGetMethod(true) != null && setter != null)
                                    HarmonyInstance.Patch(setter, postfix: new HarmonyMethod(updateProperty));
                            }
                        }
                    }
                }
            }
        }

        public static void Unpatch()
        {
            if (!Patched)
                throw new InvalidOperationException(EXCP_NOT_PATCHED);
            HarmonyInstance.UnpatchAll();
        }

    }
}
