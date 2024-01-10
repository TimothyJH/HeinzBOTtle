namespace HeinzBOTtle.Requirements;

/// <summary>
/// Represents a guild game requirement. The information associated with a requirement is read-only.
/// </summary>
public abstract class Requirement {

    /// <summary>The name of Discord guild role associated with this requirement</summary>
    public string Title { get; }
    /// <summary>The clean name of the Hypixel minigame represented by this requirement.</summary>
    public string GameTitle { get; }

    public Requirement(string title, string gameTitle) {
        Title = title;
        GameTitle = gameTitle;
    }

    /// <param name="json">The JSON representing the player to evaluate.</param>
    /// <returns>True if the player represented by the provided JSON meets this requirement, otherwise false.</returns>
    public abstract bool MeetsRequirement(Json json);

}
