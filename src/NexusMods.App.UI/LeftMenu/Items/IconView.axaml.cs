using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public partial class IconView : ReactiveUserControl<IIconViewModel>
{
    public IconView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(view => view.ViewModel!.Name)
                .BindTo(this, view => view.NameText.Text)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Icon)
                .BindTo(this, view => view.LeftIcon.Value)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.NavigateCommand, view => view.ItemButton)
                .DisposeWith(d);
        });
    }
}

