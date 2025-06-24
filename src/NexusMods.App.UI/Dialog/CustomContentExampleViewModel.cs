using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia.Threading;
using NexusMods.Abstractions.UI;
using ReactiveUI;

namespace NexusMods.App.UI.Dialog;

public class CustomContentExampleViewModel : AViewModel<IViewModelInterface>, IViewModelInterface
{
    public bool DontAskAgain { get; set; }
    public bool ShouldEndorseDownloadedMods { get; set; }
    //public ReactiveCommand<string, Unit> CloseWindowCommand { get; }
    public string CustomText { get; set; }
    public ObservableCollection<string> MyItems { get; set; } = new ()
    {
        "Item 1",
        "Item 2",
        "Item 3"
    };

    public string? MySelectedItem { get; set; }

    public CustomContentExampleViewModel(string text)
    {
        CustomText = text;
    }
}
