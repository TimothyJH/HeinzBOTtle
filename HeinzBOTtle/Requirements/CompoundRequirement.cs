namespace HeinzBOTtle.Requirements;

/// <summary>
/// Represents a guild game requirement based on two minimum values, each of a single node.
/// </summary>
public class CompoundRequirement : Requirement {

    /// <summary>The first dot-delimited JSON node from a Hypixel API player response associated with this requirement.</summary>
    public string Node1 { get; }
    /// <summary>The minimum value of the first node necessary to meet this requirement.</summary>
    public double Min1 { get; }
    /// <summary>The second dot-delimited JSON node from a Hypixel API player response associated with this requirement.</summary>
    public string Node2 { get; }
    /// <summary>The minimum value of the second node necessary to meet this requirement.</summary>
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
