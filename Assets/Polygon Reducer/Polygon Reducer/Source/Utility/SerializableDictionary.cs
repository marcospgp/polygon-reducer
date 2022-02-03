using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MarcosPereira.Utility {
    public class SerializableDictionary<K, V> :
    ScriptableObject, ISerializationCallbackReceiver, IEnumerable<KeyValuePair<K, V>> {
        private Dictionary<K, V> dictionary = new Dictionary<K, V>();

        [SerializeField]
        private List<K> serializableKeys;

        [SerializeField]
        private List<V> serializableValues;

        void ISerializationCallbackReceiver.OnBeforeSerialize() {
            this.serializableKeys = new List<K>(this.dictionary.Keys);
            this.serializableValues = new List<V>(this.dictionary.Values);
            this.dictionary = null;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize() {
            this.dictionary = new Dictionary<K, V>(this.serializableKeys.Count);

            for (int i = 0; i < this.serializableKeys.Count; i++) {
                this.dictionary.Add(
                    this.serializableKeys[i],
                    this.serializableValues[i]
                );
            }

            this.serializableKeys = null;
            this.serializableValues = null;
        }

        // Allow iterating over this class
        IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator() =>
            this.dictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            this.dictionary.GetEnumerator();

        public static SerializableDictionary<K, V> Create() {
            SerializableDictionary<K, V> instance =
                ScriptableObject.CreateInstance<SerializableDictionary<K, V>>();

            return instance;
        }

        public V this[K key] {
            get => this.dictionary[key];
            set => this.dictionary[key] = value;
        }

        public int count => this.dictionary.Count;

        public Dictionary<K, V>.KeyCollection keys => this.dictionary.Keys;
        public Dictionary<K, V>.ValueCollection values =>
            this.dictionary.Values;

        public void Add(K key, V value) => this.dictionary.Add(key, value);

        public bool Remove(K key) => this.dictionary.Remove(key);

        public bool TryGetValue(K key, out V value) =>
            this.dictionary.TryGetValue(key, out value);
    }
}
