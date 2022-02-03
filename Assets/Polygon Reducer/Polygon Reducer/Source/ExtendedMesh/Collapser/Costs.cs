using System.Collections.Generic;
using UnityEngine;
using MarcosPereira.Utility;

namespace MarcosPereira.MeshManipulation {
    public class Costs : ScriptableObject {
        [SerializeField]
        private SerializableSortedSet<Edge> costs;

        [SerializeField]
        private SerializableDictionary<int, float> costByVertex;

        // Used in place of constructor, since this is a ScriptableObject
        public static Costs Create() {
            Costs instance = ScriptableObject.CreateInstance<Costs>();

            instance.costs = SerializableSortedSet<Edge>.Create(Costs.CostComparer());
            instance.costByVertex = SerializableDictionary<int, float>.Create();

            return instance;
        }

        public void Add(Edge edge) {
            // SortedSet does not allow duplicate entries, so when there is
            // a duplicate we increment the cost by the smallest possible
            // amount until it is unique.
            Edge e = edge;
            while (!this.costs.Add(e)) {
                float newCost = Costs.NextFloat(e.cost);
                e = new Edge(newCost, e.fromVertex, e.toVertex);
            }

            this.costByVertex[e.fromVertex] = e.cost;
        }

        public void RemoveByVertex(int i) {
            float cost = this.costByVertex[i];

            // Custom comparer will look only at cost
            if (!this.costs.Remove(new Edge(cost, 0, 0))) {
                throw new System.Exception(
                    $"Failed to remove neighbor cost of {cost}"
                );
            }

            _ = this.costByVertex.Remove(i);
        }

        public Edge PopMinimumCost() {
            Edge min = this.costs.min;

            if (!this.costs.Remove(min)) {
                Debug.LogError(
                    $"Failed to remove smallest cost edge ({min})"
                );
            }

            if (!this.costByVertex.Remove(min.fromVertex)) {
                Debug.LogError(
                    "Failed to remove cost of minimum cost vertex " +
                    $"{min.fromVertex}"
                );
            }

            return min;
        }

        private static Comparer<Edge> CostComparer() =>
            Comparer<Edge>.Create((
                Edge a,
                Edge b
            ) => {
                if (a.cost > b.cost) {
                    return 1;
                }

                if (a.cost < b.cost) {
                    return -1;
                }

                return 0;
            });

        private static float NextFloat(float f) {
            if (f == float.PositiveInfinity) {
                throw new System.Exception(
                    "Can't return next float of positive infinity."
                );
            }

            float nextFloat = f;
            float epsilon = float.Epsilon;

            // On ARM systems, the value of the Epsilon constant is too small to
            // be detected, so it equates to zero. You can define an alternative
            // epsilon value that equals 1.175494351E-38 instead.
            // Source: https://docs.microsoft.com/en-us/dotnet/api/system.single.epsilon?view=net-5.0#platform-notes
            if (epsilon == 0f) {
                epsilon = 1.175494351E-38f;
            }

            // Enable overflow checking that may save us from infinite loops
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/checked
            nextFloat = checked(nextFloat + epsilon);

            while (nextFloat == f) {
                epsilon *= 2;
                nextFloat = checked(nextFloat + epsilon);

                // Prevent infinite loops
                if (nextFloat == float.PositiveInfinity) {
                    throw new System.Exception(
                        "Next float is positive infinity."
                    );
                }
            }

            return nextFloat;
        }
    }
}
