using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DurableJobs;

namespace NexusMods.DurableJobs;

/// <summary>
/// An actor for a restartable persistent job.
/// </summary>
public class JobActor : Actor<OrchestrationState, IJobMessage>
{
    public JobActor(ILogger logger, OrchestrationState initialState) : base(logger, initialState)
    {
    }
    
    internal JobReport GetJobReport()
    {
        return new JobReport(State.Id, State.Job.GetType(), State.Arguments);
    }
    
    public override async ValueTask<(OrchestrationState, bool)> Handle(OrchestrationState state, IJobMessage message)
    {
        var shouldContinue = true;
        switch (message)
        {
            case RunMessage:
                var context = new OrchestrationContext
                {
                    JobManager = state.Manager!,
                    JobId = state.Id,
                    History = state.History,
                };
                try
                {
                    var result = await ((AOrchestration)state.Job).Run(context, state.Arguments);
                    state.Manager!.FinalizeJob(state, result, false);
                }
                catch (WaitException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    state.Manager!.FinalizeJob(state, ex.Message, true);
                }
                break;
            case JobResultMessage rm:
                var entry = state.History[rm.Offset];
                entry.Result = rm.Result;
                entry.Status = rm.IsFailure ? JobStatus.Failed : JobStatus.Completed;
                Post(RunMessage.Instance);
                break;
            case CancelMessage cancelMessage:
                throw new NotImplementedException();
            default:
                throw new InvalidOperationException("Unknown message type " + message.GetType());
        }
        state = (OrchestrationState)state.Manager!.SaveState(state);
        return (state, shouldContinue);
    }
}
