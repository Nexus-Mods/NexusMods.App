using System.Diagnostics;
using CliWrap;
using NexusMods.Abstractions.CLI;
using NexusMods.Common;
using NexusMods.Paths;

namespace NexusMods.Updater.Verbs;

public class CopyAppToFolder : AVerb<AbsolutePath, AbsolutePath, AbsolutePath, int>, IRenderingVerb
{
    public CopyAppToFolder(IProcessFactory processFactory)
    {
        _processFactory = processFactory;
    }

    public IRenderer Renderer { get; set; } = null!;

    public static VerbDefinition Definition => new("copy-app-to-folder",
        "Copies the app to the specified folder, used by the auto-updater.",
        new OptionDefinition[]
    {
        new OptionDefinition<AbsolutePath>("-f", "--from", "The source path to copy the app from."),
        new OptionDefinition<AbsolutePath>("-t", "--to", "The destination path to copy the app to."),
        new OptionDefinition<AbsolutePath>("-c", "--on-complete", "The file to run when the copy is complete."),
        new OptionDefinition<int>("-p", "-process-id", "The folder id to copy the app to.")
    });

    private readonly IProcessFactory _processFactory;


    public async Task<int> Run(AbsolutePath from, AbsolutePath to, AbsolutePath onComplete, int processId, CancellationToken token)
    {
        while (Process.GetProcesses().FirstOrDefault(p => p.Id == processId) != null)
        {
            await Renderer.Render($"Waiting for process {processId} to exit...");
            await Task.Delay(500, token);
        }

        await Renderer.Render($"Copying app from {from} to {to}...");

        var markerFile = to.Combine(Constants.UpdateMarkerFile);

        foreach (var file in from.EnumerateFiles())
        {
            if (file == markerFile)
                continue;

            var relativePath = file.RelativeTo(from);
            var toFile = to.Combine(relativePath);
            toFile.Parent.CreateDirectory();
            await Renderer.Render($"Copying {relativePath}");
            await using var fromStream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            await using var toStream = toFile.Open(FileMode.Create, FileAccess.Write, FileShare.None);
            await fromStream.CopyToAsync(toStream, token);
        }

        markerFile.Delete();

        var command = new Command(onComplete.ToString())
            .WithWorkingDirectory(onComplete.Parent.ToString());

        _ = _processFactory.ExecuteAndDetach(command);
        return 0;
    }
}
