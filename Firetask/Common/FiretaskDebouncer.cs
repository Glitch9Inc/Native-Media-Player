using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Glitch9.Apis.Google.Firestore.Tasks
{
    /// <summary>
    /// Provides functionality to debounce Firestore tasks, ensuring they are not executed too frequently.
    /// </summary>
    public static class FiretaskDebouncer
    {
        private const int DEFAULT_QUIET_PERIOD_IN_SEC = 10;
        private static readonly Dictionary<int /* batchId */, CancellationTokenSource> _debounceTasks = new();

        /// <summary>
        /// Debounces the execution of a Firestore task. If the same batch is triggered within the debounce period, the timer resets.
        /// </summary>
        /// <param name="task">The Firestore task to debounce.</param>
        /// <param name="batchId">The ID of the batch.</param>
        /// <param name="seconds">The debounce period in seconds. Defaults to 10 seconds.</param>
        public static async void DebounceAsync(this FiretaskBase task, int batchId, int seconds = DEFAULT_QUIET_PERIOD_IN_SEC)
        {
            if (task == null) return;

            if (_debounceTasks.ContainsKey(batchId))
            {
                _debounceTasks[batchId].Cancel();
                _debounceTasks[batchId] = new CancellationTokenSource(); // create new token source
            }
            else
            {
                _debounceTasks.Add(batchId, new CancellationTokenSource());
            }

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(seconds),
                    cancellationToken: _debounceTasks[batchId].Token);
                IResult result = await Firetask.ExecuteBatchAsync(batchId); // Execute batch update if no Set calls within the last specified seconds
                if (result.IsSuccess) _debounceTasks.Remove(batchId);
            }
            catch (OperationCanceledException)
            {
                // This block intentionally left empty.
            }
        }
    }
}
