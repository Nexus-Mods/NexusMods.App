using AvaloniaEdit.Document;
using JetBrains.Annotations;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.TextEdit;

[UsedImplicitly]
public class TextEditorPageViewModel : APageViewModel<ITextEditorPageViewModel>, ITextEditorPageViewModel
{
    [Reactive] public bool IsReadOnly { get; set; }

    [Reactive] public bool IsModified { get; set; }

    [Reactive] public TextDocument? Document { get; set; }

    public TextEditorPageViewModel(IWindowManager windowManager) : base(windowManager) { }
}
