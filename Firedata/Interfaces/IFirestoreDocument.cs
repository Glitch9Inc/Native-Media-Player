using Firebase.Firestore;

namespace Glitch9.Apis.Google.Firestore
{
    /// <summary>
    /// Represents a Firestore document that can interact with Firestore.
    /// </summary>
    public interface IFirestoreDocument : IFiredata
    {
        /// <summary>
        /// Applies data from a Firestore document snapshot to this document.
        /// </summary>
        /// <param name="data">The document snapshot containing the Firestore data.</param>
        /// <returns>
        /// An <see cref="IFiredata"/> instance with the applied data.
        /// </returns>
        IFiredata ToLocalFormat(DocumentSnapshot data);
    }
}