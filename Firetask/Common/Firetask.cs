using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using Glitch9.Apis.Google.Firebase;
using Glitch9.Apis.Google.Firestore.Tasks;
using System;
using System.Collections.Generic;

namespace Glitch9.Apis.Google.Firestore
{
    /// <summary>
    /// Manager class for handling Firestore tasks in batches.
    /// </summary>
    public static class Firetask
    {
        public static HashSet<FiretaskBase> Tasks { get; set; } = new();
        public static HashSet<FiretaskBase> FallbackTasks { get; set; } = new();
        public static Dictionary<int, HashSet<FiretaskBase>> BatchTasks { get; set; } = new();
        public static List<string> CurrentBatchInfo { get; set; }

        /// <summary>
        /// Executes a batch of Firestore tasks asynchronously.
        /// </summary>
        /// <param name="batchSetId">The ID of the batch to execute.</param>
        /// <param name="onComplete">An optional callback action to invoke upon completion.</param>
        public static async void ExecuteBatch(int batchSetId, Action<IResult> onComplete = null) => await HandleBatchAsync(batchSetId, onComplete);

        /// <summary>
        /// Executes a batch of Firestore tasks asynchronously.
        /// </summary>
        /// <param name="batchSetId">The ID of the batch to execute.</param>
        /// <returns>A task representing the asynchronous operation, containing the result of the execution.</returns>
        public static async UniTask<IResult> ExecuteBatchAsync(int batchSetId) => await HandleBatchAsync(batchSetId);

        private static async UniTask<IResult> HandleBatchAsync(int batchSetId, Action<IResult> onComplete = null)
        {
            if (!FirebaseManager.CheckFirebaseAuth()) return Result.Fail(Strings.INVALID_FIREBASE_AUTH);
            HashSet<FiretaskBase> batchList = GetBatchSet(batchSetId);
            if (batchList.IsNullOrEmpty()) return Result.Fail(Strings.BATCH_IS_NULL_OR_EMPTY);

            bool success = false;
            int retryCount = 0;
            int maxRetries = 3; // Maximum number of retries
            TimeSpan retryDelay = TimeSpan.FromSeconds(3); // Delay between retries
            IResult result = Result.Fail(Strings.BATCH_FAILED);

            while (!success && retryCount < maxRetries)
            {
                try
                {
                    await BuildWriteBatch(batchList).CommitAsync();
                    FirestoreManager.Logger.Info(Strings.BATCH_COMPLETED_SUCCESSFULLY);
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
            if (success) BatchTasks.Remove(batchSetId);

            return result;
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
                    FirestoreManager.Logger.Error($"{Strings.BATCH_CALLBACK_ERROR} {ex.Message}");
                }
            }
        }

        private static void HandleBatchException(Exception ex, ref int retryCount, int maxRetries, TimeSpan retryDelay)
        {
            string errorMsg = ex.Message;

            if (errorMsg.StartsWith(Strings.UNABLE_TO_CREATE_CONVERTER))
            {
                FirestoreManager.Logger.Error($"{Strings.CONVERSION_FAILED}: {errorMsg}\n\n{ex.StackTrace}");
                retryCount = maxRetries;
            }

            retryCount++;
            if (retryCount < maxRetries)
            {
                FirestoreManager.Logger.Info($"{Strings.RETRYING_BATCH} {retryCount}");
                UniTask.Delay(retryDelay).Forget(); // Wait before retrying
            }
            else
            {
                GNLog.Exception(ex);
                string currentBatchInfoString = GetCurrentBatchInfoString();
                GNLog.Critical($"{Strings.BATCH_FAILED_CURRENT_BATCH_INFO}\n{currentBatchInfoString}");
                CurrentBatchInfo.Clear();
            }
        }

        private static string GetCurrentBatchInfoString()
        {
            if (CurrentBatchInfo.IsNullOrEmpty()) return "";
            return string.Join("\n", CurrentBatchInfo);
        }

        /// <summary>
        /// Executes all batch tasks.
        /// </summary>
        /// <param name="onComplete">An optional callback action to invoke upon completion.</param>
        public static void ExecuteAllBatch(Action<IResult> onComplete = null)
        {
            if (BatchTasks.Count == 0) return;

            foreach (KeyValuePair<int, HashSet<FiretaskBase>> batch in BatchTasks)
            {
                ExecuteBatch(batch.Key, onComplete);
            }
        }

        /// <summary>
        /// Executes all batch tasks asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation, containing the result of the execution.</returns>
        public static async UniTask<IResult> ExecuteAllBatchAsync()
        {
            if (BatchTasks.IsNullOrEmpty()) return Result.Fail(Strings.NO_BATCH_TASK);

            foreach (KeyValuePair<int, HashSet<FiretaskBase>> batch in BatchTasks)
            {
                await ExecuteBatchAsync(batch.Key);
            }

            return Result.Success();
        }

        private static HashSet<FiretaskBase> GetBatchSet(int batchSetId)
        {
            if (!BatchTasks.TryGetValue(batchSetId, out HashSet<FiretaskBase> batchSet) || batchSet == null || batchSet.Count == 0)
            {
                FirestoreManager.Logger.Warning($"{Strings.NO_BATCH_TASK_WITH_ID} {batchSetId}");
                return null;
            }

            batchSet.RemoveWhere(task => task.ValidateTask().IsFailure);

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

        /// <summary>
        /// Contains constant string values for logging and messages.
        /// </summary>
        private static class Strings
        {
            internal const string INVALID_FIREBASE_AUTH = "Invalid Firebase Auth.";
            internal const string BATCH_IS_NULL_OR_EMPTY = "Batch is null or empty.";
            internal const string BATCH_FAILED = "Batch Failed";
            internal const string BATCH_COMPLETED_SUCCESSFULLY = "Batch Completed successfully.";
            internal const string BATCH_CALLBACK_ERROR = "Error occurred while processing batch callback:";
            internal const string UNABLE_TO_CREATE_CONVERTER = "Unable to create converter";
            internal const string CONVERSION_FAILED = "Conversion failed";
            internal const string RETRYING_BATCH = "Retrying batch... Attempt ";
            internal const string BATCH_FAILED_CURRENT_BATCH_INFO = "Batch Failed. Current batch info:";
            internal const string NO_BATCH_TASK = "There is no batch task";
            internal const string NO_BATCH_TASK_WITH_ID = "There is no batch task with id:";
        }
    }
}
