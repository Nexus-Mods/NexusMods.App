namespace NexusMods.DataModel.Games;

public interface IOriginGame : IGame
{
    public IEnumerable<string> OriginGameIds { get; }
}