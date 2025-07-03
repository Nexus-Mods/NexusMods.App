using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Labs.Panels;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.ReactiveUI;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Dialog.Enums;
using R3;
using ReactiveUI;
using SerialDisposable = System.Reactive.Disposables.SerialDisposable;

namespace NexusMods.App.UI.Dialog;

public partial class DialogWindow : ReactiveWindow<IDialogViewModel>, IDisposable
{
    // this is what is returned when the window close button is clicked
    private readonly ButtonDefinitionId _closeButtonResult = ButtonDefinitionId.CloseWindow;
    
    private SerialDisposable _serialDisposable;
        
    public DialogWindow()
    {
        ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = 33;

        InitializeComponent();
        
        this.WhenActivated(disposables =>
            {
                CloseButton.CommandParameter = _closeButtonResult;
        
                // Bind the CloseWindowCommand to the CloseButton's Command.
                this.OneWayBind(ViewModel,
                        vm => vm.ButtonPressCommand,
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
                        vm => vm.ContentViewModel,
                        view => view.ContentViewModelHost.ViewModel
                    )
                    .DisposeWith(disposables);
                
                GenerateButtons();
            }
        );
        
        _serialDisposable = new SerialDisposable();

        // Bind the CloseCommand to the Window's close action
        this.DataContextChanged += (sender, args) =>
        {
            // Bind the CloseCommand to the Window's close action
            _serialDisposable.Disposable = ViewModel!.ButtonPressCommand.Subscribe( id => 
            {
                Close();
            });
        };
    }
    
    private void GenerateButtons()
    {
        if (ViewModel is null) return;
        
        // Find the ButtonsFlexPanel container
        var buttonsFlexPanel = this.FindControl<FlexPanel>("ButtonsFlexPanel");
        if (buttonsFlexPanel == null) return;

        // Clear existing buttons and make invisible in case nothing gets added
        buttonsFlexPanel.IsVisible = false;
        buttonsFlexPanel.Children.Clear();

        // Access the ButtonDefinitions from the DataContext (assumes it's bound to MessageBoxViewModel)
        if (ViewModel.ButtonDefinitions.Length == 0) return;

        // Loop through each button definition and create a StandardButton
        foreach (var buttonDefinition in ViewModel.ButtonDefinitions)
        {
            // Create a new StandardButton
            var button = new StandardButton
            {
                Name = $"{buttonDefinition.Id}-button",
                Text = buttonDefinition.Text,
                Command = ViewModel.ButtonPressCommand,
                CommandParameter = buttonDefinition.Id,
                IsDefault = buttonDefinition.ButtonAction == ButtonAction.Accept,
                IsCancel = buttonDefinition.ButtonAction == ButtonAction.Reject,
                [Flex.GrowProperty] = 1,
                [Flex.ShrinkProperty] = 0,
                [Flex.BasisProperty] = FlexBasis.Parse("0%"),
                LeftIcon = buttonDefinition.LeftIcon,
                RightIcon = buttonDefinition.RightIcon,
                ShowIcon = buttonDefinition switch
                {
                    { LeftIcon: not null, RightIcon: null } => StandardButton.ShowIconOptions.Left,
                    { LeftIcon: null, RightIcon: not null } => StandardButton.ShowIconOptions.Right,
                    { LeftIcon: not null, RightIcon: not null } => StandardButton.ShowIconOptions.Both,
                    _ => StandardButton.ShowIconOptions.None
                },
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
    
    public void Dispose()
    {
        _serialDisposable.Dispose();
    }
}
