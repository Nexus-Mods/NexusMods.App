using System.Diagnostics;
using System.Threading.Channels;
using NexusMods.ProxyConsole.Abstractions;
using Spectre.Console;
using Impl = NexusMods.ProxyConsole.Abstractions.Implementations;
using Render = Spectre.Console.Rendering;

namespace NexusMods.ProxyConsole;

/// <summary>
/// An adapter for rendering <see cref="Abstractions.IRenderable"/>s to the console using Spectre.Console.
/// </summary>
public class SpectreRenderer : Abstractions.IRenderer
{
    private readonly IAnsiConsole _console;
    private Channel<IRenderable>? _progressChannel;
    private Task? _progressTask = null;

    /// <summary>
    /// Wraps the given <see cref="IAnsiConsole"/> instance as a <see cref="Abstractions.IRenderer"/>.
    /// </summary>
    /// <param name="console"></param>
    public SpectreRenderer(IAnsiConsole console)
    {
        _console = console;
    }

    /// <summary>
    /// Converts the given <see cref="Abstractions.IRenderable"/> to a <see cref="Render.IRenderable"/> that can be
    /// sent to Spectre.Console.
    /// </summary>
    /// <param name="renderable"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async ValueTask<Render.IRenderable> ToSpectreAsync(Abstractions.IRenderable renderable)
    {
        switch (renderable)
        {
            case Impl.Text text:
                if (text.Arguments.Length == 0)
                {
                    return new Text(text.Template);
                }
                else
                {
                    return new Text(string.Format(text.Template, text.Arguments));
                }
            case Impl.Table table:
                return await ToSpectreAsync(table);
            default:
                throw new NotImplementedException();
        }
    }

    private async ValueTask<Render.IRenderable> ToSpectreAsync(Impl.Table table)
    {
        var t = new Table();
        foreach (var column in table.Columns)
        {
            t.AddColumn(new TableColumn(await ToSpectreAsync(column)));
        }

        foreach (var row in table.Rows)
        {
            var convertedRow = new List<Render.IRenderable>();
            foreach (var cell in row)
            {
                convertedRow.Add(await ToSpectreAsync(cell));
            }
            t.AddRow(convertedRow.ToArray());
        }
        return t;
    }

    /// <inheritdoc />
    public async ValueTask RenderAsync(Abstractions.IRenderable renderable)
    {
        switch (renderable)
        {
            case Impl.StartProgress startProgress:
                StartProgress(startProgress);
                break;
            case Impl.StopProgress stopProgress:
                await StopProgress(stopProgress);
                break;
            case Impl.CreateProgressTask createProgressTask:
                _progressChannel!.Writer.TryWrite(createProgressTask);
                break;
            case Impl.DeleteProgressTask deleteProgressTask:
                _progressChannel!.Writer.TryWrite(deleteProgressTask);
                break;
            case Impl.UpdateTask updateTask:
                _progressChannel!.Writer.TryWrite(updateTask);
                break;
            default:
                var spectre = await ToSpectreAsync(renderable);
                _console.Write(spectre);
                break;
        }
        
    }

    private async Task StopProgress(Impl.StopProgress _)
    {
        Debug.Assert(_progressChannel != null);
        _progressChannel.Writer.TryComplete();
        await _progressTask!;
    }

    private void StartProgress(Impl.StartProgress _)
    {
        Debug.Assert(_progressChannel == null);
        _progressChannel = Channel.CreateUnbounded<IRenderable>();
        _progressTask = _console.Progress()
            .HideCompleted(true)
            .Columns(
                new SpinnerColumn(), 
                new PercentageColumn(), 
                new ProgressBarColumn(), 
                new RemainingTimeColumn(), 
                new TaskDescriptionColumn {Alignment = Justify.Left}
            )
            .StartAsync(ProgressTask);
    }

    private async Task ProgressTask(ProgressContext context)
    {
        var tasks = new Dictionary<Guid, ProgressTask>();
        await foreach (var renderable in _progressChannel!.Reader.ReadAllAsync())
        {
            switch (renderable)
            {
                case Impl.CreateProgressTask createProgressTask:
                    var task = context.AddTask(createProgressTask.Text, maxValue: 1.0);
                    tasks.Add(createProgressTask.TaskId, task);
                    break;
                case Impl.UpdateTask updateTask:
                    if (!tasks.TryGetValue(updateTask.TaskId, out task))
                        break;
                    task.Increment(updateTask.IncrementProgressBy);
                    break;
                case Impl.DeleteProgressTask deleteProgressTask:
                    if (!tasks.TryGetValue(deleteProgressTask.TaskId, out task))
                        break;
                    task.StopTask();
                    tasks.Remove(deleteProgressTask.TaskId);
                    break;
            }
        }
    }

    /// <inheritdoc />
    public ValueTask ClearAsync()
    {
        _console.Clear();
        return ValueTask.CompletedTask;
    }

}
