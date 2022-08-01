using System.Reflection;

namespace Parcel.Serialization
{
    /// <summary>
    /// Extension methods for the PropertyInfo type.
    /// </summary>
    internal static class PropertyInfoHashCode
    {

        /// <summary>
        /// Convert a property's name into a hashcode.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>A uint hashcode.</returns>
        public static uint FromString(string str)
        {
            uint hash = 1000000007U;
            foreach (char c in str)
            {
                hash ^= 172056037U;
                hash += c * 526453999U;
            }
            return hash;
        }

        /// <summary>
        /// Convert a property's name into a hashcode.
        /// </summary>
        /// <param name="propInfo">The PropertyInfo instance.</param>
        /// <returns>A uint hashcode.</returns>
        public static uint GetPropertyNameHash(this PropertyInfo propInfo)
        {
            uint hash = 1000000007U;
            foreach (char c in propInfo.Name)
            {
                hash ^= 172056037U;
                hash += c * 526453999U;
            }
            return hash;
        }
    }
}
