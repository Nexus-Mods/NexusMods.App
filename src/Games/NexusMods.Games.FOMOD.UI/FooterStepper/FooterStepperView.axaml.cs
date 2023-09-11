using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.FOMOD.UI;

public partial class FooterStepperView : ReactiveUserControl<IFooterStepperViewModel>
{
    public FooterStepperView()
    {
        InitializeComponent();

        this.WhenActivated(disposable =>
        {
            this.OneWayBind(ViewModel, vm => vm.Progress, view => view.ProgressTextBlock.Text, progress => progress.ToString())
                .DisposeWith(disposable);

            this.BindCommand(ViewModel, vm => vm.GoToPrevCommand, view => view.GoToPrevButton)
                .DisposeWith(disposable);

            this.BindCommand(ViewModel, vm => vm.GoToNextCommand, view => view.GoToNextButton)
                .DisposeWith(disposable);
        });
    }
}

