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
        var redacted = RedactUtils.Redact(exception.Message);
        var stackTrace = exception.StackTrace;
        var message = redacted;
        var stackTraceIsMessage = false;

        if (!string.IsNullOrEmpty(stackTrace))
        {
            var span = stackTrace.AsSpan();
            var lines = span.EnumerateLines();
            foreach (var line in lines)
            {
                const string prefix = "at ";
                const string suffix = " in ";

                var trimmed = line.Trim();
                var prefixIndex = trimmed.IndexOf(prefix, StringComparison.Ordinal);
                if (prefixIndex != 0) continue;
                trimmed = trimmed.Slice(start: prefix.Length);

                var suffixIndex = trimmed.IndexOf(suffix, StringComparison.Ordinal);
                if (suffixIndex != -1)
                {
                    trimmed = trimmed.Slice(start: 0, length: suffixIndex);
                }

                message = trimmed.Trim().ToString();
                stackTraceIsMessage = true;
                break;
            }
        }

        return new ExceptionData(
            Type: type,
            Message: message,
            StackTrace: stackTraceIsMessage ? $"{redacted}\n{stackTrace}" : stackTrace
        );
    }
}
