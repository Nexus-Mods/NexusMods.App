# Jobs Examples

This folder contains examples demonstrating how to use the NexusMods.App Jobs
system for background task management with progress tracking, cancellation
support, and error handling.

## Usage Examples

### Creating a Simple Job Definition

Simple job definitions (using the `Begin` method with a lambda) are used when
you want to handle the execution logic in the same method where you call
`Begin`.

The job definition serves as context/data, while the actual work is done in the
lambda callback right where you need it.

See [**SimpleJobDefinitionExample.cs**](../../../tests/NexusMods.Jobs.Tests/Examples/SimpleJobDefinitionExample.cs).

### Self-Executing Job

A job with entirely self-contained logic/context, to fire and (sometimes)
forget.

The fields you store in the job definition are effectively method parameters
passed in to run the job.

See [**SelfExecutingJobExample.cs**](../../../tests/NexusMods.Jobs.Tests/Examples/SelfExecutingJobExample.cs).

## Best Practices

### Cancellation and Error Handling

Always call `context.YieldAsync()` around expensive, time-consuming code to
support job cancellation.

**For Job Callers:** Use `JobMonitor.Cancel()`, `CancelGroup()`, or
`CancelAll()` to cancel jobs.

**For Job Implementations:** Use `YieldAsync()` to check for cancellation at
natural breakpoints.

**Jobs Cancelling Themselves:** Jobs can cancel themselves using
`context.CancelAndThrow()` when conditions require termination.

**Interactive Cancellation:** Jobs may interact with external components (like
UI dialogs) that can cancel. Propagate external `OperationCanceledException`
through `context.CancelAndThrow()`.

See [**CancellationExample.cs**](../../../tests/NexusMods.Jobs.Tests/Examples/BestPractices/CancellationExample.cs) and
[**InteractiveCancellationExample.cs**](../../../tests/NexusMods.Jobs.Tests/Examples/BestPractices/InteractiveCancellationExample.cs).

### Progress Reporting

Use percentage for determinate progress when total work is known. For
indeterminate progress, use `Size.One` as maximum to avoid division by zero.

See [**DeterminateProgressExample.cs**](../../../tests/NexusMods.Jobs.Tests/Examples/BestPractices/DeterminateProgressExample.cs)
and [**IndeterminateProgressExample.cs**](../../../tests/NexusMods.Jobs.Tests/Examples/BestPractices/IndeterminateProgressExample.cs).

### Pausing and Resuming Jobs

Pausing uses the same mechanism as cancellation (cancellation tokens), but paused jobs can be resumed while cancelled jobs cannot.

Jobs must call `context.YieldAsync()` to respect pause requests.

When resumed, jobs restart from their entry point.
Therefore, to properly support resuming, you must persist some state inside a class field.

See [**PauseResumeExample.cs**](../../../tests/NexusMods.Jobs.Tests/Examples/BestPractices/PauseResumeExample.cs).

### Job State Management

Jobs can expose internal state for external monitoring via `IPublicJobStateData`.

```csharp
// Public interface - subset of internal state
public interface IReadOnlyHttpDownloadState : IPublicJobStateData
{
    Optional<Size> ContentLength { get; }
    Size TotalBytesDownloaded { get; }
}

// Internal state - includes both public and private properties
internal sealed class HttpDownloadState : IReadOnlyHttpDownloadState
{
    public Optional<Size> ContentLength { get; set; }          // ← Exposed
    public Size TotalBytesDownloaded { get; set; }             // ← Exposed
    
    public Optional<EntityTagHeaderValue> ETag { get; set; }   // ← Internal only
    public Optional<bool> AcceptRanges { get; set; }           // ← Internal only
}
```

Implementation:

```csharp
public class HttpDownloadJob : IJobDefinition<AbsolutePath>
{
    private readonly HttpDownloadState _state = new();
    public IPublicJobStateData? GetJobStateData() => _state;
    // ...
}

// Usage
var downloadState = job.GetJobStateData<IReadOnlyHttpDownloadState>();
```

Usually, the read-only interface is a subset of the full internal state; which is sometimes also used across suspend/resume cycles.

See [**JobStateManagementExample.cs**](../../../tests/NexusMods.Jobs.Tests/Examples/BestPractices/JobStateManagementExample.cs).

### Factory Methods

Often you want to fire a job right away after it is created. In this case,
make `Create()` helper functions to encapsulate the job creation and job
starting logic as one.

Factory methods encapsulate dependency injection and job configuration,
creating reusable job creation patterns.

See [**FactoryMethodExample.cs**](../../../tests/NexusMods.Jobs.Tests/Examples/BestPractices/FactoryMethodExample.cs).

## General Guidelines

- Use `record` types for job definitions as they should not mutate parameters
- Handle resource cleanup within job execution logic before completion
- Follow existing codebase patterns

## Caveats

!!! warning "Job Disposal Not Implemented"

    While jobs can implement `IDisposable` or `IAsyncDisposable`, they're not
    actually disposed by the job system. If you need resource cleanup, handle
    it within the job's execution logic before completion.

## Common Patterns

- Use `record` types for job definitions as they should not mutate their
  parameters
- Call `context.YieldAsync()` around expensive operations to support
  cancellation
- Use `SetPercent()` with `Size.From()` for progress reporting
- Access job data through `context.Definition`

## Related Documentation

For comprehensive architecture details and core interfaces, see the
[Jobs System Documentation](../../../docs/developers/development-guidelines/JobsSystem.md).
