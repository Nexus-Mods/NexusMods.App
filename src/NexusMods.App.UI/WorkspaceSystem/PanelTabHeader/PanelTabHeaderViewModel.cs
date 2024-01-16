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

    [Reactive] public bool CanClose { get; set; }

    [Reactive] public bool IsSelected { get; set; } = true;

    public ReactiveCommand<Unit, PanelTabId> CloseTabCommand { get; }

    public PanelTabHeaderViewModel(PanelTabId id)
    {
        Id = id;
        CloseTabCommand = ReactiveCommand.Create(() => Id, this.WhenAnyValue(vm => vm.CanClose));

        this.WhenActivated(disposables =>
        {
            Disposable.Create(this, state =>
            {
                if (state.Icon is IDisposable disposable) disposable.Dispose();
                state.Icon = null;
            }).DisposeWith(disposables);
        });
    }
}
