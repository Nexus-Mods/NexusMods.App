namespace NexusMods.DataModel.Games;

/// <summary>
/// When implemented, enables support for detecting installations of your
/// <see cref="AGame"/> managed by Epic.
/// </summary>
/// <remarks>
/// Game detection is automatic provided the correct <see cref="EpicCatalogItemId"/>
/// is applied.
/// </remarks>
public interface IEpicGame : IGame
{
    /// <summary>
    /// Unique catalog item id returned from Epic.
    /// </summary>
    public IEnumerable<string> EpicCatalogItemId { get; }
}
