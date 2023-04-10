using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using FluentAssertions;
using NexusMods.App.UI.LeftMenu.Game;
using NexusMods.App.UI.Windows;
using NexusMods.DataModel.Loadouts;
using NexusMods.Paths;
using NexusMods.UI.Tests.Framework;

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
    public async Task CanOpenTheMainAppWindow()
    {

        await using var host =
            await _app.GetControl<GameLeftMenuView, GameLeftMenuDesignViewModel,
                IGameLeftMenuViewModel>();
        var btn = host.GetViewControl<Button>("LaunchButton");
        btn.Should().NotBeNull();
        host.ViewModel.LaunchButton.Should().NotBeNull();


    }

}
