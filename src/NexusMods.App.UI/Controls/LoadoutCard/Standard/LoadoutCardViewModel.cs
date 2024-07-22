using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Alias;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.App.UI.Controls.LoadoutBadge;
using NexusMods.App.UI.Resources;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.LoadoutCard;

public class LoadoutCardViewModel : AViewModel<ILoadoutCardViewModel>, ILoadoutCardViewModel
{
    private readonly ILogger<LoadoutCardViewModel> _logger;
    
    public LoadoutCardViewModel(Loadout.ReadOnly loadout, IConnection conn, IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<LoadoutCardViewModel>>();
        var applyService = serviceProvider.GetRequiredService<IApplyService>();
        LoadoutName = loadout.Name;
        var badgeVm = serviceProvider.GetRequiredService<ILoadoutBadgeViewModel>();
        badgeVm.LoadoutValue = loadout;
        LoadoutBadgeViewModel = badgeVm;
        
        DeleteLoadoutCommand = ReactiveCommand.CreateFromTask(() =>
            {
                IsDeleting = true;
                return DeleteLoadout(loadout);
            }
        );
        
        this.WhenActivated(d =>
        {
            Observable.FromAsync(() => LoadImage(loadout.InstallationInstance))
                .OnUI()
                .BindToVM(this, x => x.LoadoutImage)
                .DisposeWith(d);
            
            Loadout.Observe(conn, loadout.Id)
                .Select(l => l.Name)
                .OnUI()
                .BindToVM(this, x => x.LoadoutName)
                .DisposeWith(d);
            
            applyService.LastAppliedRevisionFor(loadout.InstallationInstance)
                .Select(rev => rev.Id == loadout.LoadoutId)
                .OnUI()
                .BindToVM(this, x => x.IsLoadoutApplied)
                .DisposeWith(d);

            var interval = Observable.Interval(TimeSpan.FromMinutes(1)).Prepend(0);
            
            interval.Select(_ => FormatCreatedTime(loadout.GetCreatedAt()))
                .OnUI()
                .BindToVM(this, x => x.HumanizedLoadoutCreationTime);
            
            // TODO: implement getting LastApplied time by updating Apply detection code to use Revision changes instead of tx changes 
            // interval.Select(_ => FormatCreatedTime(loadout.LastAppliedAt))
            //     .OnUI()
            //     .BindToVM(this, x => x.HumanizedLoadoutLastApplyTime);

            Loadout.Observe(conn, loadout.Id)
                .Select(l => FormatNumMods(l.Mods.Count))
                .OnUI()
                .BindToVM(this, x => x.LoadoutModCount)
                .DisposeWith(d);
            
            Loadout.ObserveAll(conn)
                .Filter(l => l.IsVisible() && l.Installation.Path == loadout.Installation.Path && l.LoadoutId != loadout.LoadoutId)
                .Count()
                .Select(count => count == 0)
                .OnUI()
                .BindToVM(this, x => x.IsLastLoadout)
                .DisposeWith(d);

        });
        
    }

    public ILoadoutBadgeViewModel LoadoutBadgeViewModel { get; private set; }
    [Reactive] public string LoadoutName { get; private set; }
    [Reactive] public IImage? LoadoutImage { get; private set; } 
    [Reactive] public bool IsLoadoutApplied { get; private set; } = false;
    [Reactive] public string HumanizedLoadoutLastApplyTime { get; private set; } = "";
    [Reactive] public string HumanizedLoadoutCreationTime { get; private set; } = "";
    [Reactive] public string LoadoutModCount { get; private set; } = "";
    [Reactive] public bool IsDeleting { get;  private set; } = false;
    public bool IsSkeleton => false;
    public required ReactiveCommand<Unit, Unit> VisitLoadoutCommand { get; init; }
    public required ReactiveCommand<Unit, Unit> CloneLoadoutCommand { get; init; } 
    public ReactiveCommand<Unit, Unit> DeleteLoadoutCommand { get; }
    
    [Reactive] public bool IsLastLoadout { get; set; } = true;
    
    
    private string FormatNumMods(int numMods)
    {
        return string.Format(Language.LoadoutCardViewModel_FormatNumMods_Mods__0_, numMods);
    }
    
    private string FormatCreatedTime(DateTime creationTime)
    {
        return string.Format(Language.LoadoutCardViewModel_CreationTimeConverter_Created__0_, creationTime.Humanize());
    }
    
    private string FormatLastAppliedTime(DateTime creationTime)
    {
        return string.Format(Language.LoadoutCardViewModel_FormatLastAppliedTime_Last_applied__0_, creationTime.Humanize());
    }
    private async Task<Bitmap?> LoadImage(GameInstallation source)
    {
        return await Task.Run(async () =>
        {
            try
            {
                var stream = await source.GetGame().Icon.GetStreamAsync();
                return new Bitmap(stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "While loading game image for {GameName} v{Version}", source.Game.Name, source.Version);
                return null;
            }
        });
    }
    
    private Task DeleteLoadout(Loadout.ReadOnly loadout)
    {
        return Task.Run(() => loadout.InstallationInstance.GetGame().Synchronizer.DeleteLoadout(loadout.InstallationInstance, loadout.LoadoutId));
    }
    
}
