using System.Reactive;
using Avalonia.Controls;
using FluentAssertions;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.DataModel.RateLimiting;
using NexusMods.UI.Tests.Framework;
using ReactiveUI;

namespace NexusMods.UI.Tests.Controls;

public class LaunchButtonViewTests : AViewTest<LaunchButtonView, LaunchButtonDesignViewModel, ILaunchButtonViewModel>
{
    private Button? _button;
    private TextBlock? _text;
    private ProgressBar? _progressBar;
    
    public LaunchButtonViewTests(IServiceProvider provider, AvaloniaApp app) : base(provider, app) { }
    
    protected override async Task PostInitializeSetup()
    {
        await base.PostInitializeSetup();
        
        _button = await GetControl<Button>("LaunchButton");
        _text = await GetControl<TextBlock>("LaunchText");
        _progressBar = await GetControl<ProgressBar>("ProgressBarControl");
    }

    [Fact]
    public async Task ClickingTheButtonFiresTheCommand()
    {
        var clicked = false;
        ViewModel.Command = ReactiveCommand.Create<Unit, Unit>(_ =>
        { 
            clicked = true;
            return Unit.Default;
        });

        clicked.Should().BeFalse();
        
        await Host.Click(_button!);
        clicked.Should().BeTrue();
    }

    [Fact]
    public async Task LabelTextAffectsButtonAndProgressText()
    {
        var text = Random.Shared.Next() + " Text";
        ViewModel.Label = text;
        await Host.OnUi(async () =>
        {
            _text!.Text.Should().Be(text);
            _progressBar!.ProgressTextFormat.Should().Be(text);
        });
    }
    
    [Fact]
    public async Task ProgressAffectsProgressBar()
    {
        ViewModel.Progress = Percent.CreateClamped(0.25);
        await Host.OnUi(async () =>
        {
            _progressBar!.IsIndeterminate.Should().BeFalse();
            _progressBar!.Value.Should().Be(0.25);
        });
        
        ViewModel.Progress = null;
        
        await Host.OnUi(async () =>
        {
            _progressBar!.IsIndeterminate.Should().BeTrue();
        });
    }

    [Fact]
    public async Task InProgressTaskAffectsControlVisibility()
    {
        var tcs = new TaskCompletionSource();
        ViewModel.Command = ReactiveCommand.CreateFromTask(async () =>
        {
            await tcs.Task;
            return Unit.Default;
        });

        await Host.OnUi(async () =>
        {
            _button!.IsVisible.Should().BeTrue();
            _progressBar!.IsVisible.Should().BeFalse();
        });
        
        await Host.Click(_button!);

        await Host.OnUi(async () =>
        {
            _button!.IsVisible.Should().BeFalse();
            _progressBar!.IsVisible.Should().BeTrue();
        });

    }
}
