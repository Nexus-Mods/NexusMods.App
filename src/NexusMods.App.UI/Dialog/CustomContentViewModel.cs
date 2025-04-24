using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia.Threading;
using NexusMods.Abstractions.UI;
using ReactiveUI;

namespace NexusMods.App.UI.Dialog;

public class CustomContentViewModel : AViewModel<IDialogContentViewModel>, IDialogContentViewModel
{
    public bool DontAskAgain { get; set; }
    public bool ShouldEndorseDownloadedMods { get; set; }
    public ReactiveCommand<string, Unit> CloseWindowCommand { get; }
    public string CustomText { get; set; }
    public ObservableCollection<string> MyItems { get; set; } = new ()
    {
        "Item 1",
        "Item 2",
        "Item 3"
    };

    private string? _mySelectedItem;
    public string? MySelectedItem
    {
        get { return _mySelectedItem; }
        set
        {
            // Some logic here
            _mySelectedItem = value;
        }
    }
    
    public CustomContentViewModel(string text)
    {
        CustomText = text;
        CloseWindowCommand = ReactiveCommand.Create<string>(CloseWindow);
    }

    public void CloseWindow(string id)
    {
        _parent?.CloseWindow(ButtonDefinitionId.From(string.IsNullOrEmpty(id) ? "none" : id));
    }

    private IDialogViewModel<ButtonDefinitionId>? _parent;


    public void SetCloseable(IDialogViewModel<ButtonDefinitionId> dialogViewModel)
    {
        _parent = dialogViewModel;
    }
}
