using System;

namespace Glitch9.Apis.Google.Firestore
{
    /// <summary>
    /// Specifies that a class is associated with a Firestore collection and provides the path template for the collection.
    /// Path example: "users/{userId}/tasks/{taskId}"
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class FirestoreCollectionAttribute : Attribute
    {
        /// <summary>
        /// Gets the path template for the Firestore collection.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FirestoreCollectionAttribute"/> class with the specified path.
        /// </summary>
        /// <param name="path">The path template for the Firestore collection.</param>
        public FirestoreCollectionAttribute(string path)
        {
            Path = path;
        }
    }
}