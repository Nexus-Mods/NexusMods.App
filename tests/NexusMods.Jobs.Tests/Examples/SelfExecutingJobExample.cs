using FluentAssertions;
using JetBrains.Annotations;
using NexusMods.Abstractions.Jobs;
using NexusMods.Paths;
using Xunit;
// ReSharper disable UnusedParameter.Local
namespace NexusMods.Jobs.Tests.Examples;

// When running more complex jobs, you will want to make dedicated job objects.
// For self-contained jobs, use `IJobDefinitionWithStart`.

[PublicAPI]
public class SelfExecutingJobExample(IJobMonitor jobMonitor, TemporaryFileManager temporaryFileManager)
{
    [Fact]
    public async Task RunExample()
    {
        await using var tempFile = temporaryFileManager.CreateFile();
        var somePath = tempFile.Path;

        // Usage
        var job = new ProcessFileJob { FilePath = somePath };
        var jobTask = jobMonitor.Begin<ProcessFileJob, ProcessResult>(job);
        var result = await jobTask;
        
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }
}

[PublicAPI]
public record ProcessResult(bool Success);

// Use records, as jobs should not mutate their own parameters.
// The fields we store here are effectively method parameters passed in
// to run the job.
public record ProcessFileJob : IJobDefinitionWithStart<ProcessFileJob, ProcessResult>
{
    public required AbsolutePath FilePath { get; init; }

    public async ValueTask<ProcessResult> StartAsync(IJobContext<ProcessFileJob> context)
    {
        // Access job definition properties
        var filePath = context.Definition.FilePath;

        // Report progress
        context.SetPercent(Size.Zero, Size.From(100));

        // Process file
        var result = await ProcessFileAsync(filePath, context.CancellationToken);

        context.SetPercent(Size.From(100), Size.From(100));
        return result;
    }

    private static Task<ProcessResult> ProcessFileAsync(AbsolutePath filePath, CancellationToken cancellationToken)
    {
        // await Task.Delay(100, cancellationToken);  // Simulate file processing work
        // Here you would implement the actual file processing logic
        return Task.FromResult(new ProcessResult(Success: true));
    }
}
