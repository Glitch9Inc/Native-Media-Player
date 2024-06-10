using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Threading;
using Glitch9.IO.Network;

// ReSharper disable StaticMemberInGenericType

namespace Glitch9.Apis.Google.Firestore
{
    /// <summary>
    /// 1개의 FirestoreDocument를 Dictionary로 관리하는 클래스
    /// 필드 1개의 Name은 Key로, Value는 Value로 사용된다.
    /// </summary>
    public sealed class FirestoreDictionary<TValue> : Dictionary<string, TValue>, IFirestoreDictionary
    //where TValue : class, IFirestoreEntity, new()
    {
        private static readonly SemaphoreSlim _semaphore = new(1, 1);

        // Unified entry point for initialization
        public static async UniTask<FirestoreDictionary<TValue>> CreateAsync<TRef>(TRef reference, Action<bool> onSuccess = null) where TRef : class
        {
            try
            {
                await _semaphore.WaitAsync();
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
                _semaphore.Release();
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
                GNLog.Error($"GNFirestoreDictionary({typeof(TValue).Name})의 reference가 잘못되었거나 null입니다.");
            }

            int count = dict?.Count ?? 0;
            LogInstanceDetails(count, refName);

            return dict;
        }

        private static void LogInstanceDetails(int count, string refName)
        {
            string colorString = count == 0 ? "red" : "blue";
            GNLog.Info($"<color=blue>{refName}</color>에서 <color={colorString}>{count}개</color>의 {typeof(TValue).Name}를 로드했습니다");
        }

        public FirestoreDictionary() { }

        public FiredataType FiredataType => _firedataType;
        private FiredataType _firedataType;
        private DocumentReference _docRef;
        private CollectionReference _colRef;
        public DocumentReference GetDocument(string email = null) => _docRef;
        public CollectionReference GetCollection(string email = null) => _colRef;


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
                        string objName = fObj.GetEntityName();
                        if (string.IsNullOrWhiteSpace(objName))
                        {
                            GNLog.Error($"{fObj.GetType().Name}의 이름이 없습니다.");
                            continue;
                        }

                        Dictionary<string, object> obj = fObj.ToFirestoreFormat();
                        if (obj == null)
                        {
                            GNLog.Warning($"{fObj.GetType().Name}를 FirestoreObject로 변환할 수 없습니다.");
                            continue;
                        }

                        dictionary.AddOrUpdate(objName, obj);
                    }
                    catch (Exception e)
                    {
                        GNLog.Error($"{fObj.GetType().Name}를 FirestoreObject로 변환하는데 실패했습니다.\n{e.Message}\n{e.StackTrace}");
                    }
                }
            }

            return dictionary;
        }



        /// <summary>
        /// 이 Dictionary의 Key가 Document이름, Value가 Field일 경우 사용한다.
        /// FirestoreDocument안에 Field의 포멧이 전부 동일해야한다. (아니면 에러가 난다)
        /// </summary>
        public IFiredata SetMap(Dictionary<string, object> data)
        {
            string snapshotId = _docRef?.Id;
            LogDictionaryDetails(snapshotId, data.Count);
            Type valueType = typeof(TValue);

            foreach (KeyValuePair<string, object> item in data)
            {
                if (item.Value == null) continue;

                string fieldName = item.Key;
                if (fieldName == "edited_at") continue;
                //string pascalCasedFieldName = fieldName.ToPascalCase();
                object fieldValue = item.Value;

                try
                {
                    object newValue = CloudConverter.ToLocalFormat(valueType, fieldName, fieldValue);

                    if (newValue is not TValue value)
                    {
                        GNLog.Warning($"{fieldName}({fieldValue.GetType().Name})를 {typeof(TValue).Name} 타입으로 변환할 수 없습니다.");
                        continue;
                    }

                    this.AddOrUpdate(fieldName, value);
                }
                catch (Exception e)
                {
                    GNLog.Error($"{fieldName}({fieldValue.GetType().Name})를 {typeof(TValue).Name} 타입으로 변환하는데 실패했습니다.\n{e.Message}\n{e.StackTrace}");
                }
            }

            return this;
        }


        /// <summary>
        /// 이 Dictionary의 Key가 Collection이름, Value가 Document일 경우 사용한다.
        /// </summary>
        public IFiredata SetSnapshots(QuerySnapshot snapshots)
        {
            string snapshotId = $"{typeof(TValue).Name}의 쿼리";
            LogDictionaryDetails(snapshotId, snapshots.Count);

            foreach (DocumentSnapshot snapshot in snapshots)
            {
                string documentName = snapshot.Id;

                // 타입에 맞는 인스턴스를 생성하는데, id로 documentName을 사용한다.
                // 예를들어 Character콜렉션 안에 Tuto라는 Document가 있으면, "Tuto"를 인자로 넘긴다.
                TValue data = ReflectionUtils.CreateInstance<TValue>(documentName);

                if (data == null) continue;

                if (data is IFirestoreDocument firestoreDocument)
                {
                    firestoreDocument.SetSnapshot(snapshot);
                }
                else
                {
                    GNLog.Error($"{typeof(TValue).Name}는 FirestoreDocument가 아닙니다.");
                    continue;
                }

                this.AddOrUpdate(documentName, data);
            }

            return this;
        }

        private void LogDictionaryDetails(string snapshotId, int count)
        {
            if (count == 0)
            {
                GNLog.Info($"<color=blue>{snapshotId}</color>에 <color=blue>{typeof(TValue).Name}</color>가 없습니다.");
            }
            else
            {
                GNLog.Info($"<color=blue>{snapshotId}</color>에서 <color=blue>{count}개</color>의 <color=blue>{typeof(TValue).Name}</color>를 찾았습니다.");
            }
        }
    }
}
