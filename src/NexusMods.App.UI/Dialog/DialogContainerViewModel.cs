using System.ComponentModel;
using NexusMods.App.UI.Dialog.Enums;
using ReactiveUI;

namespace NexusMods.App.UI.Dialog;

public class DialogContainerViewModel : IDialogViewModel<ButtonDefinitionId>
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public ViewModelActivator Activator { get; } = null!;

    public IDialogView<ButtonDefinitionId>? View { get; set; }
    
    public ButtonDefinitionId Return { get; set; }
    
    public void SetView(IDialogView<ButtonDefinitionId> view)
    {
        View = view;
    }

    public string WindowTitle { get; }
    public double WindowMaxWidth { get; }
    public bool ShowWindowTitlebar { get; }

    public IDialogContentViewModel ContentViewModel { get; set; }

    public DialogContainerViewModel(IDialogContentViewModel contentViewModel, string windowTitle, double windowMaxWidth, bool showWindowTitlebar = false)
    {
        ContentViewModel = contentViewModel;
        WindowTitle = windowTitle;
        WindowMaxWidth = windowMaxWidth;
        ShowWindowTitlebar = showWindowTitlebar;
    }
}
