using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using NexusMods.App.UI;
using NexusMods.Games.FOMOD.UI.Resources;
using ReactiveUI;

namespace NexusMods.Games.FOMOD.UI;

public partial class FooterStepperView : ReactiveUserControl<IFooterStepperViewModel>
{
    public FooterStepperView()
    {
        InitializeComponent();

        this.WhenActivated(disposable =>
        {
            this.OneWayBind(ViewModel, vm => vm.Progress, view => view.ProgressBar.Value, progress => progress.Value)
                .DisposeWith(disposable);

            this.BindCommand(ViewModel, vm => vm.GoToPrevCommand, view => view.GoToPrevButton)
                .DisposeWith(disposable);

            this.BindCommand(ViewModel, vm => vm.GoToNextCommand, view => view.GoToNextButton)
                .DisposeWith(disposable);

            this.WhenAnyValue(x => x.ViewModel!.IsLastStep)
                .SubscribeWithErrorLogging(logger: default, isFinalStep =>
                {
                    GoToNextButtonTextBlock.Text = isFinalStep ? Language.FooterStepperView_FooterStepperView_Finish : Language.FooterStepperView_FooterStepperView_Next;
                    IconArrowRight.IsVisible = !isFinalStep;
                })
                .DisposeWith(disposable);
        });
    }
}

