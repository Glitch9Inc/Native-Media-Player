using Firebase.Firestore;
using Glitch9.Apis.Google.Firestore.Tasks;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Glitch9.Apis.Google.Firestore
{
    /// <summary>
    /// Provides methods to manage Firestore document and collection references dynamically.
    /// </summary>
    public static class FirestoreReference
    {
        private static readonly FirebaseFirestore _firestore = FirebaseFirestore.DefaultInstance;
        private static readonly Dictionary<Type, Func<string[], DocumentReference>> k_DocumentFactories = new();
        private static readonly Dictionary<Type, Func<string[], CollectionReference>> k_CollectionFactories = new();
        private static readonly object k_LockObject = new(); // Lock object for synchronization

        /// <summary>
        /// Static constructor to initialize and register all factories.
        /// </summary>
        static FirestoreReference()
        {
            RegisterAllFactories();
        }

        /// <summary>
        /// Registers document and collection factories for all types in the executing assembly.
        /// </summary>
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
                        FirestoreManager.Logger.Error($"Document path for type {type.Name} is not set.");
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
                        FirestoreManager.Logger.Error($"Collection path for type {type.Name} is not set.");
                        return;
                    }

                    RegisterCollectionFactory(type, args => CreateCollectionReference(path, args));
                }
            }
        }

        /// <summary>
        /// Creates a Firestore document reference.
        /// </summary>
        /// <param name="path">The path template of the document.</param>
        /// <param name="args">The arguments to format the path template.</param>
        /// <returns>A <see cref="DocumentReference"/> instance.</returns>
        private static DocumentReference CreateDocumentReference(string path, params string[] args)
        {
            string formattedPath = string.Format(path, args);
            return _firestore.Document(formattedPath);
        }

        /// <summary>
        /// Creates a Firestore collection reference.
        /// </summary>
        /// <param name="path">The path template of the collection.</param>
        /// <param name="args">The arguments to format the path template.</param>
        /// <returns>A <see cref="CollectionReference"/> instance.</returns>
        private static CollectionReference CreateCollectionReference(string path, params string[] args)
        {
            string formattedPath = string.Format(path, args);
            return _firestore.Collection(formattedPath);
        }

        /// <summary>
        /// Registers a document factory for a specific type.
        /// </summary>
        /// <typeparam name="T">The type to register the factory for.</typeparam>
        /// <param name="factory">The factory function to create a <see cref="DocumentReference"/>.</param>
        public static void RegisterDocumentFactory<T>(Func<string[], DocumentReference> factory)
        {
            RegisterDocumentFactory(typeof(T), factory);
        }

        /// <summary>
        /// Registers a collection factory for a specific type.
        /// </summary>
        /// <typeparam name="T">The type to register the factory for.</typeparam>
        /// <param name="factory">The factory function to create a <see cref="CollectionReference"/>.</param>
        public static void RegisterCollectionFactory<T>(Func<string[], CollectionReference> factory)
        {
            RegisterCollectionFactory(typeof(T), factory);
        }

        /// <summary>
        /// Registers a document factory for a specific type.
        /// </summary>
        /// <param name="type">The type to register the factory for.</param>
        /// <param name="factory">The factory function to create a <see cref="DocumentReference"/>.</param>
        public static void RegisterDocumentFactory(Type type, Func<string[], DocumentReference> factory)
        {
            lock (k_LockObject)
            {
                k_DocumentFactories.AddOrUpdate(type, factory);
            }
        }

        /// <summary>
        /// Registers a collection factory for a specific type.
        /// </summary>
        /// <param name="type">The type to register the factory for.</param>
        /// <param name="factory">The factory function to create a <see cref="CollectionReference"/>.</param>
        public static void RegisterCollectionFactory(Type type, Func<string[], CollectionReference> factory)
        {
            lock (k_LockObject)
            {
                k_CollectionFactories.AddOrUpdate(type, factory);
            }
        }

        /// <summary>
        /// Gets a Firestore document reference for a specific type.
        /// </summary>
        /// <typeparam name="T">The type to get the document reference for.</typeparam>
        /// <param name="args">The arguments to format the path template.</param>
        /// <returns>A <see cref="DocumentReference"/> instance.</returns>
        public static DocumentReference GetDocument<T>(params string[] args)
        {
            return GetDocument(typeof(T), args);
        }

        /// <summary>
        /// Gets a Firestore document reference for a specific type.
        /// </summary>
        /// <param name="type">The type to get the document reference for.</param>
        /// <param name="args">The arguments to format the path template.</param>
        /// <returns>A <see cref="DocumentReference"/> instance.</returns>
        public static DocumentReference GetDocument(Type type, params string[] args)
        {
            lock (k_LockObject)
            {
                if (k_DocumentFactories.TryGetValue(type, out Func<string[], DocumentReference> factory))
                {
                    return factory(args);
                }

                throw FiretaskException.DocumentFactoryNotRegistered(type);
            }
        }

        /// <summary>
        /// Gets the document factory for a specific type.
        /// </summary>
        /// <typeparam name="T">The type to get the document factory for.</typeparam>
        /// <returns>The factory function to create a <see cref="DocumentReference"/>.</returns>
        public static Func<string[], DocumentReference> GetDocumentFactory<T>()
        {
            lock (k_LockObject)
            {
                if (k_DocumentFactories.TryGetValue(typeof(T), out Func<string[], DocumentReference> factory))
                {
                    return factory;
                }

                throw FiretaskException.DocumentFactoryNotRegistered(typeof(T));
            }
        }

        /// <summary>
        /// Gets a Firestore collection reference for a specific type.
        /// </summary>
        /// <typeparam name="T">The type to get the collection reference for.</typeparam>
        /// <param name="args">The arguments to format the path template.</param>
        /// <returns>A <see cref="CollectionReference"/> instance.</returns>
        public static CollectionReference GetCollection<T>(params string[] args)
        {
            return GetCollection(typeof(T), args);
        }

        /// <summary>
        /// Gets a Firestore collection reference for a specific type.
        /// </summary>
        /// <param name="type">The type to get the collection reference for.</param>
        /// <param name="args">The arguments to format the path template.</param>
        /// <returns>A <see cref="CollectionReference"/> instance.</returns>
        public static CollectionReference GetCollection(Type type, params string[] args)
        {
            lock (k_LockObject)
            {
                if (k_CollectionFactories.TryGetValue(type, out Func<string[], CollectionReference> factory))
                {
                    return factory(args);
                }

                throw FiretaskException.CollectionFactoryNotRegistered(type);
            }
        }

        /// <summary>
        /// Gets the collection factory for a specific type.
        /// </summary>
        /// <typeparam name="T">The type to get the collection factory for.</typeparam>
        /// <returns>The factory function to create a <see cref="CollectionReference"/>.</returns>
        public static Func<string[], CollectionReference> GetCollectionFactory<T>()
        {
            lock (k_LockObject)
            {
                if (k_CollectionFactories.TryGetValue(typeof(T), out Func<string[], CollectionReference> factory))
                {
                    return factory;
                }

                throw FiretaskException.CollectionFactoryNotRegistered(typeof(T));
            }
        }
    }
}
