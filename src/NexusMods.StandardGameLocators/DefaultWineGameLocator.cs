using GameFinder.Wine;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// <see cref="AWineGameLocator{TPrefix}"/> for <see cref="WinePrefix"/>.
/// </summary>
public class DefaultWineGameLocator : AWineGameLocator<WinePrefix>
{
    /// <inheritdoc/>
    public DefaultWineGameLocator(IServiceProvider serviceProvider) : base(serviceProvider) { }
}
