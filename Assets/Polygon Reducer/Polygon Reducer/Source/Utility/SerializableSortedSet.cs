using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MarcosPereira.Utility {
    public class SerializableSortedSet<T> :
    ScriptableObject, ISerializationCallbackReceiver, IEnumerable<T> {
        private SortedSet<T> sortedSet = new SortedSet<T>();

        [SerializeField]
        private List<T> serializableItems;

        void ISerializationCallbackReceiver.OnBeforeSerialize() {
            this.serializableItems = new List<T>(this.sortedSet);
            this.sortedSet = null;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize() {
            foreach (T item in this.serializableItems) {
                _ = this.sortedSet.Add(item);
            }

            this.serializableItems = null;
        }

        // Allow iterating over this class
        IEnumerator<T> IEnumerable<T>.GetEnumerator() =>
            this.sortedSet.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            this.sortedSet.GetEnumerator();

        public static SerializableSortedSet<T> Create() {
            SerializableSortedSet<T> instance =
                ScriptableObject.CreateInstance<SerializableSortedSet<T>>();

            return instance;
        }

        public int count => this.sortedSet.Count;

        public bool Add(T item) => this.sortedSet.Add(item);

        public bool Remove(T item) => this.sortedSet.Remove(item);

        public bool Contains(T item) => this.sortedSet.Contains(item);
    }
}
