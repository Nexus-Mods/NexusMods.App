using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers;

public class TestBindingTarget : IModContentBindingTarget
{
    public List<IUnlinkableItem> unlinkables;

    public GamePath Bind(IUnlinkableItem unlinkable)
    {
        throw new NotImplementedException();
    }
}
