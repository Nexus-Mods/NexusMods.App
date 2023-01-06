using NexusMods.CLI;
using NexusMods.DataModel.RateLimiting;
using Spectre.Console;

namespace NexusMods.App.CLI.Renderers;

/// <summary>
/// IRenderer that renders verb output to the console.
/// </summary>
public class Spectre : IRenderer
{
    private readonly IResource[] _resources;
    public Spectre(IEnumerable<IResource> resources)
    {
        _resources = resources.ToArray();
    }
    public string Name => "console";
    public void RenderBanner()
    {
        //AnsiConsole.Write(new FigletText("NexusMods.App") {Color = NexusColor});
    }

    public async Task<T> WithProgress<T>(CancellationToken token, Func<Task<T>> f, bool showSize = true)
    {
        var tcs = new TaskCompletionSource<T>();

        ProgressColumn[] columns;

        if (showSize)
        {
            columns = new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new RemainingTimeColumn(),
                new TransferSpeedColumn(),
                new SpinnerColumn(Spinner.Known.Dots)
            };
        }
        else
        {
            columns = new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn(Spinner.Known.Dots)
            };
        }

        await AnsiConsole.Progress()
            .HideCompleted(true)
            .Columns(columns)
            .StartAsync(async ctx =>
        {
            
            try
            {
                var innerTask = Task.Run(f, token);

                var tasks = new Dictionary<(IResource, ulong), ProgressTask>();

                while (!token.IsCancellationRequested && !innerTask.IsCompleted)
                {
                    var jobs = _resources.SelectMany(r => r.Jobs).ToDictionary(r => (r.Resource, r.Id));
                    foreach (var (id, job) in jobs)
                    {
                        if (tasks.TryGetValue(id, out var task))
                        {
                            if (job is IJob<Paths.Size> sj)
                            {
                                task.Increment((ulong)sj.Current - task.Value);
                            }
                            else
                            {
                                task.Increment((double)job.Progress - task.Percentage);
                            }
                        }
                        else
                        {
                            ProgressTask newTask;
                            if (job is IJob<Paths.Size> sj)
                            {
                                newTask = ctx.AddTask(job.Description, true, (ulong)sj.Size);
                                newTask.Increment((ulong)sj.Current);
                            }
                            else
                            {
                                newTask = ctx.AddTask(job.Description, true, 1.0d);
                                newTask.Increment((double)job.Progress);
                            }
                            tasks[(job.Resource, job.Id)] = newTask;
                        }
                    }

                    foreach (var (id, task) in tasks)
                    {
                        if (jobs.TryGetValue(id, out _)) continue;
                        task.StopTask();
                        tasks.Remove(id);
                    }


                    await Task.Delay(100, token);
                }

                tcs.SetResult(await innerTask);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return await tcs.Task;
    }
    
    private Color NexusColor = new(0xda, 0x8e, 0x35);

    public async Task Render<T>(T o)
    {
        switch (o)
        {
            case NexusMods.CLI.DataOutputs.Table t:
                await RenderTable(t);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private Task RenderTable(NexusMods.CLI.DataOutputs.Table table)
    {

        var ot = new Table();
        foreach (var column in table.Columns)
            ot.AddColumn(new TableColumn(new Text(column, new Style(foreground: NexusColor))));

        foreach (var row in table.Rows)
        {
            ot.AddRow(row.Select(r => r.ToString()).ToArray()!);
        }
        AnsiConsole.Write(ot);
        return Task.CompletedTask;
    }
}