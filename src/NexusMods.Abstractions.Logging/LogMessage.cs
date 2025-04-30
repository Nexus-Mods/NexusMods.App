namespace NexusMods.Abstractions.Logging;

/// <summary>
/// A generic log message. Exists as a record to de-couple exceptions and log messages
/// from the backend logging targets
/// </summary>
/// <param name="Exception">The attached Exception (if any)</param>
/// <param name="Message">The log's message</param>
public record LogMessage(Exception? Exception, string Message)
{
    
}
