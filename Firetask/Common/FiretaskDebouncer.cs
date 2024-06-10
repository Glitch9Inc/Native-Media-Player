using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Glitch9.Apis.Google.Firestore.Tasks
{
    public static class FiretaskDebouncer
    {
        private const int DEFAULT_QUIET_PERIOD_IN_SEC = 10;
        private static readonly Dictionary<int /* batchId */, CancellationTokenSource> DebounceTasks = new();

        public static async void DebounceAsync(this FiretaskBase task, int batchId, int seconds = DEFAULT_QUIET_PERIOD_IN_SEC)
        {
            if (task == null) return;
            if (DebounceTasks.ContainsKey(batchId))
            {
                DebounceTasks[batchId].Cancel();
                DebounceTasks[batchId] = new(); // create new token source
            }
            else
            {
                DebounceTasks.Add(batchId, new());
            }

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(seconds),
                    cancellationToken: DebounceTasks[batchId].Token);
                bool success = await Firetask.ExecuteBatchAsync(batchId); // Execute batch update if no Set calls within the last 10 seconds
                if (success) DebounceTasks.Remove(batchId);
            }
            catch (OperationCanceledException)
            {
                // This block intentionally left empty.
            }
        }
    }
}