using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Loadouts;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Toolbars;

public class DefaultLoadoutToolbarViewModel : AViewModel<IDefaultLoadoutToolbarViewModel>, IDefaultLoadoutToolbarViewModel
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<DefaultLoadoutToolbarViewModel> _logger;
    private readonly LoadoutManager _loadoutManager;

    [Reactive]
    public string Caption { get; set; } = "";

    [Reactive]
    public LoadoutId LoadoutId { get; set; } = Initializers.LoadoutId;

    public DefaultLoadoutToolbarViewModel(ILogger<DefaultLoadoutToolbarViewModel> logger,
        IFileSystem fileSystem, LoadoutManager loadoutManager, LoadoutRegistry loadoutRegistry)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _loadoutManager = loadoutManager;

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(x => x.LoadoutId)
                .SelectMany(loadoutRegistry.RevisionsAsLoadouts)
                .WhereNotNull()
                .Select(loadout => loadout!.Installation.Game.Name)
                .BindTo(this, x => x.Caption)
                .DisposeWith(d);
        });
    }


    public async Task StartManualModInstall(string path)
    {
        var file = AbsolutePath.FromFullPath(path, _fileSystem);
        if (!_fileSystem.FileExists(file))
        {
            _logger.LogError("File {File} does not exist, not installing mod",
                file);
            return;
        }

        var _ = Task.Run(async () =>
        {
            await _loadoutManager.InstallModAsync(LoadoutId, file, file.FileName);
        });
    }
}
