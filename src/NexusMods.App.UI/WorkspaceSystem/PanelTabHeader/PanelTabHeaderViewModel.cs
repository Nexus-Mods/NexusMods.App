using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using NexusMods.App.UI.Resources;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public class PanelTabHeaderViewModel : AViewModel<IPanelTabHeaderViewModel>, IPanelTabHeaderViewModel
{
    public PanelTabId Id { get; }

    [Reactive]
    public string Title { get; set; } = Language.PanelTabHeaderViewModel_Title_New_Tab;

    [Reactive]
    public IImage? Icon { get; set; }

    [Reactive]
    public bool IsSelected { get; set; }

    public ReactiveCommand<Unit, Unit> CloseTabCommand { get; }

    public PanelTabHeaderViewModel(IPanelViewModel panelViewModel, PanelTabId id)
    {
        Id = id;

        CloseTabCommand = ReactiveCommand.Create(() =>
        {
            panelViewModel.CloseTab(id);
        });

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(vm => vm.IsSelected)
                .Where(isSelected => isSelected)
                .SubscribeWithErrorLogging(_ => panelViewModel.SelectedTabId = Id)
                .DisposeWith(disposables);

            Disposable.Create(this, state =>
            {
                if (state.Icon is IDisposable disposable) disposable.Dispose();
                state.Icon = null;
            }).DisposeWith(disposables);
        });
    }
}
