using Firebase.Firestore;
using System;
using System.Collections.Generic;

namespace Glitch9.Apis.Google.Firestore.Tasks
{

    /// <summary>
    /// Provides extension methods for Firestore tasks.
    /// </summary>
    public static class FiretaskExtensions
    {
        /// <summary>
        /// Creates a new FieldTask for the specified DocumentReference.
        /// </summary>
        /// <param name="docRef">The DocumentReference to create the task for.</param>
        /// <returns>A new FieldTask instance.</returns>
        public static FieldTask SetFieldTask(this DocumentReference docRef) => new(docRef);

        /// <summary>
        /// Creates a new FieldTask for the specified DocumentReference with the given field name and value.
        /// </summary>
        /// <param name="docRef">The DocumentReference to create the task for.</param>
        /// <param name="fieldName">The name of the field to set.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>A new FieldTask instance.</returns>
        public static FieldTask SetFieldTask(this DocumentReference docRef, string fieldName, object value) => new(docRef, fieldName, value);

        /// <summary>
        /// Creates a new DocumentTask for the specified DocumentReference with the given data.
        /// </summary>
        /// <param name="docRef">The DocumentReference to create the task for.</param>
        /// <param name="data">The data to set in the document.</param>
        /// <returns>A new DocumentTask instance.</returns>
        public static DocumentTask SetDocumentTask(this DocumentReference docRef, Dictionary<string, object> data = null) => new(docRef, data);

        /// <summary>
        /// Creates a new CollectionTask for the specified CollectionReference with the given data.
        /// </summary>
        /// <param name="colRef">The CollectionReference to create the task for.</param>
        /// <param name="data">The data to set in the collection.</param>
        /// <returns>A new CollectionTask instance.</returns>
        public static CollectionTask SetCollectionTask(this CollectionReference colRef, Dictionary<string, object> data = null) => new(colRef, data);

        /// <summary>
        /// Creates a new FieldTask for the specified Firestore field.
        /// </summary>
        /// <typeparam name="T">The type of the Firestore field.</typeparam>
        /// <param name="field">The Firestore field to create the task for.</param>
        /// <param name="args">The arguments to format the path template.</param>
        /// <returns>A new FieldTask instance, or null if the entity type is invalid.</returns>
        public static FieldTask SetFieldTask<T>(this T field, params string[] args) where T : IFiredata
        {
            if (!field.IsValidEntityType(FiredataType.Field)) return null;
            DocumentReference docRef = field.GetDocument(args);
            return !docRef.IsValidDocument() ? null : new FieldTask(docRef, field.ToFirestoreFormat());
        }

        /// <summary>
        /// Creates a new DocumentTask for the specified Firestore document.
        /// </summary>
        /// <typeparam name="TDoc">The type of the Firestore document.</typeparam>
        /// <param name="doc">The Firestore document to create the task for.</param>
        /// <param name="args">The arguments to format the path template.</param>
        /// <returns>A new DocumentTask instance, or null if the entity type is invalid.</returns>
        public static DocumentTask SetDocumentTask<TDoc>(this TDoc doc, params string[] args) where TDoc : IFirestoreDocument
        {
            if (!doc.IsValidEntityType(FiredataType.Document)) return null;
            DocumentReference docRef = doc.GetDocument(args);
            return !docRef.IsValidDocument() ? null : new DocumentTask(docRef, doc.ToFirestoreFormat());
        }

        /// <summary>
        /// Creates a new CollectionTask for the specified Firestore collection.
        /// </summary>
        /// <typeparam name="TMap">The type of the Firestore collection.</typeparam>
        /// <param name="map">The Firestore collection to create the task for.</param>
        /// <param name="args">The arguments to format the path template.</param>
        /// <returns>A new CollectionTask instance, or null if the entity type is invalid.</returns>
        public static CollectionTask SetCollectionTask<TMap>(this TMap map, params string[] args) where TMap : IFirestoreDictionary
        {
            if (!map.IsValidEntityType(FiredataType.Collection)) return null;
            CollectionReference colRef = map.GetCollection(args);
            return !colRef.IsValidCollection() ? null : new CollectionTask(colRef, map.ToFirestoreFormat());
        }

        /// <summary>
        /// Sets a merge batch task for the specified Firestore entity.
        /// </summary>
        /// <typeparam name="TEntity">The type of the Firestore entity.</typeparam>
        /// <param name="entity">The Firestore entity to create the task for.</param>
        /// <param name="batchId">The batch ID to associate with the task.</param>
        /// <param name="args">The arguments to format the path template.</param>
        /// <returns>The batch ID, or -1 if the entity is null.</returns>
        public static int SetMergeBatch<TEntity>(this TEntity entity, int batchId, params string[] args)
            where TEntity : IFiredata => entity.SetMergeBatch(batchId, null, args);

        /// <summary>
        /// Sets a merge batch task for the specified Firestore entity.
        /// </summary>
        /// <typeparam name="TEntity">The type of the Firestore entity.</typeparam>
        /// <param name="entity">The Firestore entity to create the task for.</param>
        /// <param name="batchId">The batch ID to associate with the task.</param>
        /// <param name="args">The arguments to format the path template.</param>
        /// <param name="onComplete">The callback to invoke upon task completion.</param>
        /// <returns>The batch ID, or -1 if the entity is null.</returns>
        public static int SetMergeBatch<TEntity>(this TEntity entity, int batchId, Action<IResult> onComplete = null, params string[] args)
            where TEntity : IFiredata
        {
            if (entity == null) return -1;

            if (entity is IFirestoreDictionary mapEntity)
            {
                CollectionTask task = mapEntity.SetCollectionTask(args).SetAction(FiretaskAction.AddDocuments);
                return task.SetBatch(batchId, onComplete);
            }
            else if (entity is IFirestoreDocument docEntity)
            {
                DocumentTask task = docEntity.SetDocumentTask(args).SetAction(FiretaskAction.MergeAll);
                return task.SetBatch(batchId, onComplete);
            }
            else
            {
                FieldTask task = entity.SetFieldTask(args).SetAction(FiretaskAction.MergeAll);
                return task.SetBatch(batchId, onComplete);
            }
        }

        /// <summary>
        /// Sets a delete batch task for the specified Firestore entity.
        /// </summary>
        /// <typeparam name="TEntity">The type of the Firestore entity.</typeparam>
        /// <param name="entity">The Firestore entity to create the task for.</param>
        /// <param name="batchId">The batch ID to associate with the task.</param>
        /// <param name="args">The arguments to format the path template.</param>
        /// <returns>The batch ID, or -1 if the entity is null.</returns>
        public static int SetDeleteBatch<TEntity>(this TEntity entity, int batchId, params string[] args)
            where TEntity : IFiredata => entity.SetDeleteBatch(batchId, null, args);

        /// <summary>
        /// Sets a delete batch task for the specified Firestore entity.
        /// </summary>
        /// <typeparam name="TEntity">The type of the Firestore entity.</typeparam>
        /// <param name="entity">The Firestore entity to create the task for.</param>
        /// <param name="batchId">The batch ID to associate with the task.</param>
        /// <param name="args">The arguments to format the path template.</param>
        /// <param name="onComplete">The callback to invoke upon task completion.</param>
        /// <returns>The batch ID, or -1 if the entity is null.</returns>
        public static int SetDeleteBatch<TEntity>(this TEntity entity, int batchId, Action<IResult> onComplete = null, params string[] args)
            where TEntity : IFiredata
        {
            if (entity == null) return -1;

            if (entity is IFirestoreDictionary mapEntity)
            {
                CollectionTask task = mapEntity.SetCollectionTask(args).SetAction(FiretaskAction.Delete);
                return task.SetBatch(batchId, onComplete);
            }
            else if (entity is IFirestoreDocument docEntity)
            {
                DocumentTask task = docEntity.SetDocumentTask(args).SetAction(FiretaskAction.Delete);
                return task.SetBatch(batchId, onComplete);
            }
            else
            {
                FieldTask task = entity.SetFieldTask(args).SetAction(FiretaskAction.Delete);
                return task.SetBatch(batchId, onComplete);
            }
        }

        /// <summary>
        /// Sets a merge task for the specified Firestore entity.
        /// </summary>
        /// <typeparam name="TEntity">The type of the Firestore entity.</typeparam>
        /// <param name="entity">The Firestore entity to create the task for.</param>
        /// <param name="args">The arguments to format the path template.</param>
        /// <returns>A FiretaskBase instance representing the task, or null if the entity is invalid.</returns>
        public static FiretaskBase SetMergeTask<TEntity>(this TEntity entity, params string[] args) where TEntity : IFiredata
            => entity.SetMergeTask(null, args);

        /// <summary>
        /// Sets a merge task for the specified Firestore entity.
        /// </summary>
        /// <typeparam name="TEntity">The type of the Firestore entity.</typeparam>
        /// <param name="entity">The Firestore entity to create the task for.</param>
        /// <param name="args">The arguments to format the path template.</param>
        /// <param name="onComplete">The callback to invoke upon task completion.</param>
        /// <returns>A FiretaskBase instance representing the task, or null if the entity is invalid.</returns>
        public static FiretaskBase SetMergeTask<TEntity>(this TEntity entity, Action<IResult> onComplete = null, params string[] args) where TEntity : IFiredata
        {
            if (entity == null) return null;

            if (entity is IFirestoreDictionary mapEntity)
            {
                if (mapEntity.FiredataType == FiredataType.Document)
                {
                    return OnComplete(mapEntity.GetDocument(args).SetDocumentTask(mapEntity.ToFirestoreFormat()).SetAction(FiretaskAction.MergeAll), onComplete);
                }
                else if (mapEntity.FiredataType == FiredataType.Collection)
                {
                    return OnComplete(mapEntity.GetCollection(args).SetCollectionTask(mapEntity.ToFirestoreFormat()).SetAction(FiretaskAction.AddDocuments), onComplete);
                }
                else
                {
                    FirestoreManager.Logger.Error("This firestore map's entity type is invalid");
                    return null;
                }
            }
            else if (entity is IFirestoreDocument docEntity)
            {
                return OnComplete(docEntity.SetDocumentTask(args).SetAction(FiretaskAction.MergeAll), onComplete);
            }
            else
            {
                return OnComplete(entity.SetFieldTask(args).SetAction(FiretaskAction.MergeAll), onComplete);
            }
        }

        /// <summary>
        /// Sets a delete task for the specified Firestore entity.
        /// </summary>
        /// <typeparam name="TEntity">The type of the Firestore entity.</typeparam>
        /// <param name="entity">The Firestore entity to create the task for.</param>
        /// <param name="args">The arguments to format the path template.</param>
        /// <returns>A FiretaskBase instance representing the task, or null if the entity is invalid.</returns>
        public static FiretaskBase SetRemoveTask<TEntity>(this TEntity entity, params string[] args) where TEntity : IFiredata
            => entity.SetRemoveTask(null, args);

        /// <summary>
        /// Sets a delete task for the specified Firestore entity.
        /// </summary>
        /// <typeparam name="TEntity">The type of the Firestore entity.</typeparam>
        /// <param name="entity">The Firestore entity to create the task for.</param>
        /// <param name="args">The arguments to format the path template.</param>
        /// <param name="onComplete">The callback to invoke upon task completion.</param>
        /// <returns>A FiretaskBase instance representing the task, or null if the entity is invalid.</returns>
        public static FiretaskBase SetRemoveTask<TEntity>(this TEntity entity, Action<IResult> onComplete = null, params string[] args) where TEntity : IFiredata
        {
            if (entity == null) return null;

            if (entity is IFirestoreDictionary mapEntity)
            {
                return OnComplete(mapEntity.SetCollectionTask(args).SetAction(FiretaskAction.Delete), onComplete);
            }
            else if (entity is IFirestoreDocument docEntity)
            {
                return OnComplete(docEntity.SetDocumentTask(args).SetAction(FiretaskAction.Delete), onComplete);
            }
            else
            {
                return OnComplete(entity.SetFieldTask(args).SetAction(FiretaskAction.Delete), onComplete);
            }
        }


        private static bool IsValidEntityType<T>(this T entity, FiredataType type) where T : IFiredata
        {
            if (entity.GetFiredataType() != type)
            {
                FirestoreManager.Logger.Error($"This firestore entity is not a {type}");
                return false;
            }

            return true;
        }

        private static bool IsValidDocument(this DocumentReference docRef)
        {
            if (docRef == null)
            {
                FirestoreManager.Logger.Error("Document reference is null");
                return false;
            }

            return true;
        }

        private static bool IsValidCollection(this CollectionReference colRef)
        {
            if (colRef == null)
            {
                FirestoreManager.Logger.Error("Collection reference is null");
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