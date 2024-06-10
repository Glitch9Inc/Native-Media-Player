namespace Glitch9.Apis.Google.Firestore.Tasks
{
    /// <summary>
    /// Specifies the possible actions that can be performed on Firestore documents and collections.
    /// </summary>
    public enum FiretaskAction
    {
        /// <summary>
        /// Merges all fields in the target document with the fields from the source document.
        /// </summary>
        MergeAll,

        /// <summary>
        /// Completely overwrites the target document with the source document.
        /// </summary>
        Overwrite,

        /// <summary>
        /// Updates the fields in the target document with the fields from the source document.
        /// </summary>
        Update,

        /// <summary>
        /// Deletes the target document.
        /// </summary>
        Delete,

        /// <summary>
        /// Adds new documents to the target collection. This action is only used by <see cref="CollectionTask"/>.
        /// </summary>
        AddDocuments,
    }
}