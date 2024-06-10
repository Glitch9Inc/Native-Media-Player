using System;

namespace Glitch9.Apis.Google.Firestore
{
    /// <summary>
    /// FirestoreManager is a static class that manages Firestore.
    /// </summary>
    public class FirestoreManager
    {
        /// <summary>
        /// Set or get the logger for FirestoreManager.
        /// </summary>
        public static ILogger Logger { get; set; } = new FirestoreLogger();

        internal static void LogNotInitializedYet(Type type)
        {
            Logger.Warning($"{type.Name} is not initialized yet.");
        }
    }
}