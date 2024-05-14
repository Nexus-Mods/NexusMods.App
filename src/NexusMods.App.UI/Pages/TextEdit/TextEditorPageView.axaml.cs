using Avalonia.ReactiveUI;
using JetBrains.Annotations;

namespace NexusMods.App.UI.Pages.TextEdit;

[UsedImplicitly]
public partial class TextEditorPageView : ReactiveUserControl<ITextEditorPageViewModel>
{
    public TextEditorPageView()
    {
        InitializeComponent();
    }
}

