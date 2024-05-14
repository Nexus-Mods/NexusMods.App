using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using JetBrains.Annotations;
using NexusMods.Paths;
using ReactiveUI;
using TextMateSharp.Grammars;

namespace NexusMods.App.UI.Pages.TextEdit;

[UsedImplicitly]
public partial class TextEditorPageView : ReactiveUserControl<ITextEditorPageViewModel>
{
    public TextEditorPageView()
    {
        InitializeComponent();

        TextEditor.Options = new TextEditorOptions
        {
            EnableTextDragDrop = false,
            EnableHyperlinks = false,
        };

        var registryOptions = new RegistryOptions(ThemeName.Dark);
        var textMate = TextEditor.InstallTextMate(registryOptions);

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.IsReadOnly, view => view.TextEditor.IsReadOnly)
                .DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.IsModified, view => view.TextEditor.IsModified)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.Document)
                .SubscribeWithErrorLogging(document =>
                {
                    TextEditor.Document = document;
                    if (document is null) return;

                    try
                    {
                        var extension = Extension.FromPath(document.FileName);
                        textMate.SetGrammar(registryOptions.GetScopeByExtension(extension.ToString()));
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                })
                .DisposeWith(disposables);
        });
    }
}

