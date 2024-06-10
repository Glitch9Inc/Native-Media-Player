namespace Glitch9.Apis.Google.Firestore
{
    /// <summary>
    /// Specifies the type of Firestore data entity.
    /// </summary>
    public enum FiredataType
    {
        /// <summary>
        /// Type is not set. This is the default value.
        /// </summary>
        Unset,

        /// <summary>
        /// Represents a Firestore document.
        /// </summary>
        Document,

        /// <summary>
        /// Represents a Firestore collection.
        /// </summary>
        Collection,

        /// <summary>
        /// Represents a Firestore field within a document.
        /// </summary>
        Field
    }
}