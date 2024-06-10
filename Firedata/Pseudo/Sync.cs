using Firebase.Firestore;
using Glitch9.Apis.Google.Firestore.Tasks;
using System;
using System.Collections.Generic;
using Glitch9.IO.Network;
using UnityEngine;

namespace Glitch9.Apis.Google.Firestore
{
    /// <summary>
    /// Firestore의 Field를 동기화하는 클래스
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Sync<T>
    {
        public static implicit operator T(Sync<T> sync) => sync.Value;

        private readonly DocumentReference document;
        private readonly string _fieldName;
        private T _value;
        private readonly object _lock = new();

        public Sync() { }

        public Sync(DocumentReference docRef, string fieldName)
        {
            document = docRef ?? throw new ArgumentNullException(nameof(docRef));
            _fieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
        }

        public Sync(DocumentReference docRef, string fieldName, T value)
        {
            document = docRef ?? throw new ArgumentNullException(nameof(docRef));
            _fieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
            _value = value;
        }

        public T Value
        {
            get => GetValue();
            set => SetValue(value);
        }

        private T GetValue()
        {
            lock (_lock)
            {
                if (_value == null)
                {
                    if (typeof(T) == typeof(string))
                    {
                        _value = (T)(object)string.Empty;
                    }
                    else if (Nullable.GetUnderlyingType(typeof(T)) != null || !typeof(T).IsValueType)
                    {
                        _value = Activator.CreateInstance<T>();
                    }
                    else
                    {
                        _value = default;
                    }
                }

                return _value;
            }
        }

        private void SetValue(T newValue)
        {
            lock (_lock)
            {
                if (document == null || EqualityComparer<T>.Default.Equals(_value, newValue))
                {
                    return;
                }

                _value = newValue;

                if (_value != null)
                {
                    FieldTask fieldTask;

                    if (_value is Color color)
                    {
                        string colorHex = color.ToHex();
                        fieldTask = new FieldTask(document).SetData(_fieldName, colorHex);
                        fieldTask.Execute();
                        return;
                    }

                    object converted = CloudConverter.ToCloudFormat(typeof(T), _value);
                    if (converted == null) return;
                    fieldTask = new FieldTask(document).SetData(_fieldName, converted);
                    fieldTask.Execute();
                }
            }
        }
    }
}
