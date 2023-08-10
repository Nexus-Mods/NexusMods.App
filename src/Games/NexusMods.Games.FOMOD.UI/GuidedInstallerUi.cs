using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common.GuidedInstaller;

namespace NexusMods.Games.FOMOD.UI;

[UsedImplicitly]
public sealed class GuidedInstallerUi : IGuidedInstaller, IDisposable
{
    private readonly IServiceProvider _serviceProvider;

    private IServiceScope? _currentScope;
    private IGuidedInstallerWindowViewModel? _windowViewModel;

    public GuidedInstallerUi(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetupInstaller(string windowName)
    {
        Debug.Assert(_currentScope is null);
        _currentScope = _serviceProvider.CreateScope();

        // TODO: create/show window?
        _windowViewModel = _currentScope.ServiceProvider.GetRequiredService<IGuidedInstallerWindowViewModel>();
        _windowViewModel.WindowName = windowName;
    }

    public void CleanupInstaller()
    {
        if (_windowViewModel is not null)
        {
            // TODO: dispose & close window?
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
