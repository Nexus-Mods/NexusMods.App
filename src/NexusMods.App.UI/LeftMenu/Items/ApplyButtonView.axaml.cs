using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public partial class ApplyButtonView : ReactiveUserControl<IApplyButtonViewModel>
{
    public ApplyButtonView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.BindCommand(ViewModel, vm => vm.ApplyCommand, v => v.ApplyButton)
                .DisposeWith(disposables);
        });
    }
}
