using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using Glitch9.Apis.Google.Firebase;
using Glitch9.IO.Network;
using System;
using System.Collections.Generic;


namespace Glitch9.Apis.Google.Firestore.Tasks
{
    public abstract class FiretaskBase
    {
        public DocumentReference Document { get; protected set; }
        public CollectionReference Collection { get; protected set; }
        public Dictionary<string, object> Data { get; protected set; } = new();
        public FiretaskAction TaskAction { get; set; } = FiretaskAction.MergeAll;
        public Action<IResult> OnComplete { get; set; }

        public abstract IResult ValidateTask();
        protected abstract UniTask OnExecuteAsync();
        public async void Execute(Action<IResult> onComplete = null) => await ExecuteAsync(onComplete);
        public async UniTask<IResult> ExecuteAsync(Action<IResult> onComplete = null)
        {
            if (!FirebaseManager.CheckFirebaseAuth()) return Result.Fail("Invalid Firebase Auth");

            IResult validation = ValidateTask();
            if (validation.IsFailure) return validation;

            if (onComplete != null) OnComplete += onComplete;
            bool success = false;
            int retryCount = 0;
            int maxRetries = 3; // You can set the maximum number of retries
            TimeSpan retryDelay = TimeSpan.FromSeconds(3); // Delay between retries
            IResult result = Result.Fail("Task failed to execute");

            while (!success)
            {
                try
                {
                    await OnExecuteAsync();

                    success = true; // If operation succeeds
                    result = Result.Success();

                    if (Firetask.FallbackTasks.Count > 0)
                    {
                        foreach (FiretaskBase task in Firetask.FallbackTasks)
                        {
                            try
                            {
                                await task.ExecuteAsync();
                                Firetask.FallbackTasks.Remove(task);
                            }
                            catch (Exception ex)
                            {
                                GNLog.Exception(ex);
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    retryCount++;

                    if (retryCount >= maxRetries)
                    {
                        GNLog.Exception(ex);
                        result = new Error(ex);
                        break;
                    }

                    await UniTask.Delay(retryDelay); // Wait Before retrying
                }
                finally
                {
                    OnComplete?.Invoke(result);
                }
            }

            return result;
        }


        protected Dictionary<string, object> ConvertData(Dictionary<string, object> data)
        {
            if (data == null) return null;

            Dictionary<string, object> convertedData = new();

            foreach (KeyValuePair<string, object> item in data)
            {
                object convertedValue = ConvertData(item.Value);
                if (convertedValue == null) continue;
                convertedData.Add(item.Key, convertedValue);
            }

            if (convertedData.Count == 0) return null;
            return convertedData;
        }

        protected object ConvertData(object fieldValue)
        {
            if (fieldValue == null) return null;

            object converted;

            if (fieldValue is Dictionary<string, object> dictionary)
            {
                Dictionary<string, object> convertedDictionary = new();

                foreach (KeyValuePair<string, object> item in dictionary)
                {
                    object convertedValue = ConvertData(item.Value);
                    if (convertedValue == null) continue;
                    convertedDictionary.Add(item.Key, convertedValue);
                }

                if (convertedDictionary.Count == 0) return null;
                converted = convertedDictionary;
            }
            else if (fieldValue is IFiredata firestoreObject)
            {
                converted = firestoreObject.ToFirestoreFormat();
            }
            else
            {
                converted = CloudConverter.ToCloudFormat(fieldValue.GetType(), fieldValue);
            }

            return converted ?? null;
        }

        public int SetBatch(int batchId = -1, Action<IResult> onComplete = null)
        {
            if (batchId == -1) batchId = Firetask.BatchTasks.Count;

            if (Firetask.BatchTasks.TryGetValue(batchId, out HashSet<FiretaskBase> batchSet))
            {
                batchSet.Add(this);
            }
            else
            {
                batchSet = new HashSet<FiretaskBase>
                {
                    this
                };
                Firetask.BatchTasks.Add(batchId, batchSet);
            }

            OnComplete = onComplete;
            FirestoreManager.Logger.Info("Batch task added: " + batchId);
            return batchId;
        }
    }
}