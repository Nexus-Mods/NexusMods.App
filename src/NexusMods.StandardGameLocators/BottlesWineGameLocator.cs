using GameFinder.Wine.Bottles;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// <see cref="AWineGameLocator{TPrefix}"/> for <see cref="BottlesWinePrefix"/>.
/// </summary>
public class BottlesWineGameLocator : AWineGameLocator<BottlesWinePrefix>
{
    /// <inheritdoc/>
    public BottlesWineGameLocator(IServiceProvider serviceProvider) : base(serviceProvider) { }
}
