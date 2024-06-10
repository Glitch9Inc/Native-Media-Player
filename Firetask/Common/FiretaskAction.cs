namespace Glitch9.Apis.Google.Firestore.Tasks
{
    public enum FiretaskAction
    {
        MergeAll,
        Overwrite,
        Update,
        Delete,
        /// <summary>
        /// Only used by CollectionReference.
        /// </summary>
        AddDocuments,
    }
}