using System.Diagnostics;
using FluentAssertions;
using NexusMods.Abstractions.CLI;
using NexusMods.Common;
using NexusMods.Paths;
using NexusMods.Updater.Verbs;

namespace NexusMods.Updater.Tests;

public class CopyAppToFolderTests
{
    private readonly CopyAppToFolder _verb;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly UpdaterService _updaterService;

    public CopyAppToFolderTests(TemporaryFileManager temporaryFileManager, CopyAppToFolder copyAppToFolder, UpdaterService updaterService, IRenderer renderer)
    {
        _verb = copyAppToFolder;
        _temporaryFileManager = temporaryFileManager;
        _updaterService = updaterService;
        copyAppToFolder.Renderer = renderer;
    }

    [Fact]
    public async Task FilesAreCopied()
    {
        await using var targetFolder = _temporaryFileManager.CreateFolder();

        var onContinue = targetFolder.Path.Combine("NexusMods.App.exe");

        // ReSharper disable once AccessToDisposedClosure
        Func<Task<int>> act = async () => await _verb.Run(_updaterService.AppFolder, targetFolder, onContinue, -1, CancellationToken.None);

        await act.Should().ThrowAsync<FakeProcessFactory.ExecuteAndDetatchException>("Process was started")
            .Where(e => e.Command.TargetFilePath == onContinue.ToString());

        targetFolder.Path.Combine("NexusMods.App.exe").FileExists.Should().BeTrue();

    }
}
