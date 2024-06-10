using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using System;
using System.Threading;
// ReSharper disable StaticMemberInGenericType

namespace Glitch9.Apis.Google.Firestore
{
    public abstract class FirestoreDocument<TSelf> : Firedata<TSelf>, IFirestoreDocument, IMapEntry
        where TSelf : FirestoreDocument<TSelf>, new()
    {
        public abstract string Key { get; }
        private static readonly SemaphoreSlim _semaphore = new(1, 1);
        private static bool _isInitialized = false;

        private static TSelf _playerInstance;
        public static TSelf Player              // 현재 사용자의 인스턴스
        {
            get
            {
                if (!_isInitialized)
                {
                    GNLog.Warning($"{typeof(TSelf).Name}가 아직 초기화되지 않았습니다.");
                    return null;
                }
                return _playerInstance ??= ReflectionUtils.CreateInstance<TSelf>();
            }
            private set => _playerInstance = value;
        }

        public bool IsPlayerInstance() => Player == this; // Property로 사용하면 Reflection에서 인식함으로 Method로 사용

        /// <summary>
        /// 주로 에디터에서 Application.isPlaying이 false일때 인스턴스를 불러오기 위해 사용
        /// </summary>
        public static async UniTask<TSelf> GetMyInstanceAsync()
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }
            return Player;
        }

        public static async UniTask InitializeAsync(Action<bool> onSuccess = null)
        {
            try
            {
                await _semaphore.WaitAsync();

                if (!_isInitialized)
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
                _isInitialized = true;
                _semaphore.Release();
            }
        }

        protected FirestoreDocument() : base() { }
        protected FirestoreDocument(string documentName) : base(documentName) { }

        public IFiredata SetSnapshot(DocumentSnapshot snapshot)
        {
            if (snapshot == null) return null;
            System.Collections.Generic.Dictionary<string, object> map = snapshot.ToDictionary();
            if (map == null) return null;
            SetMap(map);
            return this;
        }

        public override DocumentReference GetDocument(string email = null)
        {
            IMapEntry mapEntry = this;
            string documentName = mapEntry.Key;
            DocumentReference document = FirestoreReference.GetDocument(GetType(), email, documentName);
            if (document == null)
            {
                if (documentName == null) GNLog.Error($"{GetType().Name}의 DocumentReference를 가져오는데 실패했습니다.");
                else GNLog.Error($"{GetType().Name}의 DocumentReference를 가져오는데 실패했습니다. DocumentName: {documentName}");
                return null;
            }

            return document;
        }
    }
}