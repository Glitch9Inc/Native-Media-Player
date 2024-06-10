using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using System;
using UnityEngine;

namespace Glitch9.Apis.Google.Firestore
{
    /// <summary>
    /// 서버를 통해 데이터를 저장하고 불러오는 기능을 제공한다.
    /// </summary>
    public static class FiredataLoader
    {
        /// <summary>
        /// Firestore Document를 로드하여 1개의 class에 저장한다.
        /// </summary>
        public static async UniTask LoadDocument<T>(Action<T> onLoaded, Action<bool> onSuccess = null, string email = null)
            where T : IFirestoreDocument
        {
            DocumentReference docRef = FirestoreReference.GetDocument<T>(email);
            T result = await docRef.LoadDocumentAsync<T>(onSuccess);
            onLoaded?.Invoke(result);
        }

        /// <summary>
        /// Firestore Document를 로드하여 1개의 class에 저장한다.
        /// </summary>
        public static async UniTask<T> LoadDocumentAsync<T>(Action<bool> onSuccess = null, string email = null)
            where T : IFirestoreDocument
        {
            DocumentReference docRef = FirestoreReference.GetDocument<T>(email);
            return await LoadDocumentAsync<T>(docRef, onSuccess);
        }

        private static async UniTask<T> LoadDocumentAsync<T>(this DocumentReference docRef, Action<bool> onSuccess = null)
            where T : IFirestoreDocument
        {
            T instance = ReflectionUtils.CreateInstance<T>();

            if (docRef == null)
            {
                Debug.LogWarning($"{typeof(T).Name}의 DocumentReference가 없습니다");
                onSuccess?.Invoke(false);
                return instance;
            }

            try
            {
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
                if (snapshot == null || !snapshot.Exists)
                {
                    //Debug.LogWarning($"{typeof(T).Name}의 Document가 존재하지만 스냅샷을 찾을 수 없습니다");
                    onSuccess?.Invoke(false);
                    return default;
                }

                instance.SetSnapshot(snapshot);
                onSuccess?.Invoke(true);
                return instance;
            }
            catch (Exception e)
            {
                GNLog.Error($"{typeof(T).Name}를 로드하는데 실패했습니다.\n{e.Message}\n{e.StackTrace}");
                onSuccess?.Invoke(false);
            }

            return instance;
        }

        public static async UniTask<TDict> LoadDictionaryAsync<TDict>(this DocumentReference docRef, Action<bool> onSuccess = null)
            where TDict : IFirestoreDictionary, new()
        {
            Type[] types = typeof(TDict).GetGenericArguments();
            string valueTypeName = types[0].Name;
            TDict instance = ReflectionUtils.CreateInstance<TDict>();

            if (docRef == null)
            {
                Debug.LogWarning($"{valueTypeName}의 DocumentReference가 없습니다");
                onSuccess?.Invoke(false);
                return instance;
            }

            try
            {
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
                if (snapshot == null || !snapshot.Exists)
                {
                    //Debug.LogWarning($"{valueTypeName}의 Document가 존재하지만 스냅샷을 찾을 수 없습니다");
                    onSuccess?.Invoke(false);
                    return instance;
                }

                instance.SetMap(snapshot.ToDictionary());
                onSuccess?.Invoke(true);
                return instance;
            }
            catch (Exception e)
            {
                GNLog.Error($"{valueTypeName}를 로드하는데 실패했습니다.\n{e.Message}\n{e.StackTrace}");
                onSuccess?.Invoke(false);
            }

            return instance;
        }

        public static async UniTask<TDict> LoadDictionaryAsync<TDict>(this Query query, Action<bool> onSuccess = null)
          where TDict : IFirestoreDictionary, new()
        {
            Type[] types = typeof(TDict).GetGenericArguments();
            string valueTypeName = types[0].Name;
            TDict instance = ReflectionUtils.CreateInstance<TDict>();

            if (query == null)
            {
                Debug.LogWarning($"{valueTypeName}의 CollectionReference가 없습니다");
                onSuccess?.Invoke(false);
                return instance;
            }

            try
            {
                QuerySnapshot snapshots = await query.GetSnapshotAsync();
                if (snapshots == null)
                {
                    //Debug.LogWarning($"{valueTypeName}의 Collection이 존재하지만 스냅샷을 찾을 수 없습니다");
                    onSuccess?.Invoke(false);
                    return instance;
                }

                instance.SetSnapshots(snapshots);
                onSuccess?.Invoke(true);
                return instance;
            }
            catch (Exception e)
            {
                GNLog.Error($"{valueTypeName}를 로드하는데 실패했습니다.\n{e.Message}\n{e.StackTrace}");
                onSuccess?.Invoke(false);
            }

            return instance;
        }

        public static async UniTask<T> FindFieldAsync<T>(this CollectionReference colRef, string fieldName, string valueToFind)
            where T : class, IFirestoreDocument
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
                        result.SetSnapshot(document);
                        return (T)result;
                    }
                }
            }
            catch (Exception e)
            {
                GNLog.Error($"<color=blue>{colRef.Id}</color>의 {fieldName}을 찾는데 실패했습니다.\n{e.Message}\n{e.StackTrace}");
            }

            return null;
        }

        public static async UniTask<T> FindFieldAsync<T>(this DocumentReference docRef, string fieldName, string valueToFind)
            where T : class, IFirestoreDocument
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
                        result.SetSnapshot(document);
                        return (T)result;
                    }
                }
            }
            catch (Exception e)
            {
                GNLog.Error($"<color=blue>{docRef.Id}</color>의 {fieldName}을 찾는데 실패했습니다.\n{e.Message}\n{e.StackTrace}");
            }

            return null;
        }
    }
}
