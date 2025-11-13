using System.Diagnostics;
using System.Threading.Channels;
using NexusMods.ProxyConsole.Messages;
using NexusMods.Sdk.ProxyConsole;
using Spectre.Console;
using SpectreRender = Spectre.Console.Rendering;
using SpectreText = Spectre.Console.Text;
using SpectreTable = Spectre.Console.Table;
using SpectreTableColumn = Spectre.Console.TableColumn;

namespace NexusMods.ProxyConsole;

/// <summary>
/// An adapter for rendering <see cref="IRenderable"/>s to the console using Spectre.Console.
/// </summary>
public class SpectreRenderer : IRenderer
{
    private readonly Spectre.Console.IAnsiConsole _console;
    private Channel<IRenderable>? _progressChannel;
    private Task? _progressTask = null;

    /// <summary>
    /// Wraps the given <see cref="Spectre.Console.IAnsiConsole"/> instance as a <see cref="IRenderer"/>.
    /// </summary>
    /// <param name="console"></param>
    public SpectreRenderer(Spectre.Console.IAnsiConsole console)
    {
        _console = console;
    }

    /// <summary>
    /// Converts the given <see cref="IRenderable"/> to a <see cref="Render.IRenderable"/> that can be
    /// sent to Spectre.Console.
    /// </summary>
    /// <param name="renderable"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async ValueTask<SpectreRender.IRenderable> ToSpectreAsync(IRenderable renderable)
    {
        switch (renderable)
        {
            case NexusMods.Sdk.ProxyConsole.Text text:
                if (text.Arguments.Length == 0)
                {
                    return new SpectreText(text.Template);
                }
                else
                {
                    return new SpectreText(string.Format(text.Template, text.Arguments));
                }
            case NexusMods.Sdk.ProxyConsole.Table table:
                return await ToSpectreAsync(table);
            default:
                throw new NotImplementedException();
        }
    }

    private async ValueTask<SpectreRender.IRenderable> ToSpectreAsync(NexusMods.Sdk.ProxyConsole.Table table)
    {
        var t = new SpectreTable();
        foreach (var column in table.Columns)
        {
            t.AddColumn(new SpectreTableColumn(await ToSpectreAsync(column)));
        }

        foreach (var row in table.Rows)
        {
            var convertedRow = new List<SpectreRender.IRenderable>();
            foreach (var cell in row)
            {
                convertedRow.Add(await ToSpectreAsync(cell));
            }
            t.AddRow(convertedRow.ToArray());
        }
        return t;
    }

    /// <inheritdoc />
    public async ValueTask RenderAsync(IRenderable renderable)
    {
        switch (renderable)
        {
            case StartProgress startProgress:
                StartProgress(startProgress);
                break;
            case StopProgress stopProgress:
                await StopProgress(stopProgress);
                break;
            case CreateProgressTask createProgressTask:
                _progressChannel!.Writer.TryWrite(createProgressTask);
                break;
            case DeleteProgressTask deleteProgressTask:
                _progressChannel!.Writer.TryWrite(deleteProgressTask);
                break;
            case UpdateTask updateTask:
                _progressChannel!.Writer.TryWrite(updateTask);
                break;
            default:
                var spectre = await ToSpectreAsync(renderable);
                _console.Write(spectre);
                break;
        }
        
    }

    private async Task StopProgress(StopProgress _)
    {
        Debug.Assert(_progressChannel != null);
        _progressChannel.Writer.TryComplete();
        await _progressTask!;
    }

    private void StartProgress(StartProgress _)
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
                case CreateProgressTask createProgressTask:
                    var task = context.AddTask(createProgressTask.Text, maxValue: 1.0);
                    tasks.Add(createProgressTask.TaskId, task);
                    break;
                case UpdateTask updateTask:
                    if (!tasks.TryGetValue(updateTask.TaskId, out task))
                        break;
                    task.Increment(updateTask.IncrementProgressBy);
                    break;
                case DeleteProgressTask deleteProgressTask:
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
