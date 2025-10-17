using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GuidedInstallers;
using NexusMods.App.UI;
using NexusMods.Sdk.Jobs;
using ReactiveUI;

namespace NexusMods.Games.FOMOD.UI;

[UsedImplicitly]
public sealed class GuidedInstallerUi : IGuidedInstaller
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CompositeDisposable _compositeDisposable;

    private IServiceScope? _currentScope;
    private ReactiveWindow<IGuidedInstallerWindowViewModel>? _window;

    private readonly EventWaitHandle _waitHandle = new ManualResetEvent(initialState: false);

    public GuidedInstallerUi(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _compositeDisposable = new CompositeDisposable();
    }

    public void SetupInstaller(string windowName)
    {
        Debug.Assert(_window is null);
        Debug.Assert(_currentScope is null);
        _currentScope = _serviceProvider.CreateScope();

        var windowViewModel = _currentScope.ServiceProvider.GetRequiredService<IGuidedInstallerWindowViewModel>();
        windowViewModel.WindowName = windowName;

        OnUi((windowViewModel, this), tuple =>
        {
            var (innerViewModel, state) = tuple;
            SetupWindow(innerViewModel, state);
            state._waitHandle.Set();
        });

        // NOTE(erri120): We need to wait for the window to be created.
        // Otherwise, the _window field isn't set when we want to request the
        // first user choice.
        _waitHandle.WaitOne(timeout: TimeSpan.FromSeconds(1));
        _waitHandle.Reset();
    }

    private static void SetupWindow(
        IGuidedInstallerWindowViewModel windowViewModel,
        GuidedInstallerUi state)
    {
        state._window = new GuidedInstallerWindow
        {
            ViewModel = windowViewModel
        };

        state._window.Show();

        Observable
            .FromEventPattern(
                addHandler => state._window.Closed += addHandler,
                removeHandler => state._window.Closed -= removeHandler
            )
            .SubscribeWithErrorLogging(eventPattern =>
            {
                if (eventPattern.Sender is not GuidedInstallerWindow window) return;
                var tcs = window.ViewModel?.ActiveStepViewModel?.TaskCompletionSource;
                if (tcs is null) return;
                tcs.TrySetResult(new UserChoice(new UserChoice.CancelInstallation()));
            })
            .DisposeWith(state._compositeDisposable);
    }

    public void CleanupInstaller()
    {
        if (_window is not null)
        {
            OnUi(_window, window =>
            {
                if (window.ViewModel is not null) window.ViewModel.ActiveStepViewModel = null;
                window.Close();
            });
            _window = null;
        }

        _currentScope?.Dispose();
        _currentScope = null;
    }

    public async Task<UserChoice> RequestUserChoice(
        GuidedInstallationStep installationStep,
        Percent progress,
        CancellationToken cancellationToken)
    {
        Debug.Assert(_currentScope is not null);
        Debug.Assert(_window is not null);

        var tcs = new TaskCompletionSource<UserChoice>();

        OnUi((_currentScope, _window, tcs, installationStep, progress), tuple =>
        {
            SetupStep(tuple._currentScope, tuple._window, tuple.tcs, tuple.installationStep, tuple.progress);
        });

        await tcs.Task;
        return tcs.Task.Result;
    }

    private static void SetupStep(
        IServiceScope currentScope,
        IViewFor<IGuidedInstallerWindowViewModel> window,
        TaskCompletionSource<UserChoice> tcs,
        GuidedInstallationStep installationStep,
        Percent progress)
    {
        var viewModel = window.ViewModel!;
        viewModel.ActiveStepViewModel ??= new GuidedInstallerStepViewModel(currentScope.ServiceProvider);

        var activeStepViewModel = viewModel.ActiveStepViewModel;
        activeStepViewModel.ModName = viewModel.WindowName;
        activeStepViewModel.InstallationStep = installationStep;
        activeStepViewModel.TaskCompletionSource = tcs;
        activeStepViewModel.Progress = progress;
    }

    private static void OnUi<TState>(TState state, Action<TState> action)
    {
        // NOTE: AvaloniaScheduler has to be used to do work on the UI thread
        AvaloniaScheduler.Instance.Schedule(
            (action, state),
            AvaloniaScheduler.Instance.Now,
            (_, tuple) =>
            {
                var (innerAction, innerState) = tuple;
                innerAction(innerState);
                return Disposable.Empty;
            });
    }

    public void Dispose()
    {
        CleanupInstaller();
        _waitHandle.Dispose();
        _compositeDisposable.Dispose();
    }
}
