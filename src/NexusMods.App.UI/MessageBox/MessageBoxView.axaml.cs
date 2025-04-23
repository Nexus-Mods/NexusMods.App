using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Labs.Panels;
using Avalonia.Markup.Xaml;
using DynamicData;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.MessageBox.Enums;

namespace NexusMods.App.UI.MessageBox;

public partial class MessageBoxView : UserControl, IMessageBoxView<ButtonDefinitionId>
{
    private ButtonDefinitionId _buttonResult = ButtonDefinitionId.From("none");
    private Action? _closeAction;
    
    private Button? closeButton;
    
    public MessageBoxView()
    {
        InitializeComponent();
        
        closeButton = this.FindControl<Button>("CloseButton");
        
        if(closeButton != null)
            closeButton.Click += CloseWindow;
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
                Command = viewModel.ButtonClickCommand,
                CommandParameter = buttonDefinition.Id,
                IsDefault = buttonDefinition.ButtonRole.HasFlag(ButtonRole.AcceptRole),
                IsCancel = buttonDefinition.ButtonRole.HasFlag(ButtonRole.RejectRole),
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
            if (buttonDefinition.ButtonRole.HasFlag(ButtonRole.DestructiveRole))
                button.Classes.Add("Danger");
            
            if (buttonDefinition.ButtonRole.HasFlag(ButtonRole.InfoRole))
                button.Classes.Add("Info");
            
            if (buttonDefinition.ButtonRole.HasFlag(ButtonRole.PremiumRole))
                button.Classes.Add("Premium");
            
            if (buttonDefinition.ButtonRole.HasFlag(ButtonRole.PrimaryRole))
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

