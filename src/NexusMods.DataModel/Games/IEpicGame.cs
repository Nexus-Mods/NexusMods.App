namespace NexusMods.DataModel.Games;

public interface IEpicGame : IGame
{
    public IEnumerable<string> EpicCatalogItemId { get; }
}