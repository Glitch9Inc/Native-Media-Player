using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using System.Collections.Generic;

namespace Glitch9.Apis.Google.Firestore.Tasks
{
    public sealed class CollectionTask : FiretaskBase
    {
        public override bool Validate()
        {
            if (Collection == null)
            {
                GNLog.Error("This firestore task's collection reference is null");
                OnComplete?.Invoke(Result.Fail("This firestore task's collection reference is null"));
                return false;
            }

            if (TaskAction == FiretaskAction.AddDocuments && Data == null)
            {
                GNLog.Error("This firestore task's data is null");
                OnComplete?.Invoke(Result.Fail("This firestore task's data is null"));
                return false;
            }

            return true;
        }

        protected override async UniTask OnExecuteAsync()
        {
            if (Data == null || Data.Count == 0) return;

            if (TaskAction == FiretaskAction.AddDocuments)
            {
                // await colRef.AddAsync(data); // add document data
                // 위와 같은 방식으로하면 Document의 이름을 지정할 수 없다.
                // 아래와 같이 Document를 생성하고 SetAsync을 호출해야 한다.
                foreach (KeyValuePair<string, object> item in Data)
                {
                    await Collection.Document(item.Key).SetAsync(item.Value);
                }
            }
            else if (TaskAction == FiretaskAction.Delete)
            {
                await Collection.Document().DeleteAsync();
            }
        }

        public CollectionTask(CollectionReference collection, Dictionary<string, object> data = null)
        {
            this.Collection = collection;
            this.Data = ConvertData(data);
            Firetask.Tasks.Add(this);
        }
    }
}