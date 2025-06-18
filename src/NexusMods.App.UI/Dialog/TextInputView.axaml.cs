using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using NexusMods.Abstractions.UI;
using ReactiveUI;

namespace NexusMods.App.UI.Dialog;

public partial class TextInputView : ReactiveUserControl<ITextInputViewModel>
{
    public TextInputView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
            {
                this.OneWayBind(ViewModel,
                        vm => vm.InputLabel,
                        view => view.InputLabel.Text
                    )
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                        vm => vm.InputWatermark,
                        view => view.InputTextBox.Watermark
                    )
                    .DisposeWith(disposables);

                this.Bind(ViewModel,
                        vm => vm.InputText,
                        view => view.InputTextBox.Text
                    )
                    .DisposeWith(disposables);
                
                this.BindCommand(ViewModel,
                        vm => vm.ClearInputCommand,
                        view => view.ButtonInputClear
                    )
                    .DisposeWith(disposables);
            }
        );
    }
}
