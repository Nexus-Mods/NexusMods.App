using System.ComponentModel;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.UI.Sdk.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.Dialog;

public class MessageDialogViewModel : IDialogViewModel<ButtonDefinitionId>
{
    public R3.ReactiveCommand<ButtonDefinitionId, ButtonDefinitionId> ButtonPressCommand { get; }
    public DialogButtonDefinition[] ButtonDefinitions => _baseModel.ButtonDefinitions;
    public string WindowTitle { get; }
    public double WindowWidth { get; }
    public ButtonDefinitionId Result { get; set; }
    
    private readonly IDialogBaseModel _baseModel;
    public string Title => _baseModel.Title;
    public string? Text => _baseModel.Text;
    public string? Heading => _baseModel.Heading;
    public IconValue? Icon => _baseModel.Icon;
    public DialogWindowSize DialogWindowSize => _baseModel.DialogWindowSize;
    public IMarkdownRendererViewModel? MarkdownRenderer => _baseModel.MarkdownRenderer;
    public IViewModelInterface? ContentViewModel => _baseModel.ContentViewModel;
    
    
    public event PropertyChangedEventHandler? PropertyChanged;
    public ViewModelActivator Activator { get; } = null!;

    public MessageDialogViewModel(IDialogBaseModel baseModel)
    {
        _baseModel = baseModel;

        WindowTitle = _baseModel.Title;
        
        WindowWidth = _baseModel.DialogWindowSize switch
        {
            DialogWindowSize.Small => 320,
            DialogWindowSize.Medium => 480,
            DialogWindowSize.Large => 640,
            _ => 320,
        };
        
        ButtonPressCommand = new R3.ReactiveCommand<ButtonDefinitionId, ButtonDefinitionId>((id) =>
            {
                Console.WriteLine(id);
                Result = id;
                return id;
            }
        );
    }
}
