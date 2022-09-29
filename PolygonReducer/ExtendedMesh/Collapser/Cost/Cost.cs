using MarcosPereira.PolygonReducer.CostFunctions;

namespace MarcosPereira.PolygonReducer {
    public readonly struct Cost {
        private readonly ICost costFunction;

        private Cost(ICost costFunction) {
            this.costFunction = costFunction;
        }

        public static Cost Default { get => new Cost(new PreserveDetailCost()); }

        public static Cost RemoveDetail { get => new Cost(new RemoveDetailCost()); }

        public float Get(int from, int to, ExtendedMesh m) =>
            this.costFunction.Get(from, to, m);
    }
}
