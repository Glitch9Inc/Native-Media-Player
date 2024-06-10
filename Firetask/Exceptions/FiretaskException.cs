using System;

namespace Glitch9.Apis.Google.Firestore.Tasks
{
    public class FiretaskException : Exception
    {
        public FiretaskException() : base() { }

        public FiretaskException(string message) : base(message) { }

        public FiretaskException(string message, Exception innerException) : base(message, innerException) { }

        // Add custom properties if needed
        public int ErrorCode { get; set; }

        public FiretaskException(string message, int errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        public FiretaskException(string message, int errorCode, Exception innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }

        public static FiretaskException DocumentFactoryNotRegistered(Type type)
        {
            return new FiretaskException($"{type.Name} is not registered in FirestoreReference.");
        }

        public static FiretaskException CollectionFactoryNotRegistered(Type type)
        {
            return new FiretaskException($"{type.Name} is not registered in FirestoreReference.");
        }
    }
}