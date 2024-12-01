using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using R3;
using ReactiveUI;
using ReactiveCommand = ReactiveUI.ReactiveCommand;

namespace NexusMods.App.UI.Overlays.Generic.MessageBox.Ok;

public partial class MessageBoxOkView : ReactiveUserControl<IMessageBoxOkViewModel>
{
    public MessageBoxOkView()
    {
        InitializeComponent();

        // Bind the View's properties to the UI elements
        this.WhenActivated(disposables =>
        {
            // One-way binding from ViewModel to UI
            this.OneWayBind(ViewModel, vm => vm.Title, v => v.HeadingText.Text)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.Description, v => v.MessageTextBlock.Text)
                .DisposeWith(disposables);

            // Bind commands
            OkButton.Command = ReactiveCommand.Create(() =>
            {
                ViewModel!.Complete(Unit.Default);
            });

            CloseButton.Command = ReactiveCommand.Create(() =>
            {
                ViewModel!.Complete(Unit.Default);
            });
        });
    }
}

