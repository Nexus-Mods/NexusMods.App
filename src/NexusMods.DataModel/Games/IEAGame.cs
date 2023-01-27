namespace NexusMods.DataModel.Games;

public interface IEAGame : IGame
{
    public IEnumerable<string> EASoftwareIDs { get; }

}