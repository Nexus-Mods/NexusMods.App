using Avalonia.ReactiveUI;
using Humanizer;
using Humanizer.Bytes;
using NexusMods.App.UI.Resources;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays;

public partial class RemoveGameOverlayView : ReactiveUserControl<IRemoveGameOverlayViewModel>
{
    public RemoveGameOverlayView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(view => view.ViewModel)
                .WhereNotNull()
                .Subscribe(viewModel =>
                {
                    TitleText.Text = string.Format(Language.RemoveGameOverlayView_Title, viewModel.GameName);
                    DescriptionText.Text = string.Format(Language.RemoveGameOverlayView_Description, viewModel.GameName);
                    ToggleDescription.Text = string.Format(Language.RemoveGameOverlayView_ToggleDescription, viewModel.NumDownloads.ToString("N0"), viewModel.NumCollections.ToString("N0"), viewModel.GameName);
                    ButtonCancel.Text = Language.RemoveGameOverlayView_CancelButton;
                }).AddTo(disposables);

            this.WhenAnyValue(
                    view => view.ViewModel!.ShouldDeleteDownloads.Value,
                    view => view.ViewModel!.SumDownloadsSize)
                .Subscribe(tuple =>
                {
                    var (shouldDeleteDownloads, _) = tuple;

                    if (shouldDeleteDownloads)
                    {
                        ButtonRemove.Text = Language.RemoveGameOverlayView_RemoveButton_AlsoDelete;
                        // ButtonRemove.Text = string.Format(Language.RemoveGameOverlayView_RemoveButton_AlsoDelete, ByteSize.FromBytes(sumDownloadsSize.Value).Humanize());
                    }
                    else
                    {
                        ButtonRemove.Text = Language.RemoveGameOverlayView_RemoveButton;
                    }
                })
                .AddTo(disposables);

            this.Bind(ViewModel, vm => vm.ShouldDeleteDownloads.Value, view => view.SwitchDeleteDownloads.IsChecked)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.CommandCancel, view => view.ButtonCancel)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.CommandRemove, view => view.ButtonRemove)
                .AddTo(disposables);
        });
    }
}
