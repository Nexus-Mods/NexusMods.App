using System.Reactive;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.Trees;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.UI.Sdk.Icons;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.Diff.ApplyDiff;

public class ApplyDiffViewModel : APageViewModel<IApplyDiffViewModel>, IApplyDiffViewModel
{
    private IServiceProvider _serviceProvider;
    private DiffTreeViewModel? _fileTreeViewModel;
    private LoadoutId _loadoutId;

    [Reactive] public IViewModelInterface BodyViewModel { get; set; } = null!;

    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }


    public ApplyDiffViewModel(IWindowManager windowManager, IServiceProvider serviceProvider) : base(windowManager)
    {
        TabTitle = Language.ApplyDiffViewModel_PageTitle;
        TabIcon = IconValues.ListFilled;

        _serviceProvider = serviceProvider;

        RefreshCommand = ReactiveCommand.Create( () =>
        {
            if (_fileTreeViewModel is null)
            {
                return;
            }

            Refresh(_fileTreeViewModel);
        });
    }


    public void Initialize(LoadoutId loadoutId)
    {
        _loadoutId = loadoutId;
        _fileTreeViewModel = new DiffTreeViewModel(_loadoutId,
            _serviceProvider.GetRequiredService<ISynchronizerService>(),
            _serviceProvider.GetRequiredService<IConnection>()
        );

        Refresh(_fileTreeViewModel);
    }
    
    private void Refresh(DiffTreeViewModel diffTreeViewModel)
    {
        diffTreeViewModel.Refresh();

        Dispatcher.UIThread.Invoke(() =>
        {
            BodyViewModel = diffTreeViewModel;
        });
    }
}
