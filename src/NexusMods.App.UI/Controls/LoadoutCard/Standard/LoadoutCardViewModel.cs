using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DynamicData;
using DynamicData.Aggregation;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.LoadoutBadge;
using NexusMods.App.UI.Extensions;
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
        LoadoutVal = loadout;
        _logger = serviceProvider.GetRequiredService<ILogger<LoadoutCardViewModel>>();
        var applyService = serviceProvider.GetRequiredService<ISynchronizerService>();
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
        
        CloneLoadoutCommand = ReactiveCommand.CreateFromTask(() =>
            {
                return CopyLoadout(loadout);
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
                .BindToVM(this, x => x.HumanizedLoadoutCreationTime)
                .DisposeWith(d);

            interval.Select(_ =>
                    {
                        if (loadout.LastAppliedDateTime.TryGet(out var lastAppliedTime))
                        {
                            return FormatLastAppliedTime(lastAppliedTime);
                        }
                        return "";
                    }
                )
                .OnUI()
                .BindToVM(this, x => x.HumanizedLoadoutLastApplyTime)
                .DisposeWith(d);

            Loadout.Observe(conn, loadout.Id)
            	.OffUi()
                .Select(l => FormatNumMods(LoadoutUserFilters.GetItems(l).Count()))
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
    
    public Loadout.ReadOnly LoadoutVal { get; }
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
    public ReactiveCommand<Unit, Unit> CloneLoadoutCommand { get; } 
    public ReactiveCommand<Unit, Unit> DeleteLoadoutCommand { get; }
    
    [Reactive] public bool IsLastLoadout { get; set; } = true;
    
    
    private static string FormatNumMods(int numMods)
    {
        return string.Format(Language.LoadoutCardViewModel_FormatNumMods_Mods__0_, numMods);
    }
    
    private static string FormatCreatedTime(DateTimeOffset creationTime)
    {
        return string.Format(Language.LoadoutCardViewModel_CreationTimeConverter_Created__0_, creationTime.Humanize());
    }
    
    private static string FormatLastAppliedTime(DateTimeOffset lastAppliedTime)
    {
        var stringTime = lastAppliedTime == DateTimeOffset.MinValue ? Language.HumanizedDateTime_Never : lastAppliedTime.Humanize();
        return string.Format(Language.LoadoutCardViewModel_FormatLastAppliedTime_Last_applied__0_, stringTime);
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
                _logger.LogError(ex, "While loading game image for {GameName}", source.Game.Name);
                return null;
            }
        });
    }
    
    private static Task DeleteLoadout(Loadout.ReadOnly loadout)
    {
        return Task.Run(() => loadout.InstallationInstance.GetGame().Synchronizer.DeleteLoadout(loadout));
    }

    private static Task CopyLoadout(Loadout.ReadOnly loadout)
    {
        return Task.Run(() => loadout.InstallationInstance.GetGame().Synchronizer.CopyLoadout(loadout));
    }
    
}
