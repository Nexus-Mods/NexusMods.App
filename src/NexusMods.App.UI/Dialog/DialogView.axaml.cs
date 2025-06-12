using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Labs.Panels;
using Avalonia.Markup.Xaml;
using DynamicData;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Dialog.Enums;
using ReactiveUI;
using Avalonia.ReactiveUI;

namespace NexusMods.App.UI.Dialog;

public partial class DialogView : ReactiveUserControl<DialogViewModel>, IDialogView<ButtonDefinitionId>
{
    // this is what is returned when the window close button is clicked
    private readonly ButtonDefinitionId _closeButtonResult = ButtonDefinitionId.From("DefaultClose");

    public DialogView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
            {
                // COMMANDS

                CopyDetailsButton.Command = ReactiveCommand.CreateFromTask(async () =>
                {
                    await TopLevel.GetTopLevel(this)!.Clipboard!.SetTextAsync(ViewModel?.MarkdownRenderer?.Contents);
                });

                CloseButton.CommandParameter = _closeButtonResult;
                
                // Bind the CloseWindowCommand to the CloseButton's Command.
                this.OneWayBind(ViewModel,
                        vm => vm.CloseWindowCommand,
                        view => view.CloseButton.Command
                    )
                    .DisposeWith(disposables);

                // BINDINGS

                this.OneWayBind(ViewModel,
                        vm => vm.WindowTitle,
                        view => view.TitleTextBlock.Text
                    )
                    .DisposeWith(disposables);

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
                        vm => vm.ContentViewModel,
                        view => view.ContentViewModelHost.ViewModel
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
                
                // only show the custom content container if custom content is not null
                this.WhenAnyValue(view => view.ViewModel!.ContentViewModel)
                    .Select(custom => custom is not null)
                    .Subscribe(b => CustomContentContainer.IsVisible = b)
                    .DisposeWith(disposables);
            }
        );
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // need this here so DataContext is set
        GenerateButtons();
    }

    private void GenerateButtons()
    {
        // Find the ButtonsFlexPanel container
        var buttonsFlexPanel = this.FindControl<FlexPanel>("ButtonsFlexPanel");
        if (buttonsFlexPanel == null) return;

        // Clear existing buttons and make invisible in case nothing gets added
        buttonsFlexPanel.IsVisible = false;
        buttonsFlexPanel.Children.Clear();

        // Access the ButtonDefinitions from the DataContext (assumes it's bound to MessageBoxViewModel)
        if (DataContext is not DialogViewModel viewModel || viewModel.ButtonDefinitions.Length == 0) return;

        // Loop through each button definition and create a StandardButton
        foreach (var buttonDefinition in viewModel.ButtonDefinitions)
        {
            // Create a new StandardButton
            var button = new StandardButton
            {
                Name = $"{buttonDefinition.Id}Button",
                Text = buttonDefinition.Text,
                Command = viewModel.CloseWindowCommand,
                CommandParameter = buttonDefinition.Id,
                IsDefault = buttonDefinition.ButtonAction == ButtonAction.Accept,
                IsCancel = buttonDefinition.ButtonAction == ButtonAction.Reject,
                [Flex.GrowProperty] = 1,
                [Flex.ShrinkProperty] = 0,
                [Flex.BasisProperty] = FlexBasis.Parse("0%"),
            };

            // change showicon property based on what properties are set
            button.ShowIcon = buttonDefinition switch
            {
                { LeftIcon: not null, RightIcon: null } => StandardButton.ShowIconOptions.Left,
                { LeftIcon: null, RightIcon: not null } => StandardButton.ShowIconOptions.Right,
                { LeftIcon: not null, RightIcon: not null } => StandardButton.ShowIconOptions.Both,
                _ => StandardButton.ShowIconOptions.None
            };

            switch (buttonDefinition.ButtonStyling)
            {
                // Add appropriate classes based on the ButtonRole
                case ButtonStyling.Destructive:
                    button.Classes.Add("Danger");
                    break;
                case ButtonStyling.Info:
                    button.Classes.Add("Info");
                    break;
                case ButtonStyling.Premium:
                    button.Classes.Add("Premium");
                    break;
                case ButtonStyling.Primary:
                    button.Classes.Add("Primary");
                    break;
                case ButtonStyling.Default:
                    button.Classes.Add("Default");
                    break;
                case ButtonStyling.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Add the button to the panel
            buttonsFlexPanel.Children.Add(button);
        }

        buttonsFlexPanel.IsVisible = true;
    }
}
