using System.Reactive;
using NexusMods.Abstractions.UI;
using ReactiveUI;

namespace NexusMods.App.UI.Dialog;

public interface IDialogContentViewModel: IViewModelInterface
{
    void CloseWindow(string id);
    void SetCloseable(IDialogViewModel<ButtonDefinitionId> dialogViewModel);
    ReactiveCommand<string, Unit> CloseWindowCommand { get; }
}
