using UnityEngine;
using System.Collections.Generic;

namespace MarcosPereira.Utility {
    public class SerializableHashSet<T> : ScriptableObject, ISerializationCallbackReceiver {
        private HashSet<T> hashSet = new HashSet<T>();

        [SerializeField]
        private List<T> serializableItems;

        void ISerializationCallbackReceiver.OnBeforeSerialize() {
            this.serializableItems = new List<T>(this.hashSet);
            this.hashSet = null;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize() {
            foreach (T item in this.serializableItems) {
                _ = this.hashSet.Add(item);
            }

            this.serializableItems = null;
        }

        public static SerializableHashSet<T> Create() {
            SerializableHashSet<T> instance =
                ScriptableObject.CreateInstance<SerializableHashSet<T>>();

            return instance;
        }

        public bool Add(T item) => this.hashSet.Add(item);

        public bool Contains(T item) => this.hashSet.Contains(item);
    }
}
