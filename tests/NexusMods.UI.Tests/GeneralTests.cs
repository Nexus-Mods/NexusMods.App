using System.Reactive;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using FluentAssertions;
using NexusMods.App.UI.LeftMenu.Game;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Windows;
using NexusMods.DataModel.Loadouts;
using NexusMods.Paths;
using NexusMods.UI.Tests.Framework;
using ReactiveUI;

namespace NexusMods.UI.Tests;

public class GeneralTests : AUiTest
{
    public GeneralTests(IServiceProvider provider) : base(provider)
    {
    }

    [Fact]
    public async Task CanTestTheLaunchButton()
    {

        await using var host =
            await App.GetControl<LaunchButtonView, LaunchButtonDesignViewModel,
                ILaunchButtonViewModel>();
        var btn = await host.GetViewControl<Button>("LaunchButton");
        btn.Should().NotBeNull();

        var pressed = false; 
        var cmd = ReactiveCommand.Create<Unit, Unit>(_ =>
        {
            pressed = true;
            return Unit.Default;
        });

        host.ViewModel.Command = cmd;
        
        
        await OnUi(async () =>
        {
            btn.IsEnabled.Should().BeTrue();
            btn.Command!.Execute(null);
        });

        await Eventually(async () =>
        {
            pressed.Should().BeTrue();
        });
        



    }

}
