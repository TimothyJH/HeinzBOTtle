namespace HeinzBOTtle.Requirements;

public class SimpleRequirement : Requirement {

    public string Node { get; }
    public double Min { get; }

    public SimpleRequirement(string title, string gameTitle, string node, double min) : base(title, gameTitle) {
        Node = node;
        Min = min;
    }

    public override bool MeetsRequirement(Json json) {
        double value = json.GetDouble(Node) ?? 0.0;
        return value >= Min;
    }

}
