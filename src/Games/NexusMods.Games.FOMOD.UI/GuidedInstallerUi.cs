using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using Avalonia.Rendering;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;

namespace NexusMods.Games.FOMOD.UI;

[UsedImplicitly]
public sealed class GuidedInstallerUi : IGuidedInstaller, IDisposable
{
    private readonly IServiceProvider _serviceProvider;

    private IServiceScope? _currentScope;
    private IGuidedInstallerWindowViewModel? _windowViewModel;
    private ReactiveWindow<IGuidedInstallerWindowViewModel>? _window;

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

        // TODO: figure out a better approach than this
        AvaloniaScheduler.Instance.Schedule(
            _windowViewModel,
            AvaloniaScheduler.Instance.Now,
            (_, viewModel) =>
            {
                SetupWindow(viewModel);
                return Disposable.Empty;
            });
    }

    private void SetupWindow(IGuidedInstallerWindowViewModel viewModel)
    {
        _window = new GuidedInstallerWindow
        {
            ViewModel = viewModel
        };

        _window.Show();

        // TODO: cancel installation if the user closes the window
        var closed = Observable.FromEventPattern(
            addHandler => _window.Closed += addHandler,
            removeHandler => _window.Closed -= removeHandler
        );
    }

    public void CleanupInstaller()
    {
        if (_windowViewModel is not null)
        {
            _window?.Close();
            _window = null;

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
        Debug.Assert(_currentScope is not null);
        Debug.Assert(_windowViewModel is not null);

        _windowViewModel.ActiveStepViewModel ??= _currentScope.ServiceProvider.GetRequiredService<IGuidedInstallerStepViewModel>();

        var activeStepViewModel = _windowViewModel.ActiveStepViewModel;
        activeStepViewModel.InstallationStep = installationStep;

        var tcs = new TaskCompletionSource<UserChoice>();
        activeStepViewModel.TaskCompletionSource = tcs;

        await tcs.Task;
        return tcs.Task.Result;
    }

    public void Dispose() => CleanupInstaller();
}
