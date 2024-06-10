using System.Collections.Generic;
using Firebase.Firestore;

namespace Glitch9.Apis.Google.Firestore
{
    public interface IFiredata
    {
        /// <summary>
        /// 이 Object가 Document일 경우 Document이름을 반환하고,
        /// 이 Object가 Field일 경우 Field이름을 반환한다.
        /// </summary>
        string GetEntityName()
        {
            if (this is IMapEntry mapEntry) return mapEntry.Key.ToSnakeCase();
            return GetType().Name.ToSnakeCase();
        }

        FiredataType GetEntityType()
        {
            if (this is IFirestoreDocument) return FiredataType.Document;
            if (this is IFirestoreDictionary map) return map.FiredataType;
            return FiredataType.Field;
        }

        /// <summary>
        /// 이 Object가 사용하는 DocumentReference를 반환한다.
        /// </summary>
        /// <param name="referenceName">레퍼런스의 이름. 유저 데이터의 경우 유저의 Email이 Collection의 이름으로 사용된다.</param>
        /// <returns></returns>
        DocumentReference GetDocument(string referenceName = null);

        /// <summary>
        /// 이 Object에 Firestore에서 가져온 정보를 적용한다.
        /// </summary>
        IFiredata SetMap(Dictionary<string, object> data);

        /// <summary>
        /// 이 Object를 Firestore에 저장할 수 있는 형태로 변환한다.
        /// </summary>
        Dictionary<string, object> ToFirestoreFormat();
    }
}

