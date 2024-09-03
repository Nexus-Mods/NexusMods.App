using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DurableJobs;

namespace NexusMods.DurableJobs;

internal class UnitOfWorkActor : Actor<UnitOfWorkState, IJobMessage>
{
    public UnitOfWorkActor(ILogger logger, UnitOfWorkState initialState) : base(logger, initialState)
    {
    }
    
    public override ValueTask<(UnitOfWorkState, bool)> Handle(UnitOfWorkState state, IJobMessage message)
    {
        switch (message)
        {
            case RunMessage:
                if (state.RunningTask != null)
                    return ValueTask.FromResult((state, true));
                state = (UnitOfWorkState)state.Manager!.SaveState(state);
                state.RunningTask = Task.Run(() => RunJob(state));
                return ValueTask.FromResult((state, true));
                break;
            case SelfFinished selfFinished:
                state.Manager!.FinalizeJob(state, selfFinished.Result, selfFinished.IsFailure);
                return ValueTask.FromResult((state, false));
                break;
            default:
                throw new NotSupportedException("Unknown message type " + message.GetType());
        }

        return ValueTask.FromResult((state, true));
    }

    private async Task RunJob(UnitOfWorkState state)
    {
        try
        {
            var result = await ((AUnitOfWork)state.Job)!.Start(state.Arguments, state.CancellationTokenSource.Token);
            Post(new SelfFinished(result, false));
        }
        catch (Exception ex)
        {
            Post(new SelfFinished(ex.Message, true));
        }
    }
}
