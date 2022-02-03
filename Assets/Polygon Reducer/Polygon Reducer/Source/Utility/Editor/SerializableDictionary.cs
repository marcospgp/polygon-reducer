using UnityEngine;
using System.Collections.Generic;

namespace MarcosPereira.Utility {
    public class SerializableDictionary<K, V> : ScriptableObject, ISerializationCallbackReceiver {
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

        public static SerializableDictionary<K, V> Create() {
            SerializableDictionary<K, V> instance =
                ScriptableObject.CreateInstance<SerializableDictionary<K, V>>();

            return instance;
        }

        public void Add(K key, V value) => this.dictionary.Add(key, value);

        public V this[K key] {
            get => this.dictionary[key];
            set => this.dictionary[key] = value;
        }
    }
}
