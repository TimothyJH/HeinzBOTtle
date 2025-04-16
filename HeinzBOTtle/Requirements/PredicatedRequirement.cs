namespace HeinzBOTtle.Requirements;

/// <summary>
/// Represents a guild game requirement based on the evaluation of a predicate.
/// </summary>
public class PredicatedRequirement : Requirement {

    /// <summary>The predicate that should be satisfied for the player to meet this requirement.</summary>
    public Predicate<Json> Predicate { get; }

    public PredicatedRequirement(string title, string gameTitle, Predicate<Json> predicate) : base(title, gameTitle) {
        Predicate = predicate;
    }

    public override bool MeetsRequirement(Json json) {
        return Predicate.Invoke(json);
    }

}
