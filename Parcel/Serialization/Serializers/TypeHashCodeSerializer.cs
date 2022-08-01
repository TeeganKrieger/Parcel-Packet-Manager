using Parcel.Lib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Parcel.Serialization
{
    internal class TypeHashCodeSerializer : Serializer
    {
        private static QuickDelegate HashCodeGetter;
        private static QuickDelegate GenericArgumentsGetter;

        static TypeHashCodeSerializer()
        {
            //NOTE: Magic strings are used here since this version of C# doesn't support nameof(Type.PrivateMember). :(

            Type thcType = typeof(TypeHashCode);

            PropertyInfo hashcode = thcType.GetProperty("HashCode", BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo genericargs = thcType.GetProperty("GenericArguments", BindingFlags.Instance | BindingFlags.NonPublic);

            HashCodeGetter = hashcode.GetGetMethod(true).Bind();
            GenericArgumentsGetter = genericargs.GetGetMethod(true).Bind();

        }

        public override bool CanSerialize(Type type)
        {
            return type.Equals(typeof(TypeHashCode));
        }

        public override object Deserialize(ByteReader reader)
        {
            return reader.ReadTypeHashCode();
        }

        public override void Serialize(ByteWriter writer, object obj)
        {
            writer.Write((TypeHashCode)obj);
        }
    }
}
