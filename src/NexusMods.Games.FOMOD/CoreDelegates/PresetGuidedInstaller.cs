using NexusMods.Abstractions.GuidedInstallers;
using NexusMods.Sdk.Jobs;

namespace NexusMods.Games.FOMOD.CoreDelegates;

/// <summary>
/// A IGuidedInstaller implementation that uses a preset list of steps to make the same choices
/// a user previously made for specific steps.
/// </summary>
public class PresetGuidedInstaller : IGuidedInstaller
{
    private readonly FomodOption[] _steps;
    private int _currentStep = 0;

    /// <summary>
    /// Constructor.
    /// </summary>
    public PresetGuidedInstaller(FomodOption[] steps)
    {
        _steps = steps;
    }

    /// <inheritdoc/>
    public void Dispose() { }

    /// <inheritdoc/>
    public void SetupInstaller(string name) { }

    /// <inheritdoc/>
    public void CleanupInstaller() { }

    /// <inheritdoc/>
    public Task<UserChoice> RequestUserChoice(GuidedInstallationStep installationStep, Percent progress, CancellationToken cancellationToken)
    {
        var step = _steps[_currentStep];
        
        // This looks gross, but it's fairly simple we map through the two trees matching by name, and it's cleaner than 4 nested loops.
        var choices = 
                      from srcGroup in step.groups
                      from installGroup in installationStep.Groups
                      where installGroup.Name == srcGroup.name
                      from srcChoice in srcGroup.choices
                      from installChoice in installGroup.Options
                      where installChoice.Name == srcChoice.name
                      select new SelectedOption(installGroup.Id, installChoice.Id);
        
        _currentStep++;
        return Task.FromResult(new UserChoice(new UserChoice.GoToNextStep(choices.ToArray())));
    }
}
