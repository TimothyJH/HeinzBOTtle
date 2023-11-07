namespace HeinzBOTtle.Requirements;

public abstract class Requirement {

    public string Title { get; }
    public string GameTitle { get; }

    public Requirement(string title, string gameTitle) {
        Title = title;
        GameTitle = gameTitle;
    }

    public abstract bool MeetsRequirement(Json json);

}
