using System.ComponentModel;
using System.Runtime.CompilerServices;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.UI.Sdk.Icons;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Dialog;

public class InputDialogViewModel : AViewModel<IDialogViewModel<InputDialogResult>>, IDialogViewModel<InputDialogResult>
{
    public R3.ReactiveCommand<ButtonDefinitionId, ButtonDefinitionId> ButtonPressCommand { get; }
    public DialogButtonDefinition[] ButtonDefinitions => _baseModel.ButtonDefinitions;
    public string WindowTitle { get; }
    public double WindowWidth { get; }
    public InputDialogResult Result { get; set; }

    private readonly IDialogBaseModel _baseModel;
    
    public string Title => _baseModel.Title;
    public string? Text => _baseModel.Text;
    public string? Heading => _baseModel.Heading;
    public IconValue? Icon => _baseModel.Icon;
    public DialogWindowSize DialogWindowSize => _baseModel.DialogWindowSize;
    public IMarkdownRendererViewModel? MarkdownRenderer => _baseModel.MarkdownRenderer;
    public IViewModelInterface? ContentViewModel => _baseModel.ContentViewModel;
    
    [Reactive] public string InputText { get; set; }
    public string InputLabel { get; set; }
    public string InputWatermark { get; set; }

    public R3.ReactiveCommand ClearInputCommand { get; }


    public InputDialogViewModel(
        IDialogBaseModel dialogBaseModel,
        string? inputText = null,
        string? inputLabel = null,
        string? inputWatermark = null)
    {
        _baseModel = dialogBaseModel;
        
        WindowTitle = _baseModel.Title;
        WindowWidth = _baseModel.DialogWindowSize switch
        {
            DialogWindowSize.Small => 320,
            DialogWindowSize.Medium => 480,
            DialogWindowSize.Large => 640,
            _ => 320,
        };
        
        InputLabel = inputLabel ?? string.Empty;
        InputWatermark = inputWatermark ?? string.Empty;
        InputText = inputText ?? string.Empty;
        
        ClearInputCommand = new R3.ReactiveCommand(
            executeAsync: (_, cancellationToken) =>
            {
                InputText = string.Empty;
                return ValueTask.CompletedTask;
            }
        );

        ButtonPressCommand = new R3.ReactiveCommand<ButtonDefinitionId, ButtonDefinitionId>(id =>
            {
                Console.WriteLine(id);
                Result = new InputDialogResult
                {
                    ButtonId = id,
                    InputText = InputText,
                };
                return id;
            }
        );
    }
}
