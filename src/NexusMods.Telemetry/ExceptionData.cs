namespace NexusMods.Telemetry;

internal record struct ExceptionData(string Type, string Message, string? StackTrace = null)
{
    public static List<ExceptionData> Create(Exception exception, List<ExceptionData>? result = null)
    {
        result ??= [];

        if (exception is AggregateException aggregateException)
        {
            foreach (var innerException in aggregateException.InnerExceptions)
            {
                Create(innerException, result);
            }
        }
        else
        {
            result.Add(From(exception));
        }

        return result;
    }

    private static ExceptionData From(Exception exception)
    {
        var type = exception.GetType().ToString();
        var originalMessage = RedactUtils.Redact(exception.Message);

        var message = originalMessage;
        var stackTrace = exception.StackTrace;

        if (!string.IsNullOrEmpty(stackTrace))
        {
            var firstMethod = ExtractFirstMethod(exception.StackTrace);
            message = firstMethod ?? originalMessage;
            stackTrace = firstMethod is null ? stackTrace : $"{originalMessage}\n{stackTrace}";
        }

        return new ExceptionData(
            Type: type,
            Message: message,
            StackTrace: stackTrace
        );
    }

    /// <summary>
    /// Extracts the first fully qualified method name from the stack trace.
    /// </summary>
    private static string? ExtractFirstMethod(ReadOnlySpan<char> stackTrace)
    {
        // Input:
        //   at NexusMods.Abstractions.Loadouts.Synchronizers.ALoadoutSynchronizer.<>c__DisplayClass44_0.<<ReindexState>b__0>d.MoveNext() in /_/src/NexusMods.Abstractions.Loadouts.Synchronizers/ALoadoutSynchronizer.cs:line 1336
        // --- End of stack trace from previous location ---
        //   at System.Threading.Tasks.Parallel.<>c__53`1.<<ForEachAsync>b__53_0>d.MoveNext()
        // --- End of stack trace from previous location ---
        //   at NexusMods.Abstractions.Loadouts.Synchronizers.ALoadoutSynchronizer.ReindexState(GameInstallation installation, Boolean ignoreModifiedDates, IConnection connection, Transaction tx) in /_/src/NexusMods.Abstractions.Loadouts.Synchronizers/ALoadoutSynchronizer.cs:line 1280
        // Output:
        // NexusMods.Abstractions.Loadouts.Synchronizers.ALoadoutSynchronizer.<>c__DisplayClass44_0.<<ReindexState>b__0>d.MoveNext()

        const string prefix = "at ";
        const string suffix = " in ";

        var lines = stackTrace.EnumerateLines();
        foreach (var line in lines)
        {
            var span = line.Trim();

            var prefixIndex = span.IndexOf(prefix, StringComparison.Ordinal);
            if (prefixIndex != 0) continue;

            span = span.Slice(start: prefix.Length);

            var suffixIndex = span.IndexOf(suffix, StringComparison.Ordinal);
            if (suffixIndex != -1)
            {
                span = span.Slice(start: 0, length: suffixIndex);
            }

            var trimmed = span.Trim();
            return trimmed.ToString();
        }

        return null;
    }
}
