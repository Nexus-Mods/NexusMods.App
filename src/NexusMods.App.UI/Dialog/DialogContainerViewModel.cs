using System.ComponentModel;
using NexusMods.App.UI.Dialog.Enums;
using ReactiveUI;

namespace NexusMods.App.UI.Dialog;

public class DialogContainerViewModel : IDialogViewModel<ButtonDefinitionId>
{
    private IDialogView<ButtonDefinitionId>? _view;
    
    public event PropertyChangedEventHandler? PropertyChanged;
    public ViewModelActivator Activator { get; } = null!;

    public ButtonDefinitionId Return { get; set; }
    
    public void SetView(IDialogView<ButtonDefinitionId> view)
    {
        _view = view;
    }

    public string WindowTitle { get; } = "Dialog Container";
    public double WindowMaxWidth { get; } = 500;
}
