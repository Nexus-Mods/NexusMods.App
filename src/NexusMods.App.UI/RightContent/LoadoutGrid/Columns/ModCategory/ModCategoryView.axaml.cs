using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModCategory;

public partial class ModCategoryView : ReactiveUserControl<IModCategoryViewModel>
{
    public ModCategoryView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(view => view.ViewModel!.Category)
                .BindToUi<string, ModCategoryView, string>(this, view => view.CategoryTextBlock.Text)
                .DisposeWith(d);
        });
    }
}

