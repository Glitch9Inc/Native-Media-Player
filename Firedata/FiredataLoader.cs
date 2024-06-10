using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using System;
using UnityEngine;


namespace Glitch9.Apis.Google.Firestore
{
    /// <summary>
    /// Provides functionality to save and load data via the server.
    /// </summary>
    public static class FiredataLoader
    {
        /// <summary>
        /// Loads a Firestore Document and stores it in a single class.
        /// </summary>
        /// <typeparam name="T">Type that implements the IFirestoreDocument interface.</typeparam>
        /// <param name="onLoaded">Callback action that receives the loaded document instance.</param>
        /// <param name="onSuccess">Optional callback action that receives a boolean indicating success or failure.</param>
        /// <param name="email">Optional email parameter for identifying the document.</param>
        public static async UniTask LoadDocument<T>(Action<T> onLoaded, Action<bool> onSuccess = null, string email = null) where T : IFirestoreDocument
        {
            DocumentReference docRef = FirestoreReference.GetDocument<T>(email);
            T result = await docRef.LoadDocumentAsync<T>(onSuccess);
            onLoaded?.Invoke(result);
        }

        /// <summary>
        /// Loads a Firestore Document and stores it in a single class asynchronously.
        /// </summary>
        /// <typeparam name="T">Type that implements the IFirestoreDocument interface.</typeparam>
        /// <param name="onSuccess">Optional callback action that receives a boolean indicating success or failure.</param>
        /// <param name="email">Optional email parameter for identifying the document.</param>
        /// <returns>The loaded document instance.</returns>
        public static async UniTask<T> LoadDocumentAsync<T>(Action<bool> onSuccess = null, string email = null) where T : IFirestoreDocument
        {
            DocumentReference docRef = FirestoreReference.GetDocument<T>(email);
            return await LoadDocumentAsync<T>(docRef, onSuccess);
        }

        /// <summary>
        /// Extension method for loading a Firestore Document and storing it in a single class asynchronously.
        /// </summary>
        /// <typeparam name="T">Type that implements the IFirestoreDocument interface.</typeparam>
        /// <param name="docRef">Document reference to load.</param>
        /// <param name="onSuccess">Optional callback action that receives a boolean indicating success or failure.</param>
        /// <returns>The loaded document instance.</returns>
        private static async UniTask<T> LoadDocumentAsync<T>(this DocumentReference docRef, Action<bool> onSuccess = null) where T : IFirestoreDocument
        {
            T instance = ReflectionUtils.CreateInstance<T>();

            if (docRef == null)
            {
                Debug.LogWarning($"{typeof(T).Name}{Strings.DocumentReferenceNotFound}");
                onSuccess?.Invoke(false);
                return instance;
            }

            try
            {
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
                if (snapshot == null || !snapshot.Exists)
                {
                    onSuccess?.Invoke(false);
                    return default;
                }

                instance.ToLocalFormat(snapshot);
                onSuccess?.Invoke(true);
                return instance;
            }
            catch (Exception e)
            {
                FirestoreManager.Logger.Error($"{typeof(T).Name}{Strings.FailedToLoadDocument}\n{e.Message}\n{e.StackTrace}");
                onSuccess?.Invoke(false);
            }

            return instance;
        }

        /// <summary>
        /// Loads a Firestore Document and stores it in a dictionary asynchronously.
        /// </summary>
        /// <typeparam name="TDict">Type that implements the IFirestoreDictionary interface.</typeparam>
        /// <param name="docRef">Document reference to load.</param>
        /// <param name="onSuccess">Optional callback action that receives a boolean indicating success or failure.</param>
        /// <returns>The loaded dictionary instance.</returns>
        public static async UniTask<TDict> LoadDictionaryAsync<TDict>(this DocumentReference docRef, Action<bool> onSuccess = null) where TDict : IFirestoreDictionary, new()
        {
            Type[] types = typeof(TDict).GetGenericArguments();
            string valueTypeName = types[0].Name;
            TDict instance = ReflectionUtils.CreateInstance<TDict>();

            if (docRef == null)
            {
                Debug.LogWarning($"{valueTypeName}{Strings.DocumentReferenceNotFound}");
                onSuccess?.Invoke(false);
                return instance;
            }

            try
            {
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
                if (snapshot == null || !snapshot.Exists)
                {
                    onSuccess?.Invoke(false);
                    return instance;
                }

                instance.ToLocalFormat(snapshot.ToDictionary());
                onSuccess?.Invoke(true);
                return instance;
            }
            catch (Exception e)
            {
                FirestoreManager.Logger.Error($"{valueTypeName}{Strings.FailedToLoadDocument}\n{e.Message}\n{e.StackTrace}");
                onSuccess?.Invoke(false);
            }

            return instance;
        }

        /// <summary>
        /// Loads multiple Firestore documents matching a query and stores them in a dictionary asynchronously.
        /// </summary>
        /// <typeparam name="TDict">Type that implements the IFirestoreDictionary interface.</typeparam>
        /// <param name="query">Firestore query to execute.</param>
        /// <param name="onSuccess">Optional callback action that receives a boolean indicating success or failure.</param>
        /// <returns>The loaded dictionary instance.</returns>
        public static async UniTask<TDict> LoadDictionaryAsync<TDict>(this Query query, Action<bool> onSuccess = null) where TDict : IFirestoreDictionary, new()
        {
            Type[] types = typeof(TDict).GetGenericArguments();
            string valueTypeName = types[0].Name;
            TDict instance = ReflectionUtils.CreateInstance<TDict>();

            if (query == null)
            {
                Debug.LogWarning($"{valueTypeName}{Strings.CollectionReferenceNotFound}");
                onSuccess?.Invoke(false);
                return instance;
            }

            try
            {
                QuerySnapshot snapshots = await query.GetSnapshotAsync();
                if (snapshots == null)
                {
                    onSuccess?.Invoke(false);
                    return instance;
                }

                instance.SetSnapshots(snapshots);
                onSuccess?.Invoke(true);
                return instance;
            }
            catch (Exception e)
            {
                FirestoreManager.Logger.Error($"{valueTypeName}{Strings.FailedToLoadDocument}\n{e.Message}\n{e.StackTrace}");
                onSuccess?.Invoke(false);
            }

            return instance;
        }

        /// <summary>
        /// Finds a document in a collection where a specified field matches a given value.
        /// </summary>
        /// <typeparam name="T">Type that implements the IFirestoreDocument interface.</typeparam>
        /// <param name="colRef">Collection reference to search.</param>
        /// <param name="fieldName">Field name to search by.</param>
        /// <param name="valueToFind">Value to match.</param>
        /// <returns>The found document instance, or null if not found.</returns>
        public static async UniTask<T> FindFieldAsync<T>(this CollectionReference colRef, string fieldName, string valueToFind) where T : class, IFirestoreDocument
        {
            try
            {
                Query query = colRef.WhereEqualTo(fieldName, valueToFind);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();
                if (snapshot != null)
                {
                    foreach (DocumentSnapshot document in snapshot.Documents)
                    {
                        IFirestoreDocument result = ReflectionUtils.CreateInstance<T>();
                        result.ToLocalFormat(document);
                        return (T)result;
                    }
                }
            }
            catch (Exception e)
            {
                FirestoreManager.Logger.Error($"{colRef.Id}{Strings.FailedToFindField}{fieldName}\n{e.Message}\n{e.StackTrace}");
            }

            return null;
        }

        /// <summary>
        /// Finds a subdocument in a document's collection where a specified field matches a given value.
        /// </summary>
        /// <typeparam name="T">Type that implements the IFirestoreDocument interface.</typeparam>
        /// <param name="docRef">Document reference to search within.</param>
        /// <param name="fieldName">Field name to search by.</param>
        /// <param name="valueToFind">Value to match.</param>
        /// <returns>The found document instance, or null if not found.</returns>
        public static async UniTask<T> FindFieldAsync<T>(this DocumentReference docRef, string fieldName, string valueToFind) where T : class, IFirestoreDocument
        {
            try
            {
                Query query = docRef.Collection(docRef.Id).WhereEqualTo(fieldName, valueToFind);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();
                if (snapshot != null)
                {
                    foreach (DocumentSnapshot document in snapshot.Documents)
                    {
                        IFirestoreDocument result = ReflectionUtils.CreateInstance<T>();
                        result.ToLocalFormat(document);
                        return (T)result;
                    }
                }
            }
            catch (Exception e)
            {
                FirestoreManager.Logger.Error($"{docRef.Id}{Strings.FailedToFindField}{fieldName}\n{e.Message}\n{e.StackTrace}");
            }

            return null;
        }

        /// <summary>
        /// Class containing constant string values for logging and messages.
        /// </summary>
        private static class Strings
        {
            internal const string DocumentReferenceNotFound = "'s DocumentReference not found.";
            internal const string FailedToLoadDocument = " failed to load document.";
            internal const string CollectionReferenceNotFound = "'s CollectionReference not found.";
            internal const string FailedToFindField = " failed to find field ";
        }
    }
}

