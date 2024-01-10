namespace HeinzBOTtle.Requirements;

/// <summary>
/// Represents a guild game requirement based on a minimum sum of two nodes' values.
/// </summary>
public class AdditiveRequirement : Requirement {

    /// <summary>The first dot-delimited JSON node from a Hypixel API player response associated with this requirement.</summary>
    public string Node1 { get; }
    /// <summary>The second dot-delimited JSON node from a Hypixel API player response associated with this requirement.</summary>
    public string Node2 { get; }
    /// <summary>The minimum sum of the nodes' values necessary to meet this requirement.</summary>
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
