namespace MarcosPereira.PolygonReducer {
    // Would prefer this struct to be readonly, but then it wouldn't be
    // serializable.
    [System.Serializable]
    public struct Edge {
        public float cost;
        public int fromVertex;
        public int toVertex;

        public Edge(float cost, int fromVertex, int toVertex) {
            this.cost = cost;
            this.fromVertex = fromVertex;
            this.toVertex = toVertex;
        }
    }
}
