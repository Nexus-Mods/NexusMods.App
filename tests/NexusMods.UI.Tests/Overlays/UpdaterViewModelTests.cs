using NexusMods.App.UI.Overlays.Updater;

namespace NexusMods.UI.Tests.Overlays;

public class UpdaterViewModelTests : AVmTest<UpdaterViewModel, IUpdaterViewModel>
{
    public UpdaterViewModelTests(IServiceProvider provider) : base(provider) { }

    [Fact]
    public async Task CanCheckForReleases()
    {
        await ConcreteVm.ShouldShow();
    }
}
