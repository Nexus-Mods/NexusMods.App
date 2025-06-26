using System.ComponentModel;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Dialog.Enums;
using ReactiveUI;

namespace NexusMods.App.UI.Dialog;

public class DialogViewModel: IDialogViewModel
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public ViewModelActivator Activator { get; }
    public R3.ReactiveCommand<ButtonDefinitionId, ButtonDefinitionId> ButtonPressCommand { get; }
    public string WindowTitle { get; }
    public DialogWindowSize DialogWindowSize { get; }
    public IViewModelInterface? ContentViewModel { get; }
    public DialogButtonDefinition[] ButtonDefinitions { get; }
    public ButtonDefinitionId Result { get; set; }

    public DialogViewModel(string title, DialogButtonDefinition[] buttonsDefinitions, IViewModelInterface contentViewModel, DialogWindowSize dialogWindowSize)
    {
        Activator = new ViewModelActivator();
        WindowTitle = title;
        DialogWindowSize = dialogWindowSize;
        ContentViewModel = contentViewModel;
        ButtonDefinitions = buttonsDefinitions;
        
        ButtonPressCommand = new R3.ReactiveCommand<ButtonDefinitionId, ButtonDefinitionId>(id =>
            {
                Console.WriteLine(id);
                Result = id;
                return id;
            }
        );
    }
    
}
