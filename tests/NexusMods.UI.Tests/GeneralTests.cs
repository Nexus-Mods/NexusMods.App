using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Extensions.Hosting.Internal;

namespace NexusMods.UI.Tests;

public class GeneralTests
{
    private readonly AppHelper _helper;

    public GeneralTests(AppHelper helper)
    {
        _helper = helper;
    }

    [Fact]
    public void CanOpenTheMainAppWindow()
    {
        Thread.Sleep(1);



    }

}
