using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.LoadoutGrid;

public partial class LoadoutGridView : ReactiveUserControl<ILoadoutGridViewModel>
{
    public LoadoutGridView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(view => view.ViewModel!.Mods)
                .BindToUi(this, view => view.ModsDataGrid.Items)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Columns)
                .OnUI()
                .Subscribe(columns =>
                {
                    ModsDataGrid.Columns.Clear();
                    foreach (var column in columns)
                        ModsDataGrid.Columns.Add(column.Generate());
                })
                .DisposeWith(d);
        });
    }
}

