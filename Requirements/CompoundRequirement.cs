namespace HeinzBOTtle.Requirements {

    class CompoundRequirement : Requirement {

        public string Node1 { get; }
        public double Min1 { get; }
        public string Node2 { get; }
        public double Min2 { get; }

        public CompoundRequirement(string title, string gameTitle, string node1, double min1, string node2, double min2) : base(title, gameTitle) {
            Node1 = node1;
            Min1 = min1;
            Node2 = node2;
            Min2 = min2;
        }

        public override bool MeetsRequirement(Json json) {
            double value1 = json.GetDouble(Node1) ?? 0.0;
            double value2 = json.GetDouble(Node2) ?? 0.0;
            return value1 >= Min1 && value2 >= Min2;
        }

    }

}
