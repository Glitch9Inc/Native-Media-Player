using System;

namespace Glitch9.Apis.Google.Firestore
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class FirestoreDocumentAttribute : Attribute
    {
        public string Path { get; }

        public FirestoreDocumentAttribute(string path)
        {
            Path = path;
        }
    }
}