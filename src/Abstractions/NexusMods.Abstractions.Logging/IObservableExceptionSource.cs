namespace NexusMods.Abstractions.Logging;

/// <summary>
/// A global source of exceptions that have been observed by the application.
/// This allows for UI and other systems to tap into log messages.
/// </summary>
public interface IObservableExceptionSource
{
    IObservable<LogMessage> Exceptions { get; }
}
