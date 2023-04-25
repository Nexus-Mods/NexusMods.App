using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using NexusMods.Benchmarks.Interfaces;

namespace NexusMods.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[BenchmarkInfo("High Performance Logging", "Compares the different methods of logging")]
public class HighPerformanceLogging : IBenchmark
{
    public const string LoggingMessageWithReferenceTypes = "This uses reference types: {a} and {b}";
    public const string LoggingMessageWithValueTypes = "This uses value types: {a} and {b}";

    [Params(LogLevel.Trace, LogLevel.Information)]
    public LogLevel MinimumLogLevel { get; set; }

    private ILogger _logger;
    private string _referenceA;
    private string _referenceB;
    private Guid _valueA;
    private Guid _valueB;

    [GlobalSetup]
    public void Setup()
    {
        _logger = new DummyLogger(MinimumLogLevel);
        _valueA = Guid.NewGuid();
        _valueB = Guid.NewGuid();
        _referenceA = _valueA.ToString();
        _referenceB = _valueB.ToString();
    }

    [Benchmark]
    public void Default_ReferenceTypes()
    {
        _logger.LogTrace(LoggingMessageWithReferenceTypes, _referenceA, _referenceB);
    }

    [Benchmark]
    public void Default_ValueTypes()
    {
        _logger.LogTrace(LoggingMessageWithValueTypes, _valueA, _valueB);
    }

    [Benchmark]
    public void LoggerMessage_ReferenceTypes()
    {
        _logger.WithReferenceTypes(_referenceA, _referenceB);
    }

    [Benchmark]
    public void LoggerMessage_ValueTypes()
    {
        _logger.WithValueTypes(_valueA, _valueB);
    }

    private class DummyLogger : ILogger
    {
        private readonly LogLevel _minLogLevel;

        public DummyLogger(LogLevel minLogLevel)
        {
            _minLogLevel = minLogLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter) { }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _minLogLevel <= logLevel;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;
    }
}

internal static partial class LoggingExtensions
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Trace,
        Message = HighPerformanceLogging.LoggingMessageWithReferenceTypes)]
    public static partial void WithReferenceTypes(
        this ILogger logger,
        string a,
        string b);

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Trace,
        Message = HighPerformanceLogging.LoggingMessageWithValueTypes)]
    public static partial void WithValueTypes(
        this ILogger logger,
        Guid a,
        Guid b);
}
