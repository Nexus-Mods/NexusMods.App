using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Jobs;
using NexusMods.Paths;
// ReSharper disable UnusedParameter.Local
namespace Examples.Jobs;

[PublicAPI]
public static class SelfExecutingJobExample
{
    public static async Task<ProcessResult?> RunExample(IServiceProvider serviceProvider, AbsolutePath somePath)
    {
        var jobMonitor = serviceProvider.GetRequiredService<IJobMonitor>();
        
        // Usage
        var job = new ProcessFileJob { FilePath = somePath };
        var jobTask = jobMonitor.Begin<ProcessFileJob, ProcessResult>(job);
        var result = await jobTask;
        return result;
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
    
    private static async Task<ProcessResult> ProcessFileAsync(AbsolutePath filePath, CancellationToken cancellationToken)
    {
        // Simulate file processing work
        await Task.Delay(2000, cancellationToken);
        
        return new ProcessResult(Success: true);
    }
}
