using System.Collections.ObjectModel;
using Avalonia.Threading;
using NexusMods.Abstractions.UI;
using ReactiveUI;
using R3;

namespace NexusMods.App.UI.Dialog;

public interface ITextInputViewModel: IViewModelInterface
{
    string? InputLabel { get; set; }
    string? InputWatermark { get; set; }
    string? InputText { get; set; }
    
    ReactiveCommand<Unit> ClearInputCommand { get; set; }
}

public class TextInputViewModel : AViewModel<ITextInputViewModel>, ITextInputViewModel
{
    public string? InputLabel { get; set; }
    public string? InputWatermark { get; set; }

    private string? _inputText;
    public string? InputText
    {
        get => _inputText;
        set => this.RaiseAndSetIfChanged(ref _inputText, value);
    }
    
    public ReactiveCommand<Unit> ClearInputCommand { get; set; }
    
    public TextInputViewModel(string? label = null, string? watermark = null, string? text = null)
    {
        InputLabel = label;
        InputWatermark = watermark;
        InputText = text;
        
        ClearInputCommand = new ReactiveCommand<Unit>(
            executeAsync: (_, cancellationToken) =>
            {
                InputText = string.Empty;
                return ValueTask.CompletedTask;
            }
        );
    }
}
