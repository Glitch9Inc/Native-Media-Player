namespace Glitch9.Apis.Google.Firestore
{
    public class FirestoreLogger : ILogger
    {
        private const string TAG = "Firestore";
        
        public void Info(string message)
        {
            FirestoreManager.Logger.Info(TAG, message);
        }

        public void Warning(string message)
        {
            FirestoreManager.Logger.Warning(TAG, message);
        }

        public void Error(string message)
        {
            FirestoreManager.Logger.Error(TAG, message);
        }

        public void Info(string tag, string message)
        {
            FirestoreManager.Logger.Info(tag, message);
        }

        public void Warning(string tag, string message)
        {
            FirestoreManager.Logger.Warning(tag, message);
        }

        public void Error(string tag, string message)
        {
            FirestoreManager.Logger.Error(tag, message);
        }
    }
}