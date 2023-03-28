using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public partial class ModNameView : ReactiveUserControl<IModNameViewModel>
{
    public ModNameView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.ViewModel!.Name)
                .BindToUi(this, view => view.NameTextBox.Text)
                .DisposeWith(d);
        });
    }
}

