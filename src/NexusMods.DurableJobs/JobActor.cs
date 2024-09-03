using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DurableJobs;

namespace NexusMods.DurableJobs;

/// <summary>
/// An actor for a restartable persistent job.
/// </summary>
public class JobActor : Actor<JobState, IJobMessage>
{
    public JobActor(ILogger logger, JobState initialState) : base(logger, initialState)
    {
    }


    public override async ValueTask<(JobState, bool)> Handle(JobState state, IJobMessage message)
    {
        var shouldContinue = true;
        switch (message)
        {
            case RunMessage:
                var context = new Context
                {
                    JobManager = state.Manager!,
                    JobId = state.Id,
                    History = state.History,
                };
                try
                {
                    var result = await state.Job.Run(context, state.Arguments);
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
        state = state.Manager!.SaveState(state);
        return (state, shouldContinue);
    }
}
