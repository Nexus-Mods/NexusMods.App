using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Extensions.Hosting.Internal;
using NexusMods.App.UI.Controls.Spine;

namespace NexusMods.UI.Tests;

public class GeneralTests
{
    private readonly AppHelper _helper;

    public GeneralTests(AppHelper helper)
    {
        _helper = helper;
    }

    [Fact]
    public async Task CanOpenTheMainAppWindow()
    {
        await using var host = await _helper.MakeHost<Spine, ISpineViewModel>();


        var window = host.Window;



    }

}
