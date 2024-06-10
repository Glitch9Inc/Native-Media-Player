using Firebase.Firestore;
using Glitch9.Cloud;
using Glitch9.IO.Network;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Glitch9.Apis.Google.Firestore
{
    /// <summary>
    /// Represents a base class for Firestore data objects that provides functionality to interact with Firestore.
    /// </summary>
    /// <typeparam name="TSelf">The type of the derived class.</typeparam>
    public abstract class Firedata<TSelf> : IFiredata
        where TSelf : Firedata<TSelf>
    {
        protected static readonly Dictionary<string, Dictionary<string, FirestorePropertyInfo>> PropertyCache = new();

        static Firedata()
        {
            Type type = typeof(TSelf);
            string thisClassName = type.Name;

            if (type.IsAbstract || type.IsInterface) return;

            if (PropertyCache.ContainsKey(thisClassName)) return;

            PropertyCache.Add(thisClassName, new Dictionary<string, FirestorePropertyInfo>());

            while (type != null && type != typeof(object))
            {
                foreach (PropertyInfo property in PropertyInfoCache.Get(type))
                {
                    if (!property.CanWrite) continue;

                    CloudDataAttribute attribute = property.GetCustomAttribute<CloudDataAttribute>();
                    if (attribute == null) continue;

                    string propertyName = attribute.PropertyName;
                    if (string.IsNullOrWhiteSpace(propertyName))
                    {
                        propertyName = property.Name.ToSnakeCase();
                    }

                    FirestoreManager.Logger.Info($"{type.Name}{Strings.PropertyConverted}{property.Name}{Strings.To}{propertyName}");

                    if (!PropertyCache[thisClassName].ContainsKey(propertyName))
                    {
                        PropertyCache[thisClassName].Add(propertyName, new FirestorePropertyInfo(property.Name, property));
                    }
                }

                type = type.BaseType;
            }
        }

        /// <summary>
        /// Represents the metadata of a Firestore property.
        /// </summary>
        protected class FirestorePropertyInfo
        {
            public string Name { get; }
            public PropertyInfo Property { get; }
            public Type PropertyType => Property.PropertyType;

            public FirestorePropertyInfo(string name, PropertyInfo property)
            {
                Name = name;
                Property = property;
            }

            public object GetValue(object obj) => Property.GetValue(obj);

            public void SetValue(object obj, object value)
            {
                try
                {
                    object convertedValue = CloudConverter.ToLocalFormat(PropertyType, Name, value);
                    if (convertedValue == null) return;
                    Property.SetValue(obj, convertedValue);
                }
                catch (Exception ex)
                {
                    FirestoreManager.Logger.Error($"{Name}{Strings.FailedToConvert}{PropertyType}.");
                    GNLog.Exception(ex);
                }
            }
        }

        public DocumentReference Document { get; set; }
        private readonly Func<string[], DocumentReference> _documentFactory;

        /// <summary>
        /// Gets the Firestore document reference.
        /// </summary>
        /// <param name="args">Optional arguments to construct the document reference.</param>
        /// <returns>The Firestore document reference.</returns>
        public virtual DocumentReference GetDocument(params string[] args)
        {
            if (Document != null) return Document;
            if (_documentFactory == null) return null;
            Document = _documentFactory(args);
            return Document;
        }

        protected Firedata()
        {
            _documentFactory = FirestoreReference.GetDocumentFactory<TSelf>();
        }

        protected Firedata(string documentName)
        {
            Document = FirestoreReference.GetDocument<TSelf>(documentName);
        }

        protected Firedata(DocumentReference document)
        {
            if (document.LogIfNull()) return;
            Document = document;
        }

        /// <summary>
        /// Converts the current object to a format that can be stored in Firestore.
        /// </summary>
        /// <returns>A dictionary representing the Firestore format of the current object.</returns>
        public Dictionary<string, object> ToFirestoreFormat()
        {
            Dictionary<string, FirestorePropertyInfo> properties = GetCachedProperties();
            if (properties == null) return null;

            try
            {
                Dictionary<string, object> mapField = new();
                foreach (KeyValuePair<string, FirestorePropertyInfo> property in properties)
                {
                    if (property.Value == null) continue;
                    object convertedValue = CloudConverter.ToCloudFormat(property.Value.PropertyType, property.Value.GetValue(this));
                    if (convertedValue == null) continue;

                    mapField.Add(property.Key, convertedValue);
                }
                return mapField;
            }
            catch (Exception ex)
            {
                FirestoreManager.Logger.Error(Strings.ReflectionFailed);
                GNLog.Exception(ex);
                return null;
            }
        }

        /// <summary>
        /// Applies Firestore data to the current object.
        /// </summary>
        /// <param name="firestoreData">A dictionary containing the Firestore data.</param>
        /// <returns>The current object with applied Firestore data.</returns>
        public IFiredata ToLocalFormat(Dictionary<string, object> firestoreData)
        {
            if (firestoreData.IsNullOrEmpty()) return null;

            Dictionary<string, FirestorePropertyInfo> properties = GetCachedProperties();
            if (properties == null) return null;

            foreach (KeyValuePair<string, object> pair in firestoreData)
            {
                if (properties.TryGetValue(pair.Key, out FirestorePropertyInfo property))
                {
                    if (pair.Value != null || pair.Value != default || Nullable.GetUnderlyingType(property.PropertyType) != null)
                    {
                        property.SetValue(this, pair.Value);
                    }
                    else
                    {
                        FirestoreManager.Logger.Error($"{Strings.NullValueForNonNullableField}{pair.Key}");
                    }
                }
            }
            return this;
        }

        /// <summary>
        /// Gets the cached properties of the current type.
        /// </summary>
        /// <returns>A dictionary containing the cached properties.</returns>
        protected Dictionary<string, FirestorePropertyInfo> GetCachedProperties()
        {
            string typeName = typeof(TSelf).Name;

            if (PropertyCache.IsNullOrEmpty())
            {
                FirestoreManager.Logger.Error(Strings.ReflectionCacheEmpty);
                return null;
            }

            if (!PropertyCache.ContainsKey(typeName))
            {
                FirestoreManager.Logger.Error($"{Strings.NoFieldCache}{typeName}");
                return null;
            }

            Dictionary<string, FirestorePropertyInfo> fields = PropertyCache[typeName];

            if (fields.IsNullOrEmpty())
            {
                FirestoreManager.Logger.Error($"{Strings.NoFieldContents}{typeName}");
                return null;
            }

            return fields;
        }

        /// <summary>
        /// Contains constant string values for logging and messages.
        /// </summary>
        private static class Strings
        {
            internal const string PropertyConverted = "'s property has been converted: ";
            internal const string To = " -> ";
            internal const string FailedToConvert = " failed to convert the property.";
            internal const string ReflectionFailed = "Failed to retrieve fields using reflection. See error log for details.";
            internal const string NullValueForNonNullableField = "Null value found for non-nullable field: ";
            internal const string ReflectionCacheEmpty = "Reflection cache is empty.";
            internal const string NoFieldCache = "No field cache for: ";
            internal const string NoFieldContents = "No field contents for: ";
        }
    }
}
