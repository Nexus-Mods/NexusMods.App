namespace NexusMods.Telemetry;

internal record struct ExceptionData(string Type, string Message, string? StackTrace)
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
            result.Add(new ExceptionData(
                Type: exception.GetType().ToString(),
                Message: RedactUtils.Redact(exception.Message),
                StackTrace: exception.StackTrace
            ));
        }

        return result;
    }
}
