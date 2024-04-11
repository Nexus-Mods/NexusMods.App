namespace NexusMods.CrossPlatform;

internal static class Helper
{
    private static void FireAndForget(this Task task, CancellationToken cancellationToken = default)
    {
        _ = Task.Run(async () => await task, cancellationToken: cancellationToken);
    }

    public static Task AwaitOrForget(this Task task, bool fireAndForget, CancellationToken cancellationToken = default)
    {
        if (fireAndForget)
        {
            task.FireAndForget(cancellationToken: cancellationToken);
            return Task.CompletedTask;
        }
        else
        {
            return task;
        }
    }
}
