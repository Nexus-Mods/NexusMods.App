using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI;

[ExcludeFromCodeCoverage]
public partial class BodyView : ReactiveUserControl<IBodyViewModel>
{
    public BodyView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            // Set the mod name if VM is not null.
            this.WhenAnyValue(view => view.ViewModel)
                .WhereNotNull()
                .Do(vm => ModNameTextBlock.Text = vm.ModName)
                .Subscribe()
                .DisposeWith(disposables);

            // Bind the mod content view model.
            this.OneWayBind(ViewModel, vm => vm.ModContentViewModel,
                    view => view.ModContentSectionViewHost.ViewModel)
                .DisposeWith(disposables);

            // Bind the right content view model.
            this.OneWayBind(ViewModel, vm => vm.CurrentRightContentViewModel,
                    view => view.PreviewSectionViewHost.ViewModel)
                .DisposeWith(disposables);
        });
    }
}
