using System;

namespace Glitch9.Apis.Google.Firestore
{
    /// <summary>
    /// Specifies that a class is associated with a Firestore document and provides the path template for the document.
    /// Path example: "users/{userId}/tasks/{taskId}"
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class FirestoreDocumentAttribute : Attribute
    {
        /// <summary>
        /// Gets the path template for the Firestore document.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FirestoreDocumentAttribute"/> class with the specified path.
        /// </summary>
        /// <param name="path">The path template for the Firestore document.</param>
        public FirestoreDocumentAttribute(string path)
        {
            Path = path;
        }
    }
}