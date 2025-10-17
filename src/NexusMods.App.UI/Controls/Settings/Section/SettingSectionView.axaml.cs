using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Settings.Section;

[UsedImplicitly]
public partial class SettingSectionView : ReactiveUserControl<ISettingSectionViewModel>
{
    public SettingSectionView()
    {
        InitializeComponent();

        this.WhenActivated(disposable =>
        {
            this.WhenAnyValue(view => view.ViewModel)
                .WhereNotNull()
                .Subscribe(PopulateFromViewModel)
                .DisposeWith(disposable);
        });
    }

    private void PopulateFromViewModel(ISettingSectionViewModel viewModel)
    {
        Icon.Value = viewModel.Descriptor.IconFunc();
        Text.Text = viewModel.Descriptor.Name;
    }
}

