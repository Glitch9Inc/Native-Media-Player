using System;

namespace Glitch9.Apis.Google.Firestore
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class FirestoreCollectionAttribute : Attribute
    {
        public string Path { get; }

        public FirestoreCollectionAttribute(string path)
        {
            Path = path;
        }
    }
}