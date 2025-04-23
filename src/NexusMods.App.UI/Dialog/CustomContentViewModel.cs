using System.Reactive;
using Avalonia.Threading;
using NexusMods.Abstractions.UI;
using ReactiveUI;

namespace NexusMods.App.UI.Dialog;

public class CustomContentViewModel: AViewModel<IDialogContentViewModel>, IDialogContentViewModel
{
    public string CustomText { get; set; }
    
    public CustomContentViewModel(string text)
    {
        CustomText = text;
    }
    
}
