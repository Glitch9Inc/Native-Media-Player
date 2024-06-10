using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using System.Collections.Generic;

namespace Glitch9.Apis.Google.Firestore.Tasks
{
    public sealed class DocumentTask : FiretaskBase
    {
        public override bool Validate()
        {
            if (Document.LogIfNull())
            {
                OnComplete?.Invoke(Result.Fail("Document is null."));
                return false;
            }

            if (Data == null && TaskAction != FiretaskAction.Delete)
            {
                GNLog.Error("Firestore에 저장하려는 데이터가 null입니다.");
                OnComplete?.Invoke(Result.Fail("Firestore에 저장하려는 데이터가 null입니다."));
                return false;
            }

            return true;
        }

        protected override async UniTask OnExecuteAsync()
        {
            if (Data.Count == 0) return;

            switch (TaskAction)
            {
                case FiretaskAction.MergeAll:
                    await Document.SetAsync(Data, SetOptions.MergeAll);
                    break;
                case FiretaskAction.Overwrite:
                    await Document.SetAsync(Data);
                    break;
                case FiretaskAction.Update:
                    await Document.UpdateAsync(Data);
                    break;
                case FiretaskAction.Delete:
                    await Document.DeleteAsync();
                    break;
            }
        }

        public DocumentTask(DocumentReference document, Dictionary<string, object> data = null)
        {
            this.Document = document;
            this.Data = ConvertData(data);
            Firetask.Tasks.Add(this);
        }
    }
}