using System.ComponentModel;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Dialog.Enums;
using ReactiveUI;

namespace NexusMods.App.UI.Dialog;

public class DialogDesignViewModel : IDialogViewModel
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public ViewModelActivator Activator { get; } = null!;

    public R3.ReactiveCommand<ButtonDefinitionId, ButtonDefinitionId> ButtonPressCommand { get; } = new((id, token) =>
        {
            Console.WriteLine(id);
            return ValueTask.FromResult(id);
        }
    );

    public string WindowTitle { get; }
    public DialogWindowSize DialogWindowSize { get; }
    public IViewModelInterface? ContentViewModel { get; }
    public DialogButtonDefinition[] ButtonDefinitions { get; }
    public ButtonDefinitionId Result { get; set; }

    // create a constructor with parameters for design-time purposes instead of object initializers

    public DialogDesignViewModel(string windowTitle, DialogButtonDefinition[] buttonDefinitions, IViewModelInterface contentViewModel)
    {
        WindowTitle = windowTitle;
        DialogWindowSize = DialogWindowSize.Medium;
        ButtonDefinitions = buttonDefinitions;
        ContentViewModel = contentViewModel;
    }

    // create a static instance for a design view
    public static DialogDesignViewModel StandardContent { get; } = new(
        "StandardContent Dialog Content",
        [
            DialogStandardButtons.Ok,
            DialogStandardButtons.Cancel,
        ],
        new DialogStandardContentViewModel(
            "This is a design-time dialog content view model. It is used to demonstrate the dialog's layout and functionality without requiring a full implementation of the content view model."
        )
    );

    // create a static instance for a design view
    public static DialogDesignViewModel CustomContent { get; } = new(
        "CustomContent Dialog Content",
        [
            DialogStandardButtons.Yes,
            DialogStandardButtons.No,
        ],
        new CustomContentExampleViewModel("This is a custom content example view model for design-time purposes.")
    );
}
