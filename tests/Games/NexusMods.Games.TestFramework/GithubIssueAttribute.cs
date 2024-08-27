namespace NexusMods.Games.TestFramework;

/// <summary>
/// A documentation attribute to link to a Github issue to a test method.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class GithubIssueAttribute : Attribute
{
    /// <summary>
    /// The GithubId of the issue
    /// </summary>
    public int GithubId { get; } 
    
    public Uri GithubUri => new("https://github.com/Nexus-Mods/NexusMods.App/issues/" + GithubId);

    public GithubIssueAttribute(int githubId)
    {
        GithubId = githubId;
    }

    public override string ToString()
    {
        return "Github Issue: " + GithubId;
    }
}
