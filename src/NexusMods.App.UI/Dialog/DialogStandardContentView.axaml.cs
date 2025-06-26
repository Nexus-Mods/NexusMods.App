using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Labs.Panels;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Dialog.Enums;
using ReactiveUI;
using Avalonia.ReactiveUI;
using NexusMods.Abstractions.UI;

namespace NexusMods.App.UI.Dialog;

public partial class DialogStandardContentView : ReactiveUserControl<IDialogStandardContentViewModel>
{
    public DialogStandardContentView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
            {
                // COMMANDS

                CopyDetailsButton.Command = ReactiveCommand.CreateFromTask(async () => { await TopLevel.GetTopLevel(this)!.Clipboard!.SetTextAsync(ViewModel?.MarkdownRenderer?.Contents); });

                // BINDINGS

                this.OneWayBind(ViewModel,
                        vm => vm.Heading,
                        view => view.HeaderTextBlock.Text
                    )
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                        vm => vm.Text,
                        view => view.TextTextBlock.Text
                    )
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                        vm => vm.Icon,
                        view => view.Icon.Value
                    )
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                        vm => vm.MarkdownRenderer,
                        v => v.MarkdownRendererViewModelViewHost.ViewModel
                    )
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                        vm => vm.BottomText,
                        view => view.BottomTextTextBlock.Text
                    )
                    .DisposeWith(disposables);


                this.Bind(ViewModel,
                    vm => vm.InputText,
                    view => view.InputTextBox.Text
                );

                this.OneWayBind(ViewModel,
                    vm => vm.InputLabel,
                    view => view.InputLabel.Text
                );

                this.OneWayBind(ViewModel,
                    vm => vm.InputWatermark,
                    view => view.InputTextBox.Watermark
                );

                this.BindCommand(ViewModel,
                        vm => vm.ClearInputCommand,
                        view => view.ButtonInputClear
                    )
                    .DisposeWith(disposables);

                // HIDE CONTROLS IF NOT NEEDED

                // only show the text if not null or empty
                this.WhenAnyValue(view => view.ViewModel!.Text)
                    .Select(string.IsNullOrWhiteSpace)
                    .Subscribe(b => TextTextBlock.IsVisible = !b)
                    .DisposeWith(disposables);

                // only show the heading if not null or empty
                this.WhenAnyValue(view => view.ViewModel!.Heading)
                    .Select(string.IsNullOrWhiteSpace)
                    .Subscribe(b => HeaderTextBlock.IsVisible = !b)
                    .DisposeWith(disposables);

                // only show the icon if the icon is not null
                this.WhenAnyValue(view => view.ViewModel!.Icon)
                    .Select(icon => icon is not null)
                    .Subscribe(b => Icon.IsVisible = b)
                    .DisposeWith(disposables);

                // only show the markdown container if markdown is not null
                this.WhenAnyValue(view => view.ViewModel!.MarkdownRenderer)
                    .Select(markdown => markdown is not null)
                    .Subscribe(b => MarkdownContainer.IsVisible = b)
                    .DisposeWith(disposables);

                // only show the input stack if not null or empty
                this.WhenAnyValue(view => view.ViewModel!.InputLabel)
                    .Select(string.IsNullOrWhiteSpace)
                    .Subscribe(b => InputStack.IsVisible = !b)
                    .DisposeWith(disposables);

                // only show the bottom text if not null or empty
                this.WhenAnyValue(view => view.ViewModel!.BottomText)
                    .Select(string.IsNullOrWhiteSpace)
                    .Subscribe(b => BottomTextTextBlock.IsVisible = !b)
                    .DisposeWith(disposables);
            }
        );
    }
}
