using UnityEngine;
using System.Collections.Generic;

namespace MarcosPereira.MeshManipulation {
    public class Serializable2DList<T> : ScriptableObject, ISerializationCallbackReceiver {
        public List<List<T>> list;

        [SerializeField]
        private List<int> serializableSizes;

        [SerializeField]
        private List<T> serializableItems;

        public static Serializable2DList<T> Create() {
            Serializable2DList<T> instance =
                ScriptableObject.CreateInstance<Serializable2DList<T>>();

            return instance;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() {
            this.serializableSizes = new List<int>(this.list.Count);

            // Make capacity start at 1 per sublist
            this.serializableItems = new List<T>(this.list.Count);

            foreach (List<T> sublist in this.list) {
                this.serializableSizes.Add(sublist.Count);
                this.serializableItems.AddRange(sublist);
            }

            this.list = null;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize() {
            this.list = new List<List<T>>();

            int i = 0;

            foreach (int size in this.serializableSizes) {
                var currentList = new List<T>(size);

                for (int j = 0; j < size; j++) {
                    currentList.Add(this.serializableItems[i]);
                    i++;
                }

                this.list.Add(currentList);
            }

            this.serializableItems = null;
            this.serializableSizes = null;
        }
    }
}
