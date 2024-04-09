using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls.ModInfo.Loading;
using NexusMods.App.UI.Controls.Trees;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.Diff.ApplyDiff;

public class ApplyDiffViewModel : APageViewModel<IApplyDiffViewModel>, IApplyDiffViewModel
{
    private IServiceProvider _serviceProvider;
    private DiffTreeViewModel? _fileTreeViewModel;
    
    public ApplyDiffViewModel(IWindowManager windowManager, IServiceProvider serviceProvider) : base(windowManager)
    {
        BodyViewModel = new DummyLoadingViewModel();
        _serviceProvider = serviceProvider;
    }

    public LoadoutId LoadoutId { get; private set; }
    
    public void Initialize(LoadoutId loadoutId)
    {
        LoadoutId = loadoutId;
        _fileTreeViewModel = new DiffTreeViewModel(LoadoutId, 
            _serviceProvider.GetRequiredService<IApplyService>(),
            _serviceProvider.GetRequiredService<ILoadoutRegistry>());
        _fileTreeViewModel.Refresh();
        BodyViewModel = _fileTreeViewModel;
    }
    public IFileTreeViewModel? FileTreeViewModel => _fileTreeViewModel;
    [Reactive] public IViewModelInterface BodyViewModel { get; set; }
}
