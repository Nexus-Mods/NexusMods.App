using System.Diagnostics;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.Implementations;

namespace NexusMods.Abstractions.Cli;

/// <summary>
/// A wrapper for progress task communication
/// </summary>
public class ProgressTask : IAsyncDisposable
{
    private readonly IRenderer _renderer;
    private readonly Guid _taskId;
    private double _progress = 0;
    private readonly double? _maxValue;

    internal ProgressTask(IRenderer renderer, Guid taskId, double? maxValue)
    {
        _renderer = renderer;
        _taskId = taskId;
        _maxValue = maxValue;
    }

    /// <summary>
    /// Deletes the progress task
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _renderer.RenderAsync(new DeleteProgressTask { TaskId = _taskId });
    }

    /// <summary>
    /// Sets the progress of this task to the given value, in a range of 0 to 1
    /// </summary>
    public async Task SetProgress(double progress)
    {
        var increment = progress - _progress;
        _progress = progress;
        await _renderer.RenderAsync(new UpdateTask { TaskId = _taskId, IncrementProgressBy = increment });
    }
    
    /// <summary>
    /// Increments the progress of this task by the given value, in a range of 0 to 1
    /// </summary>
    /// <param name="increment"></param>
    public async Task IncrementProgress(double increment)
    {
        _progress += increment;
        await _renderer.RenderAsync(new UpdateTask { TaskId = _taskId, IncrementProgressBy = increment });
    }
    
    /// <summary>
    /// Increments the total "value" of this task by the given increment, the progress is then calculated as a percentage of the total value
    /// </summary>
    public async Task Increment(double increment)
    {
        Debug.Assert(_maxValue.HasValue);
        var progress = (double)increment / _maxValue!.Value;
        await IncrementProgress(progress);
    }
}
