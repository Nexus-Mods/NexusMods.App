using System.Reactive.Disposables;
using Avalonia.Media.Immutable;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.Controls;

[UsedImplicitly]
public partial class DummyView : ReactiveUserControl<IDummyViewModel>
{
    public DummyView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Color, view => view.Background, color => new ImmutableSolidColorBrush(color))
                .DisposeWith(disposables);
        });
    }
}

