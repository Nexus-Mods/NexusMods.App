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
using NexusMods.Icons;

namespace NexusMods.App.UI.Dialog;

public partial class MessageBoxView : ReactiveUserControl<MessageBoxViewModel>, IMessageBoxView<ButtonDefinitionId>
{
    private ButtonDefinitionId _buttonResult = ButtonDefinitionId.From("none");
    private Action? _closeAction;

    private Button? _closeButton;
    private UnifiedIcon? _icon;

    public MessageBoxView()
    {
        InitializeComponent();

        _closeButton = this.FindControl<Button>("CloseButton");

        if (_closeButton != null)
            _closeButton.Click += CloseButton_OnClick;

        _icon = this.FindControl<UnifiedIcon>("ContentIcon");

        this.WhenActivated(disposables =>
            {
                CopyDetailsButton.Command = ReactiveCommand.CreateFromTask(async () =>
                {
                    await TopLevel.GetTopLevel(this)!.Clipboard!.SetTextAsync(ViewModel?.MarkdownRenderer?.Contents);
                });
                
                // Bind the CloseWindowCommand to the CloseButton's Command.
                this.OneWayBind(ViewModel,
                        vm => vm.CloseWindowCommand,
                        view => view.CloseButton.Command
                    )
                    .DisposeWith(disposables);
                
                // Bind the title text block to the ViewModel's WindowTitle property.
                this.OneWayBind(ViewModel,
                        vm => vm.WindowTitle,
                        view => view.TitleTextBlock.Text
                    )
                    .DisposeWith(disposables);
                
                this.OneWayBind(ViewModel,
                        vm => vm.Heading,
                        view => view.ContentHeader.Text
                    )
                    .DisposeWith(disposables);
                
                // only show the heading if the heading is not null
                this.WhenAnyValue(view => view.ViewModel!.Heading)
                    .Select(heading => heading is not null)
                    .Subscribe(b => ContentHeader.IsVisible = b)
                    .DisposeWith(disposables);
                
                // Bind the message text block to the ViewModel's ContentMessage property.
                this.OneWayBind(ViewModel,
                        vm => vm.Text,
                        view => view.ContentTextBlock.Text
                    )
                    .DisposeWith(disposables);

                // bind the icon to the icon property
                this.OneWayBind(ViewModel,
                        vm => vm.Icon,
                        view => view.ContentIcon.Value
                    )
                    .DisposeWith(disposables);
                
                // bind the markdown renderer to the markdown renderer view host
                this.OneWayBind(ViewModel, vm => vm.MarkdownRenderer, v => v.MarkdownRendererViewModelViewHost.ViewModel)
                    .DisposeWith(disposables);
            
                // only show the markdown container if markdown is not null
                this.WhenAnyValue(view => view.ViewModel!.MarkdownRenderer)
                    .Select(markdown => markdown is not null)
                    .Subscribe(b => MarkdownContainer.IsVisible = b)
                    .DisposeWith(disposables);

                // bind the content view model
                this.OneWayBind(ViewModel,
                        vm => vm.ContentViewModel,
                        view => view.ViewModelHost.ViewModel
                    )
                    .DisposeWith(disposables);

                // only show the icon if the icon is not null
                this.WhenAnyValue(view => view.ViewModel!.Icon)
                    .Select(icon => icon is not null)
                    .Subscribe(b => ContentIcon.IsVisible = b)
                    .DisposeWith(disposables);

                // only show custom content if the content view model is not null
                // and then hide the generic content
                this.WhenAnyValue(view => view.ViewModel!.ContentViewModel)
                    .Subscribe(customContent =>
                        {
                            var hasCustomContent = customContent is not null;
                            CustomContentContainer.IsVisible = hasCustomContent;
                            GenericContentContainer.IsVisible = !hasCustomContent;
                        }
                    )
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
        if (DataContext is not MessageBoxViewModel viewModel || viewModel.ButtonDefinitions.Length == 0) return;

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

    public void CloseButton_OnClick(object? sender, EventArgs eventArgs)
    {
        _closeAction?.Invoke();
    }

    public ButtonDefinitionId Result { get; }
}
