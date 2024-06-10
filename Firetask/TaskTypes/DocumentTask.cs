using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using System.Collections.Generic;

namespace Glitch9.Apis.Google.Firestore.Tasks
{
    public sealed class DocumentTask : FiretaskBase
    {
        public override IResult ValidateTask()
        {
            if (Document == null)
            {
                string message = "FirestoreDocument is null.";
                FirestoreManager.Logger.Error(message);
                IResult result = Result.Fail(message);
                OnComplete?.Invoke(result);
                return result;
            }

            if (Data == null && TaskAction != FiretaskAction.Delete)
            {
                string message = "Data to be stored in Firestore is null.";
                FirestoreManager.Logger.Error(message);
                IResult result = Result.Fail(message);
                OnComplete?.Invoke(result);
                return result;
            }

            return Result.Success();
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