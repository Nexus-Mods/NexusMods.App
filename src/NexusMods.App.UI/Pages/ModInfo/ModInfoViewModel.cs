using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.ModInfo.ViewModFiles;
using NexusMods.App.UI.Pages.ModInfo.Types;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
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
    public CurrentModInfoPage Page { get; set; }

    [Reactive]
    public IViewModelInterface PageViewModel { get; set; } = default!;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILoadoutRegistry _registry;

    public ModInfoViewModel(IWindowManager windowManager, IServiceProvider serviceProvider, ILoadoutRegistry registry) : base(windowManager)
    {
        _serviceProvider = serviceProvider;
        _registry = registry;
        this.WhenActivated(delegate(CompositeDisposable dp)
        {
            GetWorkspaceController().SetTabTitle(GetModName(), WorkspaceId, PanelId, TabId);

            this.WhenAnyValue(x => x.Page)
                .OffUi()
                .Select(CreateNewPage)
                .OnUI()
                .Subscribe(x => PageViewModel = x)
                .DisposeWith(dp);
        });
    }

    private IViewModelInterface CreateNewPage(CurrentModInfoPage page)
    {
        switch (page)
        {
            case CurrentModInfoPage.Files:
                var vm = _serviceProvider.GetRequiredService<IViewModFilesViewModel>();
                vm.Initialize(LoadoutId, [ModId]);
                return vm;
            default:
                throw new ArgumentOutOfRangeException(nameof(page), page, null);
        }
    }

    private string GetModName() => _registry.Get(LoadoutId, ModId)!.Name;
}
