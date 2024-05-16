using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.ModInfo.Error;
using NexusMods.App.UI.Controls.ModInfo.Loading;
using NexusMods.App.UI.Controls.ModInfo.ModFiles;
using NexusMods.App.UI.Pages.ModInfo.Types;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.ModInfo;

public class ModInfoViewModel : APageViewModel<IModInfoViewModel>, IModInfoViewModel
{
    [Reactive]
    public LoadoutId LoadoutId { get; set; }
    
    [Reactive]
    public ModId ModId { get; set; }
    
    [Reactive]
    public CurrentModInfoSection Section { get; set; }

    [Reactive]
    public IViewModelInterface SectionViewModel { get; set; }

    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _conn;
    private bool _isInvalid;
    private Dictionary<CurrentModInfoSection, IViewModelInterface> _cache = new();

    public ModInfoViewModel(IWindowManager windowManager, IServiceProvider serviceProvider, IConnection conn) : base(windowManager)
    {
        _serviceProvider = serviceProvider;
        _conn = conn;
        TabIcon = IconValues.Mods;
        SectionViewModel = new DummyLoadingViewModel();
        
        this.WhenActivated(delegate(CompositeDisposable dp)
        {
            TabTitle = GetModName(out _isInvalid);
            if (_isInvalid)
            {
                SectionViewModel = new DummyErrorViewModel();
            }
            else
            {
                // TODO: Reduce latency here if possible, by assigning on UI thread
                //       if the VM is cached.
                this.WhenAnyValue(x => x.Section)
                    .OffUi()
                    .Select(CreateNewPage)
                    .OnUI()
                    .Subscribe(x => SectionViewModel = x)
                    .DisposeWith(dp);
            }
        });
    }
    
    public void SetContext(ModInfoPageContext context)
    {
        if (!_isInvalid)
            IModInfoViewModel.SetContextImpl(this, context);
    }

    private IViewModelInterface CreateNewPage(CurrentModInfoSection section)
    {
        if (_cache.TryGetValue(section, out var cached))
            return cached;
        
        switch (section)
        {
            case CurrentModInfoSection.Files:
                var vm = _serviceProvider.GetRequiredService<IModFilesViewModel>();
                vm.Initialize(LoadoutId, ModId, IdBundle);
                _cache.Add(section, vm);
                return vm;
            default:
                throw new ArgumentOutOfRangeException(nameof(section), section, null);
        }
    }

    private string GetModName(out bool isInvalid)
    {
        try
        {
            isInvalid = false;
            return _conn.Db.Get(ModId)!.Name;
        }
        catch (KeyNotFoundException ex)
        {
            // The user deleted this mod and restarted the app while having this panel open.
            // In theory this could also be thrown for invalid loadout, but then you'd lose the whole workspace,
            // so in this context, that's impossible to happen.
            isInvalid = true;
            
            var log = _serviceProvider.GetRequiredService<ILogger<ModInfoViewModel>>();
            log.LogError(ex, "Failed to Get Mod Name, Because ModId {0} no longer exists.", ModId);
            return Language.ViewModInfoPage_NotFound_Title;
        }
    }
}
