using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModName;

public partial class ModNameView : ReactiveUserControl<IModNameViewModel>
{
    public ModNameView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.ViewModel!.Value)
                .BindTo<string, ModNameView, string>(this, view => view.NameTextBox.Text)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.DataContext)
                .SubscribeWithErrorLogging(logger: default)
                .DisposeWith(d);
        });
    }
}

