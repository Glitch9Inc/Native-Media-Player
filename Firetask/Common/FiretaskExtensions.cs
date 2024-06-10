using Firebase.Firestore;
using System;
using System.Collections.Generic;

namespace Glitch9.Apis.Google.Firestore.Tasks
{
    public static class FiretaskExtensions
    {
        public static FieldTask SetFieldTask(this DocumentReference docRef) => new(docRef);
        public static FieldTask SetFieldTask(this DocumentReference docRef, string fieldName, object value) => new(docRef, fieldName, value);
        public static DocumentTask SetDocumentTask(this DocumentReference docRef, Dictionary<string, object> data = null) => new(docRef, data);
        public static CollectionTask SetCollectionTask(this CollectionReference colRef, Dictionary<string, object> data = null) => new(colRef, data);

        public static FieldTask SetFieldTask<T>(this T field, string email = null) where T : IFiredata
        {
            if (!field.IsValidEntityType(FiredataType.Field)) return null;
            DocumentReference docRef = field.GetDocument(email);
            return !docRef.IsValidDocument() ? null : new FieldTask(docRef, field.ToFirestoreFormat());
        }

        public static DocumentTask SetDocumentTask<TDoc>(this TDoc doc, string email = null) where TDoc : IFirestoreDocument
        {
            if (!doc.IsValidEntityType(FiredataType.Document)) return null;
            DocumentReference docRef = doc.GetDocument(email);
            return !docRef.IsValidDocument() ? null : new DocumentTask(docRef, doc.ToFirestoreFormat());
        }

        public static CollectionTask SetCollectionTask<TMap>(this TMap map, string email = null) where TMap : IFirestoreDictionary
        {
            if (!map.IsValidEntityType(FiredataType.Collection)) return null;
            CollectionReference colRef = map.GetCollection(email);
            return !colRef.IsValidCollection() ? null : new CollectionTask(colRef, map.ToFirestoreFormat());
        }

        public static int SetMergeBatch<TEntity>(this TEntity entity, int batchId, Action<IResult> onComplete)
            where TEntity : IFiredata => entity.SetMergeBatch(batchId, null, onComplete);
        public static int SetMergeBatch<TEntity>(this TEntity entity, int batchId, string email = null, Action<IResult> onComplete = null)
            where TEntity : IFiredata
        {
            if (entity == null) return -1;

            if (entity is IFirestoreDictionary mapEntity)
            {
                CollectionTask task = mapEntity.SetCollectionTask(email).SetAction(FiretaskAction.AddDocuments);
                return task.SetBatch(batchId, onComplete);
            }
            else if (entity is IFirestoreDocument docEntity)
            {
                DocumentTask task = docEntity.SetDocumentTask(email).SetAction(FiretaskAction.MergeAll);
                return task.SetBatch(batchId, onComplete);
            }
            else
            {
                FieldTask task = entity.SetFieldTask(email).SetAction(FiretaskAction.MergeAll);
                return task.SetBatch(batchId, onComplete);
            }
        }
        public static int SetDeleteBatch<TEntity>(this TEntity entity, int batchId, Action<IResult> onComplete)
            where TEntity : IFiredata => entity.SetDeleteBatch(batchId, null, onComplete);
        public static int SetDeleteBatch<TEntity>(this TEntity entity, int batchId, string email = null, Action<IResult> onComplete = null)
            where TEntity : IFiredata
        {
            if (entity == null) return -1;

            if (entity is IFirestoreDictionary mapEntity)
            {
                CollectionTask task = mapEntity.SetCollectionTask(email).SetAction(FiretaskAction.Delete);
                return task.SetBatch(batchId, onComplete);
            }
            else if (entity is IFirestoreDocument docEntity)
            {
                DocumentTask task = docEntity.SetDocumentTask(email).SetAction(FiretaskAction.Delete);
                return task.SetBatch(batchId, onComplete);
            }
            else
            {
                FieldTask task = entity.SetFieldTask(email).SetAction(FiretaskAction.Delete);
                return task.SetBatch(batchId, onComplete);
            }
        }

        public static FiretaskBase SetMergeTask<TEntity>(this TEntity entity, Action<IResult> onComplete) where TEntity : IFiredata
            => entity.SetMergeTask(null, onComplete);

        public static FiretaskBase SetMergeTask<TEntity>(this TEntity entity, string email = null, Action<IResult> onComplete = null) where TEntity : IFiredata
        {
            if (entity == null) return null;

            if (entity is IFirestoreDictionary mapEntity)
            {
                if (mapEntity.FiredataType == FiredataType.Document)
                {
                    return OnComplete(mapEntity.GetDocument(email).SetDocumentTask(mapEntity.ToFirestoreFormat()).SetAction(FiretaskAction.MergeAll), onComplete);
                }
                else if (mapEntity.FiredataType == FiredataType.Collection)
                {
                    return OnComplete(mapEntity.GetCollection(email).SetCollectionTask(mapEntity.ToFirestoreFormat()).SetAction(FiretaskAction.AddDocuments), onComplete);
                }
                else
                {
                    GNLog.Error("This firestore map's entity type is invalid");
                    return null;
                }
            }
            else if (entity is IFirestoreDocument docEntity)
            {
                return OnComplete(docEntity.SetDocumentTask(email).SetAction(FiretaskAction.MergeAll), onComplete);
            }
            else
            {
                return OnComplete(entity.SetFieldTask(email).SetAction(FiretaskAction.MergeAll), onComplete);
            }
        }

        public static FiretaskBase SetRemoveTask<TEntity>(this TEntity entity, Action<IResult> onComplete) where TEntity : IFiredata
            => entity.SetRemoveTask(null, onComplete);
        public static FiretaskBase SetRemoveTask<TEntity>(this TEntity entity, string email = null, Action<IResult> onComplete = null) where TEntity : IFiredata
        {
            if (entity == null) return null;

            if (entity is IFirestoreDictionary mapEntity)
            {
                return OnComplete(mapEntity.SetCollectionTask(email).SetAction(FiretaskAction.Delete), onComplete);
            }
            else if (entity is IFirestoreDocument docEntity)
            {
                return OnComplete(docEntity.SetDocumentTask(email).SetAction(FiretaskAction.Delete), onComplete);
            }
            else
            {
                return OnComplete(entity.SetFieldTask(email).SetAction(FiretaskAction.Delete), onComplete);
            }
        }

        private static bool IsValidEntityType<T>(this T entity, FiredataType type) where T : IFiredata
        {
            if (entity.GetEntityType() != type)
            {
                GNLog.Error($"This firestore entity is not a {type}");
                return false;
            }

            return true;
        }

        private static bool IsValidDocument(this DocumentReference docRef)
        {
            if (docRef == null)
            {
                GNLog.Error("Document reference is null");
                return false;
            }

            return true;
        }

        private static bool IsValidCollection(this CollectionReference colRef)
        {
            if (colRef == null)
            {
                GNLog.Error("Collection reference is null");
                return false;
            }

            return true;
        }

        public static TFiretask SetAction<TFiretask>(this TFiretask task, FiretaskAction action) where TFiretask : FiretaskBase
        {
            if (task == null) return null;
            task.TaskAction = action;
            return task;
        }

        public static TFiretask OnComplete<TFiretask>(this TFiretask task, Action<IResult> onComplete) where TFiretask : FiretaskBase
        {
            if (task == null) return null;
            task.OnComplete = onComplete;
            return task;
        }
    }
}