using Microsoft.Extensions.Logging;

namespace NexusMods.CrossPlatform;

public static class Helper
{
    public static void FireAndForget(
        this Task task,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await task;
            }
            catch (TaskCanceledException)
            {
                // ignored
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception in fire-and-forget task");
            }
        }, cancellationToken: cancellationToken);
    }

    public static Task AwaitOrForget(
        this Task task,
        ILogger logger,
        bool fireAndForget,
        CancellationToken cancellationToken = default)
    {
        if (!fireAndForget) return task;

        task.FireAndForget(logger, cancellationToken: cancellationToken);
        return Task.CompletedTask;
    }
}
