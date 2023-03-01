using FomodInstaller.Interface;
using FomodInstaller.Interface.ui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NexusMods.FOMOD.Tests;

class PluginDelegates : IPluginDelegates
{
    public Task<string[]> GetAll(bool activeOnly)
    {
        return Task.FromResult(new string[0]);
    }

    public Task<bool> IsActive(string pluginName)
    {
        return Task.FromResult(true);
    }

    public Task<bool> IsPresent(string pluginName)
    {
        return Task.FromResult(true);
    }
}

class UIDelegates : IUIDelegates
{
    private ILogger<UIDelegates> _logger;

    private Action<bool, int> _cont;
    private int _currentStep;

    /// <summary>
    /// constructor
    /// </summary>
    public UIDelegates(ILogger<UIDelegates> logger)
    {
        _logger = logger;
        _cont = nop;
    }

    public void EndDialog()
    {
        _logger.LogInformation("End dialog");
    }

    public void ReportError(string title, string message, string details)
    {
        _logger.LogError("A bad thing happened: {}: {} ({})", title, message, details);
    }

    public void StartDialog(string moduleName, HeaderImage image, Action<int, int, int[]> select, Action<bool, int> cont, Action cancel)
    {
        _logger.LogInformation("Start dialog {}", moduleName);
        _cont = cont;
    }

    public void UpdateState(InstallerStep[] installSteps, int currentStep)
    {
        _currentStep = currentStep;
        var step = installSteps[currentStep];
        _logger.LogInformation("Current step {}:", step.name);
        foreach (var group in step.optionalFileGroups.group)
        {
            _logger.LogInformation("Group {} ({}):", group.name, group.type);
            foreach (var option in group.options) {
                _logger.LogInformation("Option {} ({}): {}", option.name, option.description, option.selected);
            }
        }
        _cont(true, currentStep);
    }

    private void nop(bool forward, int current)
    {
    }
}

public class MockDelegates : ICoreDelegates
{
    public IPluginDelegates plugin { get; init; } = new PluginDelegates();

    public IContextDelegates context => throw new NotImplementedException();

    public IIniDelegates ini => throw new NotImplementedException();

    public IUIDelegates ui { get; init; }

    public MockDelegates(IServiceProvider provider)
    {
        ui = ActivatorUtilities.CreateInstance<UIDelegates>(provider);
    }
}
