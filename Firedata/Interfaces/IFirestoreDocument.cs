using Firebase.Firestore;

namespace Glitch9.Apis.Google.Firestore
{
    public interface IFirestoreDocument : IFiredata
    {
        IFiredata SetSnapshot(DocumentSnapshot data);
    }
}

