using Microsoft.Extensions.Logging;

namespace NexusMods.DurableJobs;

public record State
{
    public CancellationToken Token { get; init; }
}

public class UnitOfWorkActor : Actor<State, IJobMessage>
{
    public UnitOfWorkActor(ILogger logger, State initialState) : base(logger, initialState)
    {
    }


    public override ValueTask<(State, bool)> Handle(State state, IJobMessage message)
    {
        throw new NotImplementedException();
    }
}
