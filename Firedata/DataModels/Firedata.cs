using Firebase.Firestore;
using Glitch9.Cloud;
using Glitch9.IO.Network;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Glitch9.Apis.Google.Firestore
{
    public abstract class Firedata<TSelf> : IFiredata
        where TSelf : Firedata<TSelf>
    {
        protected class FirestorePropertyInfo
        {
            public string Name { get; private set; }
            public PropertyInfo Property { get; private set; }
            public Type PropertyType => Property.PropertyType;

            public FirestorePropertyInfo(string name, PropertyInfo field)
            {
                Name = name;
                Property = field;
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
                    GNLog.Error($"{Name}(Property)을 {PropertyType}으로 변환하는데 실패했습니다.");
                    GNLog.Exception(ex);
                }
            }
        }

        public virtual DocumentReference GetDocument(string arg = null) => DocumentFactory?.Invoke(new string[] { arg });
        protected static readonly Dictionary<string/* 타입이름 */, Dictionary<string/* 프로퍼티이름 */, FirestorePropertyInfo>> PropertyCache = new();
        protected static readonly Dictionary<string/* 서버용으로 SnakeCase로 바뀐 후 프로퍼티이름 */, string/* 바뀌기 전 프로퍼티이름 */> PropertyMap = new();
        public static string GetOriginalPropertyName(string snakeCasePropertyName)
        {
            if (PropertyMap.TryGetValue(snakeCasePropertyName, out string originalPropertyName))
            {
                return originalPropertyName;
            }
            else
            {
                return snakeCasePropertyName.ToPascalCase();
            }
        }

        public DocumentReference Document { get; set; }
        public Func<string[], DocumentReference> DocumentFactory { get; set; }

        static Firedata()
        {
            Type type = typeof(TSelf);
            string thisClassName = type.Name;

            if (type.IsAbstract || type.IsInterface) return;
            
            // ReSharper disable once PossibleNullReferenceException
            if (PropertyCache.ContainsKey(thisClassName)) return;
            
            PropertyCache.Add(thisClassName, new Dictionary<string, FirestorePropertyInfo>());

            while (type != null && type != typeof(object)) // Use typeof(object) to cover all base classes
            {
                foreach (PropertyInfo property in PropertyInfoCache.Get(type))
                {
                    if (!property.CanWrite) continue; // Check if the property has setter

                    // Check if the property has the CloudDataAttribute
                    CloudDataAttribute attribute = property.GetCustomAttribute<CloudDataAttribute>();
                    if (attribute == null) continue;

                    string propertyName = attribute.PropertyName;
                    if (string.IsNullOrWhiteSpace(propertyName))
                    {
                        propertyName = property.Name.ToSnakeCase();
                    }

                    GNLog.Info($"{type.Name}의 {property.Name}이 {propertyName}로 변환됩니다.");
                    PropertyMap.TryAdd(propertyName, property.Name); // Cache the property names (use TryAdd to handle potential duplicates)

                    if (!PropertyCache[thisClassName].ContainsKey(propertyName))
                    {
                        PropertyCache[thisClassName].Add(propertyName, new FirestorePropertyInfo(property.Name, property));
                    }
                }

                type = type.BaseType;
            }
        }

        protected Firedata()
        {
            DocumentFactory = FirestoreReference.GetDocumentFactory<TSelf>();
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

                    // 맵에 추가된 value의 타입이 뭔지 로그를 남긴다.
                    // GNLog.Warning($"{property.Key} is converted to {convertedValue.GetType()}");
                    mapField.Add(property.Key, convertedValue);
                }
                return mapField;
            }
            catch (Exception ex)
            {
                GNLog.Error("Failed to retrieve fields using reflection. See error log for details.");
                GNLog.Exception(ex);
                return null;
            }
        }

        public IFiredata SetMap(Dictionary<string, object> firestoreMap)
        {
            if (firestoreMap == null || firestoreMap.Count == 0) return null;

            Dictionary<string, FirestorePropertyInfo> properties = GetCachedProperties();
            if (properties == null) return null;

            foreach (KeyValuePair<string, object> pair in firestoreMap)
            {
                if (properties.TryGetValue(pair.Key, out FirestorePropertyInfo property))
                {
                    // Check if the value is not null or if the field type allows null
                    if (pair.Value != null || pair.Value != default || Nullable.GetUnderlyingType(property.PropertyType) != null)
                    {
                        property.SetValue(this, pair.Value);
                    }
                    else
                    {
                        GNLog.Error($"Null value found for non-nullable field: {pair.Key}");
                    }
                }
            }
            return this;
        }

        protected Dictionary<string, FirestorePropertyInfo> GetCachedProperties()
        {
            string typeName = typeof(TSelf).Name;

            if (PropertyCache.IsNullOrEmpty())
            {
                GNLog.Error("리플렉션 캐시가 없습니다."); // "Reflection cache is empty.
                return null;
            }

            if (!PropertyCache.ContainsKey(typeName))
            {
                GNLog.Error($"{typeName}의 필드 캐시가 없습니다.");
                return null;
            }

            Dictionary<string, FirestorePropertyInfo> fields = PropertyCache[typeName];

            if (fields.IsNullOrEmpty())
            {
                GNLog.Error($"{typeName}의 필드의 내용이 없습니다.");
                return null;
            }

            return fields;
        }
    }
}