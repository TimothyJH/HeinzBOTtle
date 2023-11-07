namespace HeinzBOTtle.Requirements;

public class AdditiveRequirement : Requirement {

    public string Node1 { get; }
    public string Node2 { get; }
    public double Min { get; }

    public AdditiveRequirement(string title, string gameTitle, string node1, string node2, double min) : base(title, gameTitle) {
        Node1 = node1;
        Node2 = node2;
        Min = min;
    }

    public override bool MeetsRequirement(Json json) {
        double value1 = json.GetDouble(Node1) ?? 0.0;
        double value2 = json.GetDouble(Node2) ?? 0.0;
        return value1 + value2 >= Min;
    }

}
