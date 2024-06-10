using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using Glitch9.IO.Network;
using System;
using System.Collections.Generic;

namespace Glitch9.Apis.Google.Firestore
{
    /// <summary>
    /// Implementation of ISyncStorage using Firestore.
    /// </summary>
    public class FirestoreSyncStorage : ISyncStorage
    {
        private readonly DocumentReference _document;

        /// <summary>
        /// Initializes a new instance of the FirestoreSyncStorage class.
        /// </summary>
        /// <param name="document">The Firestore DocumentReference to use for synchronization.</param>
        public FirestoreSyncStorage(DocumentReference document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
        }

        public async UniTask<object> GetDataAsync(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName)) throw new ArgumentNullException(nameof(fieldName));

            try
            {
                DocumentSnapshot snapshot = await _document.GetSnapshotAsync();
                object data = snapshot.GetValue<object>(fieldName);
                FirestoreManager.Logger.Info($"Successfully retrieved {fieldName} with value {data} from Firestore.");
                return data;
            }
            catch (Exception ex)
            {
                FirestoreManager.Logger.Error($"Failed to retrieve {fieldName} from Firestore: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Sets data asynchronously in the Firestore document.
        /// </summary>
        /// <param name="fieldName">The name of the field to set.</param>
        /// <param name="value">The value to set.</param>
        public async void SetDataAsync(string fieldName, object value)
        {
            if (string.IsNullOrWhiteSpace(fieldName)) throw new ArgumentNullException(nameof(fieldName));
            if (value == null) throw new ArgumentNullException(nameof(value));

            try
            {
                await _document.UpdateAsync(new Dictionary<string, object> { { fieldName, value } });
                FirestoreManager.Logger.Info($"Successfully updated {fieldName} with value {value} in Firestore.");
            }
            catch (Exception ex)
            {
                FirestoreManager.Logger.Error($"Failed to update {fieldName} in Firestore: {ex.Message}");
            }
        }
    }
}
