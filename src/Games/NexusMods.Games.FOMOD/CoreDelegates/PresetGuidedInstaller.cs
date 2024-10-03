using System.Diagnostics;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.GuidedInstallers;

namespace NexusMods.Games.FOMOD.CoreDelegates;

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
        
        List<SelectedOption> choices = [];
        
        foreach (var (srcGroup, installGroup) in step.groups.Zip(installationStep.Groups))
        {
            if (srcGroup.name != installGroup.Name)
            {
                throw new InvalidOperationException("Group names do not match.");
            }

            foreach (var choice in srcGroup.choices)
            {
                var installChoice = installGroup.Options[choice.idx];
                if (installChoice.Name != choice.name)
                {
                    throw new InvalidOperationException("Choice names do not match.");
                }
                choices.Add(new SelectedOption(installGroup.Id, installChoice.Id));
            }
            
        }
        _currentStep++;
        return Task.FromResult(new UserChoice(new UserChoice.GoToNextStep(choices.ToArray())));
    }
}
