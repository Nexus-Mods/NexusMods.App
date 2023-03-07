namespace NexusMods.DataModel.Games;

public interface IEADesktopGame : IGame
{
    public IEnumerable<string> EADesktopSoftwareIDs { get; }

}
