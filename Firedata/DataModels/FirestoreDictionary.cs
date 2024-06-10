using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using Glitch9.IO.Network;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Pool;

namespace Glitch9.Apis.Google.Firestore
{
    /// <summary>
    /// Manages a single Firestore document as a dictionary.
    /// The name of one field is used as the key, and the value is used as the value.
    /// </summary>
    public sealed class FirestoreDictionary<TValue> : Dictionary<string, TValue>, IFirestoreDictionary
    {
        /// <summary>
        /// Unified entry point for initialization.
        /// </summary>
        /// <typeparam name="TRef">The type of the reference, which can be either a DocumentReference or a CollectionReference.</typeparam>
        /// <param name="reference">The reference to the Firestore document or collection.</param>
        /// <param name="onSuccess">An optional callback action that is invoked with a boolean indicating success or failure.</param>
        /// <returns>A task that represents the asynchronous operation, containing the initialized <see cref="FirestoreDictionary{TValue}"/>.</returns>
        public static async UniTask<FirestoreDictionary<TValue>> CreateAsync<TRef>(TRef reference, Action<bool> onSuccess = null) where TRef : class
        {
            using PooledObject<SemaphoreSlim> pooledSemaphore = SemaphoreSlimPool.Get(out SemaphoreSlim semaphore);
            try
            {
                await semaphore.WaitAsync();
                FirestoreDictionary<TValue> dict = await RetrieveFirestoreDataAsync(reference, onSuccess) ?? new FirestoreDictionary<TValue>();
                onSuccess?.Invoke(true);
                return dict;
            }
            catch (Exception e)
            {
                GNLog.Exception(e);
                onSuccess?.Invoke(false);
                return new FirestoreDictionary<TValue>();
            }
            finally
            {
                semaphore.Release();
            }
        }

        private static async UniTask<FirestoreDictionary<TValue>> RetrieveFirestoreDataAsync<TRef>(TRef reference, Action<bool> onSuccess) where TRef : class
        {
            FirestoreDictionary<TValue> dict = null;
            string refName = null;

            if (reference is DocumentReference docRef)
            {
                dict = await docRef.LoadDictionaryAsync<FirestoreDictionary<TValue>>(onSuccess);
                dict._firedataType = FiredataType.Document;
                dict._docRef = docRef;
                refName = docRef.Id;
            }
            else if (reference is CollectionReference colRef)
            {
                dict = await colRef.LoadDictionaryAsync<FirestoreDictionary<TValue>>(onSuccess);
                dict._firedataType = FiredataType.Collection;
                dict._colRef = colRef;
                refName = colRef.Id;
            }
            else
            {
                FirestoreManager.Logger.Error($"{Strings.INVALID_REFERENCE} ({typeof(TValue).Name}).");
            }

            int count = dict?.Count ?? 0;
            LogCount(refName, count);

            return dict;
        }

        private static void LogCount(string refName, int count)
        {
            string colorString = count == 0 ? "red" : "blue";
            FirestoreManager.Logger.Info($"{Strings.LOADED_FROM_REFERENCE} <color=blue>{refName}</color> {Strings.LOADED_ITEMS} <color={colorString}>{count}</color> <color=blue>{typeof(TValue).Name}</color>.");
        }

        public FirestoreDictionary() { }

        public FiredataType FiredataType => _firedataType;
        private FiredataType _firedataType;
        private DocumentReference _docRef;
        private CollectionReference _colRef;

        public DocumentReference GetDocument(params string[] args) => _docRef;
        public CollectionReference GetCollection(params string[] args) => _colRef;

        public TValue GetOrCreate(string key)
        {
            if (TryGetValue(key, out TValue value)) return value;
            TValue newValue = ReflectionUtils.CreateInstance<TValue>(key);
            Add(key, newValue);
            return newValue;
        }

        public Dictionary<string, object> ToFirestoreFormat()
        {
            Dictionary<string, object> dictionary = new();

            foreach (KeyValuePair<string, TValue> item in this)
            {
                if (item.Value is IFiredata fObj)
                {
                    try
                    {
                        string objName = fObj.GetFiredataName();
                        if (string.IsNullOrWhiteSpace(objName))
                        {
                            FirestoreManager.Logger.Error($"{fObj.GetType().Name} {Strings.MISSING_NAME}.");
                            continue;
                        }

                        Dictionary<string, object> obj = fObj.ToFirestoreFormat();
                        if (obj == null)
                        {
                            FirestoreManager.Logger.Warning($"{fObj.GetType().Name} {Strings.CONVERSION_TO_FIRESTORE_OBJECT_FAILED}.");
                            continue;
                        }

                        dictionary.AddOrUpdate(objName, obj);
                    }
                    catch (Exception e)
                    {
                        FirestoreManager.Logger.Error($"{fObj.GetType().Name} {Strings.CONVERSION_TO_FIRESTORE_OBJECT_FAILED}\n{e.Message}\n{e.StackTrace}");
                    }
                }
            }

            return dictionary;
        }

        /// <summary>
        /// Applies data to the dictionary when the key is the document name and the value is the field.
        /// All fields within the Firestore document must have the same format (otherwise, errors will occur).
        /// </summary>
        public IFiredata ToLocalFormat(Dictionary<string, object> data)
        {
            string snapshotId = _docRef?.Id;
            LogCount(snapshotId, data.Count);
            Type valueType = typeof(TValue);

            foreach (KeyValuePair<string, object> item in data)
            {
                if (item.Value == null) continue;

                string fieldName = item.Key;
                if (fieldName == Strings.EDITED_AT_FIELD) continue;

                object fieldValue = item.Value;

                try
                {
                    object newValue = CloudConverter.ToLocalFormat(valueType, fieldName, fieldValue);

                    if (newValue is not TValue value)
                    {
                        FirestoreManager.Logger.Warning($"{fieldName}({fieldValue.GetType().Name}) {Strings.CONVERSION_TO_TYPE_FAILED} {typeof(TValue).Name}.");
                        continue;
                    }

                    this.AddOrUpdate(fieldName, value);
                }
                catch (Exception e)
                {
                    FirestoreManager.Logger.Error($"{fieldName}({fieldValue.GetType().Name}) {Strings.CONVERSION_TO_TYPE_FAILED} {typeof(TValue).Name}.\n{e.Message}\n{e.StackTrace}");
                }
            }

            return this;
        }

        /// <summary>
        /// Applies data to the dictionary when the key is the collection name and the value is the document.
        /// </summary>
        public IFiredata SetSnapshots(QuerySnapshot snapshots)
        {
            string snapshotId = $"{typeof(TValue).Name} {Strings.QUERY}";
            LogCount(snapshotId, snapshots.Count);

            foreach (DocumentSnapshot snapshot in snapshots)
            {
                string documentName = snapshot.Id;
                TValue data = ReflectionUtils.CreateInstance<TValue>(documentName);

                if (data == null) continue;

                if (data is IFirestoreDocument firestoreDocument)
                {
                    firestoreDocument.ToLocalFormat(snapshot);
                }
                else
                {
                    FirestoreManager.Logger.Error($"{typeof(TValue).Name} {Strings.NOT_A_FIRESTORE_DOCUMENT}.");
                    continue;
                }

                this.AddOrUpdate(documentName, data);
            }

            return this;
        }


        /// <summary>
        /// Contains constant string values for logging and messages.
        /// </summary>
        private static class Strings
        {
            internal const string EDITED_AT_FIELD = "edited_at";
            internal const string INVALID_REFERENCE = "The reference is invalid or null";
            internal const string LOADED_FROM_REFERENCE = "Loaded items from reference";
            internal const string LOADED_ITEMS = "items of type";
            internal const string MISSING_NAME = "is missing a name";
            internal const string CONVERSION_TO_FIRESTORE_OBJECT_FAILED = "could not be converted to Firestore object";
            internal const string CONVERSION_TO_TYPE_FAILED = "could not be converted to type";
            internal const string QUERY = "query";
            internal const string NOT_A_FIRESTORE_DOCUMENT = "is not a Firestore document";
        }
    }
}
