using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Glitch9.Apis.Google.Firestore
{
    public static class FirestoreReference
    {
        private static readonly FirebaseFirestore _firestore = FirebaseFirestore.DefaultInstance;
        private static readonly Dictionary<Type, Func<string[], DocumentReference>> k_DocumentFactories = new();
        private static readonly Dictionary<Type, Func<string[], CollectionReference>> k_CollectionFactories = new();
        private static readonly object k_LockObject = new(); // Lock object for synchronization

        static FirestoreReference()
        {
            RegisterAllFactories();
        }

        private static void RegisterAllFactories()
        {
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();

            foreach (Type type in types)
            {
                FirestoreDocumentAttribute docAttr = type.GetCustomAttribute<FirestoreDocumentAttribute>();
                if (docAttr != null)
                {
                    string path = docAttr.Path;
                    if (string.IsNullOrEmpty(path))
                    {
                        Debug.LogError($"Document path for type {type.Name} is not set.");
                        return;
                    }

                    RegisterDocumentFactory(type, args => CreateDocumentReference(path, args));
                }

                FirestoreCollectionAttribute colAttr = type.GetCustomAttribute<FirestoreCollectionAttribute>();
                if (colAttr != null)
                {
                    string path = colAttr.Path;
                    if (string.IsNullOrEmpty(path))
                    {
                        Debug.LogError($"Collection path for type {type.Name} is not set.");
                        return;
                    }

                    RegisterCollectionFactory(type, args => CreateCollectionReference(path, args));
                }
            }
        }

        private static DocumentReference CreateDocumentReference(string path, params string[] args)
        {
            string formattedPath = string.Format(path, args);
            return _firestore.Document(formattedPath);
        }

        private static CollectionReference CreateCollectionReference(string path, params string[] args)
        {
            string formattedPath = string.Format(path, args);
            return _firestore.Collection(formattedPath);
        }

        public static void RegisterDocumentFactory<T>(Func<string[], DocumentReference> factory)
        {
            RegisterDocumentFactory(typeof(T), factory);
        }

        public static void RegisterCollectionFactory<T>(Func<string[], CollectionReference> factory)
        {
            RegisterCollectionFactory(typeof(T), factory);
        }

        public static void RegisterDocumentFactory(Type type, Func<string[], DocumentReference> factory)
        {
            lock (k_LockObject)
            {
                k_DocumentFactories.AddOrUpdate(type, factory);
            }
        }

        public static void RegisterCollectionFactory(Type type, Func<string[], CollectionReference> factory)
        {
            lock (k_LockObject)
            {
                k_CollectionFactories.AddOrUpdate(type, factory);
            }
        }

        public static DocumentReference GetDocument<T>(params string[] args)
        {
            return GetDocument(typeof(T), args);
        }

        public static DocumentReference GetDocument(Type type, params string[] args)
        {
            lock (k_LockObject)
            {
                if (k_DocumentFactories.TryGetValue(type, out Func<string[], DocumentReference> factory))
                {
                    return factory(args);
                }
                else
                {
                    throw new Exception($"Document factory for type {type.Name} is not registered.");
                }
            }
        }

        public static Func<string[], DocumentReference> GetDocumentFactory<T>()
        {
            lock (k_LockObject)
            {
                if (k_DocumentFactories.TryGetValue(typeof(T), out Func<string[], DocumentReference> factory))
                {
                    return factory;
                }
                else
                {
                    throw new Exception($"Document factory for type {typeof(T).Name} is not registered.");
                }
            }
        }

        public static CollectionReference GetCollection<T>(params string[] args)
        {
            return GetCollection(typeof(T), args);
        }

        public static CollectionReference GetCollection(Type type, params string[] args)
        {
            lock (k_LockObject)
            {
                if (k_CollectionFactories.TryGetValue(type, out Func<string[], CollectionReference> factory))
                {
                    return factory(args);
                }
                else
                {
                    throw new Exception($"Collection factory for type {type.Name} is not registered.");
                }
            }
        }

        public static Func<string[], CollectionReference> GetCollectionFactory<T>()
        {
            lock (k_LockObject)
            {
                if (k_CollectionFactories.TryGetValue(typeof(T), out Func<string[], CollectionReference> factory))
                {
                    return factory;
                }
                else
                {
                    throw new Exception($"Collection factory for type {typeof(T).Name} is not registered.");
                }
            }
        }
    }
}