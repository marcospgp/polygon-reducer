namespace MarcosPereira.MeshManipulation {
    [System.Serializable]
    public readonly struct Edge {
        public readonly float cost;
        public readonly int fromVertex;
        public readonly int toVertex;

        public Edge(float cost, int fromVertex, int toVertex) {
            this.cost = cost;
            this.fromVertex = fromVertex;
            this.toVertex = toVertex;
        }
    }
}
