namespace HeinzBOTtle.Requirements;

/// <summary>
/// Represents a guild game requirement based on a minimum value of a single node.
/// </summary>
public class SimpleRequirement : Requirement {

    /// <summary>The dot-delimited JSON node from a Hypixel API player response associated with this requirement.</summary>
    public string Node { get; }
    /// <summary>The minimum value of the node necessary to meet this requirement.</summary>
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
