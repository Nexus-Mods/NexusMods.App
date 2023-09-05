using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using Avalonia.Rendering;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI;
using NexusMods.Common;
using NexusMods.Common.GuidedInstaller;
using ReactiveUI;

namespace NexusMods.Games.FOMOD.UI;

[UsedImplicitly]
public sealed class GuidedInstallerUi : IGuidedInstaller, IDisposable
{
    private readonly IServiceProvider _serviceProvider;

    private IServiceScope? _currentScope;
    private IGuidedInstallerWindowViewModel? _windowViewModel;
    private ReactiveWindow<IGuidedInstallerWindowViewModel>? _window;

    private readonly EventWaitHandle _waitHandle = new ManualResetEvent(initialState: false);

    public GuidedInstallerUi(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetupInstaller(string windowName)
    {
        Debug.Assert(_currentScope is null);
        _currentScope = _serviceProvider.CreateScope();

        _windowViewModel = _currentScope.ServiceProvider.GetRequiredService<IGuidedInstallerWindowViewModel>();
        _windowViewModel.WindowName = windowName;

        // NOTE(erri120): AvaloniaScheduler has to be used to do work on the UI thread
        AvaloniaScheduler.Instance.Schedule(
            this,
            AvaloniaScheduler.Instance.Now,
            (_, state) =>
            {
                SetupWindow(state);
                _waitHandle.Set();

                return Disposable.Empty;
            });

        // NOTE(erri120): We need to wait for the window to be created.
        // Otherwise, the _window field isn't set when we want to request the
        // first user choice.
        _waitHandle.WaitOne(timeout: TimeSpan.FromSeconds(1));
        _waitHandle.Reset();
    }

    private static void SetupWindow(GuidedInstallerUi state)
    {
        state._window = new GuidedInstallerWindow
        {
            ViewModel = state._windowViewModel
        };

        state._window.Show();

        // TODO: cancel installation if the user closes the window
        // var closed = Observable.FromEventPattern(
        //     addHandler => state._window.Closed += addHandler,
        //     removeHandler => state._window.Closed -= removeHandler
        // );
    }

    public void CleanupInstaller()
    {
        if (_window is not null)
        {
            _window.Close();
            _window = null;
        }

        if (_windowViewModel is not null)
        {
            _windowViewModel.CloseCommand.Execute();
            _windowViewModel.ActiveStepViewModel = null;
            _windowViewModel = null;
        }

        _currentScope?.Dispose();
        _currentScope = null;
    }

    public async Task<UserChoice> RequestUserChoice(
        GuidedInstallationStep installationStep,
        CancellationToken cancellationToken)
    {
        // TODO: do something with the cancellation token
        Debug.Assert(_currentScope is not null);
        Debug.Assert(_windowViewModel is not null);
        Debug.Assert(_window is not null);

        var tcs = new TaskCompletionSource<UserChoice>();

        // NOTE: AvaloniaScheduler has to be used to do work on the UI thread
        AvaloniaScheduler.Instance.Schedule(
            (_currentScope, _windowViewModel, tcs, installationStep),
            AvaloniaScheduler.Instance.Now,
            (_, tuple) =>
            {
                SetupStep(tuple._currentScope, tuple._windowViewModel, tuple.tcs, tuple.installationStep);
                return Disposable.Empty;
            });

        await tcs.Task;
        return tcs.Task.Result;
    }

    private static void SetupStep(
        IServiceScope currentScope,
        IGuidedInstallerWindowViewModel viewModel,
        TaskCompletionSource<UserChoice> tcs,
        GuidedInstallationStep installationStep)
    {
        viewModel.ActiveStepViewModel ??= currentScope.ServiceProvider.GetRequiredService<IGuidedInstallerStepViewModel>();

        var activeStepViewModel = viewModel.ActiveStepViewModel;
        activeStepViewModel.InstallationStep = installationStep;

        activeStepViewModel.TaskCompletionSource = tcs;
    }

    public void Dispose()
    {
        CleanupInstaller();
        _waitHandle.Dispose();
    }
}
