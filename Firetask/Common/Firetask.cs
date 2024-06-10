using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using Glitch9.Apis.Google.Firebase;
using Glitch9.Apis.Google.Firestore.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Glitch9.Apis.Google.Firestore
{
    public static class Firetask
    {
        public static HashSet<FiretaskBase> Tasks = new();
        public static HashSet<FiretaskBase> FallbackTasks = new();
        public static Dictionary<int, HashSet<FiretaskBase>> BatchTasks = new();
        public static List<string> CurrentBatchInfo { get; set; }


        private static HashSet<FiretaskBase> GetBatchSet(int batchSetId)
        {
            if (!BatchTasks.TryGetValue(batchSetId, out HashSet<FiretaskBase> batchSet) || batchSet == null || batchSet.Count == 0)
            {
                GNLog.Warning("There is no batch task with id : " + batchSetId);
                return null;
            }

            for (int i = 0; i < batchSet.Count; i++)
            {
                FiretaskBase task = batchSet.ElementAt(i);
                if (!task.Validate())
                {
                    batchSet.Remove(task);
                }
            }

            return batchSet;
        }

        private static WriteBatch BuildWriteBatch(IEnumerable<FiretaskBase> batchList)
        {
            WriteBatch batch = FirebaseFirestore.DefaultInstance.StartBatch();

            foreach (FiretaskBase task in batchList)
            {
                if (task is DocumentTask docTask)
                {
                    if (docTask.TaskAction == FiretaskAction.MergeAll)
                        batch.Set(docTask.Document, docTask.Data, SetOptions.MergeAll);
                    else if (docTask.TaskAction == FiretaskAction.Overwrite)
                        batch.Set(docTask.Document, docTask.Data);
                    else if (docTask.TaskAction == FiretaskAction.Update)
                        batch.Update(docTask.Document, docTask.Data);
                    else if (docTask.TaskAction == FiretaskAction.Delete)
                        batch.Delete(docTask.Document);

                    CurrentBatchInfo.Add(docTask.Document.Path);
                }
                else if (task is FieldTask fieldTask)
                {
                    string fieldName = fieldTask.FieldName;
                    object fieldValue = fieldTask.FieldValue;

                    if (fieldTask.TaskAction == FiretaskAction.MergeAll)
                        batch.Set(fieldTask.Document, new Dictionary<string, object> { { fieldName, fieldValue } }, SetOptions.MergeAll);
                    else if (fieldTask.TaskAction == FiretaskAction.Overwrite)
                        batch.Set(fieldTask.Document, new Dictionary<string, object> { { fieldName, fieldValue } });
                    else if (fieldTask.TaskAction == FiretaskAction.Update)
                        batch.Update(fieldTask.Document, fieldName, fieldValue);
                    else if (fieldTask.TaskAction == FiretaskAction.Delete)
                        batch.Update(fieldTask.Document, fieldName, FieldValue.Delete);

                    CurrentBatchInfo.Add(fieldTask.Document.Path);
                }
            }

            return batch;
        }

        public static async void ExecuteBatch(int batchSetId, Action<IResult> onComplete = null) => await HandleBatchAsync(batchSetId, onComplete);
        public static async UniTask<bool> ExecuteBatchAsync(int batchSetId, Action<IResult> onComplete = null) => await HandleBatchAsync(batchSetId, onComplete);
        private static async UniTask<bool> HandleBatchAsync(int batchSetId, Action<IResult> onComplete)
        {
            if (!FirebaseManager.CheckFirebaseAuth()) return false;
            HashSet<FiretaskBase> batchList = GetBatchSet(batchSetId);
            if (batchList == null || batchList.Count == 0) return false;

            bool success = false;
            int retryCount = 0;
            int maxRetries = 3; // You can set the maximum number of retries
            TimeSpan retryDelay = TimeSpan.FromSeconds(3); // Delay between retries
            IResult result = Result.Fail("Batch Failed");

            while (!success && retryCount < maxRetries)
            {
                try
                {
                    await BuildWriteBatch(batchList).CommitAsync();
                    GNLog.Info("Batch Completed successfully.");
                    success = true; // If operation succeeds
                    result = Result.Success();
                }
                catch (Exception ex)
                {
                    HandleBatchException(ex, ref retryCount, maxRetries, retryDelay);
                    result = Result.Error(ex);
                }
            }

            // Process callbacks outside of main try-catch block
            ProcessCallbacks(batchList, result);
            // Invoke final success callback
            onComplete?.Invoke(result);

            if (success)
            {
                BatchTasks.Remove(batchSetId);
            }

            return success;
        }

        private static void ProcessCallbacks(HashSet<FiretaskBase> batchList, IResult result)
        {
            foreach (FiretaskBase t in batchList)
            {
                try
                {
                    t.OnComplete?.Invoke(result);
                }
                catch (Exception ex)
                {
                    GNLog.Error("배치 콜백 처리 중 에러 발생: " + ex.Message);
                }
            }
        }

        private static void HandleBatchException(Exception ex, ref int retryCount, int maxRetries, TimeSpan retryDelay)
        {
            string errorMsg = ex.Message;

            if (errorMsg.StartsWith("Unable to create converter"))
            {
                GNLog.Error($"형변환 실패: {errorMsg}\n\n{ex.StackTrace}");
                retryCount = maxRetries;
            }

            retryCount++;
            if (retryCount < maxRetries)
            {
                GNLog.Info("Retrying batch... Attempt " + retryCount);
                UniTask.Delay(retryDelay).Forget(); // Wait Before retrying
            }
            else
            {
                GNLog.Exception(ex);
                string currentBatchInfoString = GetCurrentBatchInfoString();
                GNLog.Critical("Batch Failed. Current batch info:\n" + currentBatchInfoString);
                CurrentBatchInfo.Clear();
            }
        }

        private static string GetCurrentBatchInfoString()
        {
            if (CurrentBatchInfo.IsNullOrEmpty()) return "";
            string batchInfoString = "";
            foreach (string path in CurrentBatchInfo)
            {
                batchInfoString += path + "\n";
            }
            return batchInfoString;
        }

        public static void ExecuteAllBatch(Action<IResult> onComplete = null)
        {
            if (BatchTasks.Count == 0)
            {
                GNLog.Error("There is no batch task");
                return;
            }

            foreach (KeyValuePair<int, HashSet<FiretaskBase>> batch in BatchTasks)
            {
                ExecuteBatch(batch.Key, onComplete);
            }
        }

        public static async UniTask ExecuteAllBatchAsync(Action<IResult> onComplete = null)
        {
            if (BatchTasks.Count == 0)
            {
                GNLog.Error("There is no batch task");
                return;
            }

            foreach (KeyValuePair<int, HashSet<FiretaskBase>> batch in BatchTasks)
            {
                await ExecuteBatchAsync(batch.Key, onComplete);
            }
        }
    }
}