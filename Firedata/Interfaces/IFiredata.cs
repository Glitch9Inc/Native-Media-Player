using Firebase.Firestore;
using System.Collections.Generic;

namespace Glitch9.Apis.Google.Firestore
{
    /// <summary>
    /// Defines the interface for objects that can interact with Firestore.
    /// </summary>
    public interface IFiredata
    {
        /// <summary>
        /// Returns the name of the object. If the object is a document, returns the document name.
        /// If the object is a field, returns the field name.
        /// </summary>
        /// <returns>
        /// A string representing the name of the Firestore data entity.
        /// </returns>
        string GetFiredataName()
        {
            if (this is IMapEntry mapEntry) return mapEntry.Key.ToSnakeCase();
            return GetType().Name.ToSnakeCase();
        }

        /// <summary>
        /// Returns the type of the Firestore data entity.
        /// </summary>
        /// <returns>
        /// A <see cref="FiredataType"/> representing the type of the Firestore data entity.
        /// </returns>
        FiredataType GetFiredataType()
        {
            if (this is IFirestoreDocument) return FiredataType.Document;
            if (this is IFirestoreDictionary map) return map.FiredataType;
            return FiredataType.Field;
        }

        /// <summary>
        /// Returns the <see cref="DocumentReference"/> used by this object.
        /// </summary>
        /// <param name="args">Optional parameters, typically the user's email for user-specific data.</param>
        /// <returns>
        /// The <see cref="DocumentReference"/> associated with this object.
        /// </returns>
        DocumentReference GetDocument(params string[] args);

        /// <summary>
        /// Applies data retrieved from Firestore to this object.
        /// </summary>
        /// <param name="firestoreData">A dictionary containing the Firestore data.</param>
        /// <returns>
        /// An <see cref="IFiredata"/> instance with the applied data.
        /// </returns>
        IFiredata ToLocalFormat(Dictionary<string, object> firestoreData);

        /// <summary>
        /// Converts this object to a format that can be stored in Firestore.
        /// </summary>
        /// <returns>
        /// A dictionary representing this object in a Firestore-compatible format.
        /// </returns>
        Dictionary<string, object> ToFirestoreFormat();
    }
}
