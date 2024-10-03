using System.Diagnostics;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.GuidedInstallers;

namespace NexusMods.Games.FOMOD.CoreDelegates;

/// <summary>
/// A IGuidedInstaller implementation that uses a preset list of steps to make the same choices
/// a user previously made for specific steps.
/// </summary>
public class PresetGuidedInstaller : IGuidedInstaller
{
    private readonly FomodOption[] _steps;
    private int _currentStep = 0;

    public PresetGuidedInstaller(FomodOption[] steps)
    {
        _steps = steps;
    }
    
    public void Dispose()
    {
    }

    public void SetupInstaller(string name)
    {
    }

    public void CleanupInstaller()
    {
    }

    public Task<UserChoice> RequestUserChoice(GuidedInstallationStep installationStep, Percent progress, CancellationToken cancellationToken)
    {
        var step = _steps[_currentStep];
        
        // This looks gross, but it's fairly simple we map through the two trees matching by name
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
