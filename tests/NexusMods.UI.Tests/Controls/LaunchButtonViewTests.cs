﻿using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls;
using FluentAssertions;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.DataModel.RateLimiting;
using ReactiveUI;

namespace NexusMods.UI.Tests.Controls;

public class LaunchButtonViewTests : AViewTest<LaunchButtonView, LaunchButtonDesignViewModel, ILaunchButtonViewModel>
{
    private Button? _button;
    private TextBlock? _text;
    private ProgressBar? _progressBar;

    public LaunchButtonViewTests(IServiceProvider provider) : base(provider) { }

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
        var source = new TaskCompletionSource<bool>();
        

        ViewModel.Command = ReactiveCommand.Create<Unit, Unit>(_ =>
        {
            source.SetResult(true);
            return Unit.Default;
        });

        await EventuallyOnUi(() =>
        {
            _button!.Command.Should().Be(ViewModel.Command);
        });

        await Click(_button!);

        (await source.Task.WaitAsync(TimeSpan.FromSeconds(10))).Should().BeTrue();
    }

    [Fact]
    public async Task LabelTextAffectsButtonAndProgressText()
    {
        var text = Random.Shared.Next() + " Text";
        ViewModel.Label = text;
        await EventuallyOnUi(() =>
        {
            _text!.Text.Should().Be(text);
            _progressBar!.ProgressTextFormat.Should().Be(text);
        });
    }

    [Fact]
    public async Task ProgressAffectsProgressBar()
    {
        ViewModel.Progress = Percent.CreateClamped(0.25);

        await EventuallyOnUi(() =>
        {
            _progressBar!.IsIndeterminate.Should().BeFalse();
            _progressBar!.Value.Should().Be(0.25);
        });
        

        ViewModel.Progress = null;

        await EventuallyOnUi(() =>
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

        await OnUi(() =>
        {
            _button!.IsVisible.Should().BeTrue();
            _progressBar!.IsVisible.Should().BeFalse();
        });

        await Click(_button!);

        await EventuallyOnUi(() =>
        {
            _button!.IsVisible.Should().BeFalse();
            _progressBar!.IsVisible.Should().BeTrue();
        });

        tcs.SetResult();
    }

    [Fact]
    public async Task DisablingTheButtonShowsATheProgressBar()
    {
        
        var subject = new Subject<bool>();
        ViewModel.Command = ReactiveCommand.Create(() => { }, subject.StartWith(false));

        await EventuallyOnUi(() =>
        {
            _button!.IsVisible.Should().BeFalse();
            _button!.IsEnabled.Should().BeFalse();
            _progressBar!.IsVisible.Should().BeTrue();
        });
    }

    [Fact]
    public async Task EnabledCommandShouldShowEnabledButton()
    {
        var subject = new Subject<bool>();
        ViewModel.Command = ReactiveCommand.Create(() => { }, subject.StartWith(true));
        ViewModel.Progress = null;

        await EventuallyOnUi(() =>
        {
            _button!.IsVisible.Should().BeTrue();
            _button!.IsEnabled.Should().BeTrue();
            _progressBar!.IsVisible.Should().BeFalse();
            _progressBar!.IsIndeterminate.Should().BeTrue();
        });
    }
}
