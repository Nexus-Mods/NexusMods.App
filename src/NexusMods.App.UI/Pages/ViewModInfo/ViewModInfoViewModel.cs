using System.Reactive.Disposables;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.ModInfo.ViewModFiles;
using NexusMods.App.UI.Pages.ViewModInfo.Types;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.ViewModInfo;

public class ViewModInfoViewModel : APageViewModel<IViewModInfoViewModel>, IViewModInfoViewModel
{
    [Reactive]
    public LoadoutId LoadoutId { get; set; }
    
    [Reactive]
    public ModId ModId { get; set; }
    
    [Reactive]
    public CurrentViewModInfoPage Page { get; set; }

    [Reactive]
    public IViewModelInterface PageViewModel { get; set; } = default!;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILoadoutRegistry _registry;

    public ViewModInfoViewModel(IWindowManager windowManager, IServiceProvider serviceProvider, ILoadoutRegistry registry) : base(windowManager)
    {
        _serviceProvider = serviceProvider;
        _registry = registry;
        this.WhenActivated(delegate(CompositeDisposable dp)
        {
            GetWorkspaceController().SetTabTitle(GetModName(), WorkspaceId, PanelId, TabId);

            this.WhenAnyValue(x => x.Page)
                .Subscribe(CreateNewPage)
                .DisposeWith(dp);
        });
    }

    private void CreateNewPage(CurrentViewModInfoPage page)
    {
        switch (page)
        {
            case CurrentViewModInfoPage.Files:
                var vm = _serviceProvider.GetRequiredService<IViewModFilesViewModel>();
                vm.Initialize(LoadoutId, [ModId]);
                PageViewModel = vm;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(page), page, null);
        }
    }

    private string GetModName() => _registry.Get(LoadoutId, ModId)!.Name;
}
