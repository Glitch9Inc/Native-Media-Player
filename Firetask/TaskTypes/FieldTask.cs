using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using System.Collections.Generic;
using System.Linq;


namespace Glitch9.Apis.Google.Firestore.Tasks
{
    public sealed class FieldTask : FiretaskBase
    {
        public string FieldName
        {
            get
            {
                if (Data.Count == 0) return string.Empty;
                return Data.Keys.First();
            }
            set
            {
                object fieldValue = null;
                if (Data.Count > 0)
                {
                    fieldValue = Data.Values.First();
                    Data.Clear();
                }
                Data.Add(value, fieldValue);
            }
        }

        public object FieldValue
        {
            get
            {
                if (Data.Count == 0) return null;
                return Data.Values.First();
            }
            set
            {
                string key = string.Empty;
                if (Data.Count > 0)
                {
                    key = Data.Keys.First();
                    Data.Clear();
                }
                Data.Add(key, value);
            }
        }

        public FieldTask SetData(string key, object data)
        {
            this.FieldName = key;
            this.FieldValue = ConvertData(data);
            return this;
        }

        public FieldTask DeleteData(string fieldName)
        {
            this.FieldName = fieldName;
            this.FieldValue = global::Firebase.Firestore.FieldValue.Delete;
            return this;
        }

        public FieldTask SetData<T>(T iField) where T : IFiredata
        {
            if (iField == null)
            {
                FirestoreManager.Logger.Error("This firestore task's data is null");
                return this;
            }

            return SetData(iField.GetFiredataName(), iField.ToFirestoreFormat());
        }

        public override IResult ValidateTask()
        {
            if (Document == null)
            {
                string message = "This firestore task's document reference is null";
                FirestoreManager.Logger.Error(message);
                IResult result = Result.Fail(message);
                OnComplete?.Invoke(result);
                return result;
            }

            if (string.IsNullOrWhiteSpace(FieldName))
            {
                string message = "This firestore task's field name is null or empty";
                FirestoreManager.Logger.Error(message);
                IResult result = Result.Fail(message);
                OnComplete?.Invoke(result);
                return result;
            }

            if (TaskAction != FiretaskAction.Delete && FieldValue == null)
            {
                string message = "This firestore task's field value is null";
                FirestoreManager.Logger.Error(message);
                IResult result = Result.Fail(message);
                OnComplete?.Invoke(result);
                return result;
            }

            return Result.Success();
        }


        protected override async UniTask OnExecuteAsync()
        {
            if (FieldValue == null) return;

            FieldName = FieldName.ToSnakeCase();

            // TODO:
            // 버그로 UnixTime이 그대로 오는 경우가 있는데
            // 도저히 지금은 찾을 수 없어서 일단 이렇게 처리
            // 2024-01-24 Munchkin 
            if (FieldValue is UnixTime unixTime)
            {
                FieldValue = (long)unixTime.Value;
            }

            switch (TaskAction)
            {
                case FiretaskAction.MergeAll:
                    await Document.SetAsync(new Dictionary<string, object> { { FieldName, FieldValue } }, SetOptions.MergeAll);
                    break;
                case FiretaskAction.Overwrite:
                    await Document.SetAsync(new Dictionary<string, object> { { FieldName, FieldValue } });
                    break;
                case FiretaskAction.Update:
                    await Document.UpdateAsync(FieldName, FieldValue);
                    break;
                case FiretaskAction.Delete:
                    await Document.UpdateAsync(FieldName, global::Firebase.Firestore.FieldValue.Delete);
                    break;
            }
        }

        public FieldTask(DocumentReference document, Dictionary<string, object> data = null)
        {
            this.Document = document;
            if (data != null)
            {
                foreach (KeyValuePair<string, object> item in data)
                {
                    this.FieldName = item.Key;
                    this.FieldValue = item.Value;
                }
            }
            Firetask.Tasks.Add(this);
        }

        public FieldTask(DocumentReference document, string fieldName, object fieldValue)
        {
            this.Document = document;
            this.FieldName = fieldName;
            this.FieldValue = fieldValue;
            Firetask.Tasks.Add(this);
        }
    }
}