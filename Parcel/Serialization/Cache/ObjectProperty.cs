using Parcel.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Parcel.Serialization
{
    /// <summary>
    /// Represents a collection of useful information for serialization purposes about a Property.
    /// </summary>
    public sealed class ObjectProperty
    {
        private Dictionary<Type, List<Attribute>> _attributes;

        /// <summary>
        /// The PropertyInfo instance of this Property.
        /// </summary>
        public PropertyInfo PropertyInfo { get; private set; }

        /// <summary>
        /// The Type of this Property.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// The hash code of the name of this Property.
        /// </summary>
        public uint NameHash { get; private set; }

        /// <summary>
        /// The name of this Property.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// A <see cref="QuickDelegate"/> bound to the getter of this Property.
        /// </summary>
        internal QuickDelegate Getter { get; private set; }

        /// <summary>
        /// A <see cref="QuickDelegate"/> bound to the setter of this Property.
        /// </summary>
        internal QuickDelegate Setter { get; private set; }


        #region CONSTRUCTOR

        /// <summary>
        /// Construct a new instance of ObjectProperty.
        /// </summary>
        /// <param name="propertyInfo">The <see cref="PropertyInfo"/> of this Property.</param>
        /// <param name="name">The name of this Property.</param>
        /// <param name="hash">The name hash code of this Property.</param>
        /// <param name="attributes">A dictionary of Attributes that this Property has, stored by the Attribute Type.</param>
        /// <param name="getter">A QuickDelegate bound to the getter of this Property.</param>
        /// <param name="setter">A QuickDelegate bound to the setter of this Property.</param>
        private ObjectProperty(PropertyInfo propertyInfo, string name, uint hash, Dictionary<Type, List<Attribute>> attributes, QuickDelegate getter, QuickDelegate setter)
        {
            this.PropertyInfo = propertyInfo;
            this.Type = propertyInfo.PropertyType;
            this.Name = name;
            this.NameHash = hash;
            this.Getter = getter;
            this.Setter = setter;
            this._attributes = attributes;
        }

        #endregion


        #region STATIC CREATION

        /// <summary>
        /// Attempts to create an instance of ObjectProperty using <paramref name="property"/>.
        /// </summary>
        /// <param name="property">The PropertyInfo to construct an ObjectProperty instance from.</param>
        /// <param name="objectProperty">The ObjectProperty instance that was created.</param>
        /// <returns><see langword="true"/> if the ObjectProperty was successfully created; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// ObjectProperty creation will fail if the property does not have both a getter and a setter.
        /// It will also fail if the Property has the <see cref="IgnoreAttribute"/>.
        /// </remarks>
        public static bool TryCreate(PropertyInfo property, out ObjectProperty objectProperty)
        {
            Attribute[] allAttributes = property.GetCustomAttributes().ToArray();

            Dictionary<Type, List<Attribute>> sortedAttributes = new Dictionary<Type, List<Attribute>>();

            foreach (Attribute att in allAttributes)
            {
                Type attType = att.GetType();
                if (!sortedAttributes.ContainsKey(attType))
                    sortedAttributes.Add(attType, new List<Attribute>());
                sortedAttributes[attType].Add(att);
            }

            if (property.DeclaringType.IsValueType)
            {
                MethodInfo getter = property.GetGetMethod(true);
                QuickDelegate setter = new QuickDelegate((object target, object[] args) => { property.SetValue(target, args[0]); return null; });

                if (getter != null)
                {
                    objectProperty = new ObjectProperty(property, property.Name,
                        property.GetPropertyNameHash(), sortedAttributes, getter.Bind(), setter);
                    return true;
                }
                else
                {
                    objectProperty = null;
                    return false;
                }
            }
            else
            {
                MethodInfo getter = property.GetGetMethod(true);
                MethodInfo setter = property.GetSetMethod(true);

                if (getter != null && setter != null)
                {
                    objectProperty = new ObjectProperty(property, property.Name,
                        property.GetPropertyNameHash(), sortedAttributes, getter.Bind(), setter.Bind());
                    return true;
                }
                else
                {
                    objectProperty = null;
                    return false;
                }
            }
        }

        #endregion


        #region INSTANCE ACCESS

        /// <summary>
        /// Call the getter for this ObjectProperty on an object.
        /// </summary>
        /// <param name="instance">The instance of the object to call the getter on.</param>
        /// <returns>The value obtained from the getter.</returns>
        public object GetValue(object instance)
        {
            //TODO: Add try catch
            return Getter?.Invoke(instance, new object[0]);
        }

        /// <summary>
        /// Call the setter for this ObjectProperty on an object.
        /// </summary>
        /// <param name="instance">The instance of the object to call the setter on.</param>
        /// <param name="newValue">The value to set the Property to.</param>
        public void SetValue(object instance, object newValue)
        {
            //TODO: Add try catch
            Setter?.Invoke(instance, new object[] { newValue });
        }

        /// <summary>
        /// Get an Attribute of this Property, if it exists.
        /// </summary>
        /// <typeparam name="T">The Type of Attribute to get.</typeparam>
        /// <returns>The Attribute instance if it exists; otherwise, <see langword="null"/>.</returns>
        public T GetCustomAttribute<T>() where T : Attribute
        {
            if (!this._attributes.ContainsKey(typeof(T)))
                return null;
            return (T)this._attributes[typeof(T)].FirstOrDefault();
        }

        /// <summary>
        /// Get all Attributes of Type <typeparamref name="T"/> belonging to this Property, if they exists.
        /// </summary>
        /// <typeparam name="T">The Type of Attribute to get.</typeparam>
        /// <returns>An array of Attribute instances if they exist; otherwise, <see langword="null"/>.</returns>
        public T[] GetCustomAttributes<T>() where T : Attribute
        {
            if (!this._attributes.ContainsKey(typeof(T)))
                return null;
            return (T[])this._attributes[typeof(T)].ToArray();
        }

        #endregion

    }
}
