namespace MarcosPereira.PolygonReducer.CostFunctions {
    public interface ICost {
        public float Get(int from, int to, ExtendedMesh m);
    }
}
