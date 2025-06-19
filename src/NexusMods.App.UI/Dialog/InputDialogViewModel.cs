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
    public string WindowTitle { get; }
    public double WindowWidth { get; }
    public string? Text { get; set; }
    public string? Heading { get; set; }

    /// <summary>
    /// If provided, this will be displayed in a markdown control below the description. Use this
    /// for more descriptive information.
    /// </summary>
    public IMarkdownRendererViewModel? MarkdownRenderer { get; set; }

    public IconValue? Icon { get; }
    public DialogWindowSize DialogWindowSize { get; }

    public IDialogView? View { get; set; }
    public InputDialogResult Result { get; set; }
    public DialogButtonDefinition[] ButtonDefinitions { get; }
    public IViewModelInterface? ContentViewModel { get; set; }
    
    [Reactive]
    public string InputText { get; set; }
    public string InputLabel { get; set; }
    public string InputWatermark { get; set; }

    public R3.ReactiveCommand ClearInputCommand { get; }

    public R3.ReactiveCommand<ButtonDefinitionId, ButtonDefinitionId> ButtonPressCommand { get; }

    public InputDialogViewModel(
        string title,
        DialogButtonDefinition[] buttonDefinitions,
        string? text = null,
        string? heading = null,
        string? inputText = null,
        string? inputLabel = null,
        string? inputWatermark = null,
        IconValue? icon = null,
        DialogWindowSize dialogWindowSize = DialogWindowSize.Small,
        IMarkdownRendererViewModel? markdownRendererViewModel = null,
        IViewModelInterface? contentViewModel = null)
    {
        WindowTitle = title;
        Text = text;
        Heading = heading;
        ButtonDefinitions = buttonDefinitions;
        DialogWindowSize = dialogWindowSize;
        WindowWidth = dialogWindowSize switch
        {
            DialogWindowSize.Small => 320,
            DialogWindowSize.Medium => 480,
            DialogWindowSize.Large => 640,
            _ => 320,
        };

        Icon = icon;
        ContentViewModel = contentViewModel;
        MarkdownRenderer = markdownRendererViewModel;

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
