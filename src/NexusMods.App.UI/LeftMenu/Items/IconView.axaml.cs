using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Icons;
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
                .Select(v => v.ToMaterialUiName())
                .BindTo(this, view => view.LeftIcon.Value)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.Activate,
                    view => view.ItemButton)
                .DisposeWith(d);
        });
    }
}

