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

public class GeneralTests
{
    private readonly AvaloniaApp _app;
    private readonly IFileSystem _fileSystem;
    private readonly LoadoutManager _loadoutManager;
    private readonly IGameLeftMenuViewModel _vm;

    public GeneralTests(AvaloniaApp helper,
        IFileSystem fileSystem, LoadoutManager loadoutManager, IGameLeftMenuViewModel viewModel)
    {
        _fileSystem = fileSystem;
        _loadoutManager = loadoutManager;
        _app = helper;
        _vm = viewModel;
    }

    [Fact]
    public async Task CanTestTheLaunchButton()
    {

        await using var host =
            await _app.GetControl<LaunchButtonView, LaunchButtonDesignViewModel,
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
        
        await host.OnUi(async () =>
        {
            btn.IsEnabled.Should().BeTrue();
            btn.Command!.Execute(null);
        });
        

        pressed.Should().BeTrue();

    }

}
