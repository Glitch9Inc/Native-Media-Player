using Firebase.Firestore;

namespace Glitch9.Apis.Google.Firestore
{
    /// <summary>
    /// Represents a Firestore dictionary used in two scenarios:
    /// 1. When the key is a document name and the value is a field.
    /// 2. When the key is a collection name and the value is a document.
    /// (Hint: All collections used in the GNFirestore system are represented as arrays.)
    /// </summary>
    public interface IFirestoreDictionary : IFiredata
    {
        /// <summary>
        /// Gets the type of Firestore data entity.
        /// </summary>
        FiredataType FiredataType { get; }

        /// <summary>
        /// Returns the <see cref="CollectionReference"/> used by this dictionary.
        /// </summary>
        /// <param name="args">Optional parameters, typically used for constructing the collection reference name.</param>
        /// <returns>
        /// The <see cref="CollectionReference"/> associated with this dictionary.
        /// </returns>
        CollectionReference GetCollection(params string[] args);

        /// <summary>
        /// Applies the data from a Firestore query snapshot to this dictionary.
        /// This is used when the dictionary's key is a collection name and the value is a document.
        /// </summary>
        /// <param name="data">The query snapshot containing the Firestore data.</param>
        /// <returns>
        /// An <see cref="IFiredata"/> instance with the applied data.
        /// </returns>
        IFiredata SetSnapshots(QuerySnapshot data);
    }
}