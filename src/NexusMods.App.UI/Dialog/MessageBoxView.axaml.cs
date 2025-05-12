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

public partial class MessageBoxView : ReactiveUserControl<MessageBoxViewModel>, IMessageBoxView<ButtonDefinitionId>
{
    private ButtonDefinitionId _buttonResult = ButtonDefinitionId.From("none");
    private Action? _closeAction;

    private Button? closeButton;

    public MessageBoxView()
    {
        InitializeComponent();

        closeButton = this.FindControl<Button>("CloseButton");

        if (closeButton != null)
            closeButton.Click += CloseWindow;

        this.WhenActivated(disposables =>
            {
                this.OneWayBind(ViewModel,
                        vm => vm.ShowWindowTitlebar,
                        view => view.Titlebar.IsVisible
                    )
                    .DisposeWith(disposables);

                // Bind the title text block to the ViewModel's WindowTitle property.
                this.OneWayBind(ViewModel,
                        vm => vm.WindowTitle,
                        view => view.TitleTextBlock.Text
                    )
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                        vm => vm.ContentMessage,
                        view => view.ContentTextBlock.Text
                    )
                    .DisposeWith(disposables);

                // Bind the content view model
                this.OneWayBind(ViewModel,
                        vm => vm.ContentViewModel,
                        view => view.ViewModelHost.ViewModel
                    )
                    .DisposeWith(disposables);
                
                this.WhenAnyValue(view => view.ViewModel!.ContentViewModel)
                    .Select(vm => vm != null)
                    .BindTo(this, v => v.ViewModelHost.IsVisible)
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

        // Clear existing buttons
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

            // Add appropriate classes based on the ButtonRole
            if (buttonDefinition.ButtonStyling == ButtonStyling.Destructive)
                button.Classes.Add("Danger");

            if (buttonDefinition.ButtonStyling == ButtonStyling.Info)
                button.Classes.Add("Info");

            if (buttonDefinition.ButtonStyling == ButtonStyling.Premium)
                button.Classes.Add("Premium");

            if (buttonDefinition.ButtonStyling == ButtonStyling.Primary)
                button.Classes.Add("Primary");

            // Add the button to the panel
            buttonsFlexPanel.Children.Add(button);
        }
    }

    public void CloseWindow(object? sender, EventArgs eventArgs)
    {
        this.Close();
    }

    public void SetCloseAction(Action closeAction)
    {
        _closeAction = closeAction;
    }

    public void SetButtonResult(ButtonDefinitionId buttonResult)
    {
        _buttonResult = buttonResult;
    }

    public ButtonDefinitionId GetButtonResult()
    {
        return _buttonResult;
    }

    public void Close()
    {
        _closeAction?.Invoke();
    }
}
