using Firebase.Firestore;

namespace Glitch9.Apis.Google.Firestore
{
    /// <summary>
    /// 두가지 경우가 있다.
    /// 1. Key가 Document이름, Value가 Field일 경우 사용한다.
    /// 2. Key가 Collection이름, Value가 Document일 경우 사용한다.
    /// (hint) GNFirestore 시스템이 사용하는 모든 Collection은 Array형식을 의미한다.
    /// </summary>
    public interface IFirestoreDictionary : IFiredata
    {
        FiredataType FiredataType { get; }
        CollectionReference GetCollection(string referenceName = null);

        /// <summary>
        /// 이 Dictionary의 Key가 Collection이름, Value가 Document일 경우 사용한다.
        /// </summary>
        IFiredata SetSnapshots(QuerySnapshot data);
    }
}

