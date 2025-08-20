using System.Numerics;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts.Synchronizers.Rules;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// A very specialized composite updater that attempts to smooth out the progress when used with RunGroupings
/// </summary>
public class RunActionsCompositeUpdater : CompositeProgress
{
    private readonly Actions _validSteps;

    public RunActionsCompositeUpdater(int steps, Actions validSteps, Action<Percent> progressCallback) : base(steps, progressCallback)
    {
        _validSteps = validSteps;
    }


    private const ushort StepsToConsider = (ushort)(Actions.ExtractToDisk | Actions.DeleteFromDisk | Actions.BackupFile | Actions.AddReifiedDelete | Actions.IngestFromDisk);
    public static RunActionsCompositeUpdater Create(Dictionary<GamePath, SyncNode> syncTree, Action<Percent> progressCallback)
    {
        Actions actions = 0x0;
        // OR together all the actions
        foreach (var node in syncTree.Values)
        {
            actions |= node.Actions;
        }
        
        var validSteps = actions & (Actions)StepsToConsider;
        var totalSteps = BitOperations.PopCount((uint)validSteps);
        return new RunActionsCompositeUpdater(totalSteps, validSteps, progressCallback);
    }

    /// <summary>
    /// Advances to the next step, but only if the given action is one being considered by this updater 
    /// </summary>
    public void NextStep(Actions action)
    {
        if ((action & _validSteps) == 0)
            return;
        
        base.NextStep();
    }
}
