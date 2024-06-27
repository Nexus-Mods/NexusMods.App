using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.App.UI;
using NexusMods.Games.FOMOD.UI.Resources;
using ReactiveUI;

namespace NexusMods.Games.FOMOD.UI;

[UsedImplicitly]
public partial class FooterStepperView : ReactiveUserControl<IFooterStepperViewModel>
{
    public FooterStepperView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Progress, view => view.ProgressBar.Value, progress => progress.Value)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.GoToPrevCommand, view => view.GoToPrevButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.GoToNextCommand, view => view.GoToNextButton)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.IsLastStep)
                .SubscribeWithErrorLogging(isLastStep =>
                {
                    GoToNextButtonTextBlock.Text = isLastStep ? Language.FooterStepperView_FooterStepperView_Finish : Language.FooterStepperView_FooterStepperView_Next;
                    IconArrowRight.IsVisible = !isLastStep;
                })
                .DisposeWith(disposables);
        });
    }
}

