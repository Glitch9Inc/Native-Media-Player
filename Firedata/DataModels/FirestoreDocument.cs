using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using System;
using System.Threading;
using UnityEngine.Pool;

namespace Glitch9.Apis.Google.Firestore
{
    /// <summary>
    /// Abstract base class for Firestore documents that provides common functionality and interaction with Firestore.
    /// </summary>
    /// <typeparam name="TSelf">The type of the derived class.</typeparam>
    public abstract class FirestoreDocument<TSelf> : Firedata<TSelf>, IFirestoreDocument, IMapEntry
        where TSelf : FirestoreDocument<TSelf>, new()
    {
        /// <summary>
        /// Gets the key associated with this document. The key can represent the document name or collection name.
        /// </summary>
        public abstract string Key { get; }

        private static TSelf _playerInstance;

        /// <summary>
        /// Gets the current user's instance of the document.
        /// </summary>
        public static TSelf Player
        {
            get
            {
                if (_playerInstance == null)
                {
                    FirestoreManager.LogNotInitializedYet(typeof(TSelf));
                    return null;
                }
                return _playerInstance;
            }
            private set => _playerInstance = value;
        }

        /// <summary>
        /// Determines whether this instance is the current user's instance.
        /// </summary>
        /// <returns>True if this instance is the current user's instance; otherwise, false.</returns>
        public bool IsPlayerInstance() => Player == this;

        /// <summary>
        /// Gets the current user's instance asynchronously, typically used in the editor when Application.isPlaying is false.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation, containing the current user's instance.</returns>
        public static async UniTask<TSelf> GetMyInstanceAsync()
        {
            if (_playerInstance == null)
            {
                await InitializeAsync();
            }
            return Player;
        }

        /// <summary>
        /// Initializes the current user's instance asynchronously.
        /// </summary>
        /// <param name="onSuccess">An optional callback action that is invoked with a boolean indicating success or failure.</param>
        public static async UniTask InitializeAsync(Action<bool> onSuccess = null)
        {
            using PooledObject<SemaphoreSlim> pooledSemaphore = SemaphoreSlimPool.Get(out SemaphoreSlim semaphore);
            try
            {
                await semaphore.WaitAsync();

                if (_playerInstance == null)
                {
                    _playerInstance = await FiredataLoader.LoadDocumentAsync<TSelf>();
                }

                onSuccess?.Invoke(true);
            }
            catch (Exception e)
            {
                GNLog.Exception(e);
                onSuccess?.Invoke(false);
            }
            finally
            {
                _playerInstance ??= ReflectionUtils.CreateInstance<TSelf>();
                semaphore.Release();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FirestoreDocument{TSelf}"/> class.
        /// </summary>
        protected FirestoreDocument() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FirestoreDocument{TSelf}"/> class with the specified document name.
        /// </summary>
        /// <param name="documentName">The name of the document.</param>
        protected FirestoreDocument(string documentName) : base(documentName) { }

        /// <summary>
        /// Applies data from a Firestore document snapshot to this document.
        /// </summary>
        /// <param name="snapshot">The document snapshot containing the Firestore data.</param>
        /// <returns>An <see cref="IFiredata"/> instance with the applied data.</returns>
        public IFiredata ToLocalFormat(DocumentSnapshot snapshot)
        {
            if (snapshot == null) return null;
            System.Collections.Generic.Dictionary<string, object> map = snapshot.ToDictionary();
            if (map == null) return null;
            ToLocalFormat(map);
            return this;
        }
    }
}
