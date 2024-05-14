using System.Reactive.Disposables;
using System.Text;
using AvaloniaEdit.Document;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.IO;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.TextEdit;

[UsedImplicitly]
public class TextEditorPageViewModel : APageViewModel<ITextEditorPageViewModel>, ITextEditorPageViewModel
{
    [Reactive] public TextEditorPageContext? Context { get; set; }

    [Reactive] public bool IsReadOnly { get; set; }

    [Reactive] public bool IsModified { get; set; }

    [Reactive] public TextDocument? Document { get; set; }

    private readonly ReactiveCommand<TextEditorPageContext, ValueTuple<TextEditorPageContext, string>> _loadFileCommand;

    public TextEditorPageViewModel(
        ILogger<TextEditorPageViewModel> logger,
        IWindowManager windowManager,
        IFileStore fileStore) : base(windowManager)
    {
        TabIcon = IconValues.FileDocumentOutline;
        TabTitle = "Text Editor";

        _loadFileCommand = ReactiveCommand.CreateFromTask<TextEditorPageContext, ValueTuple<TextEditorPageContext, string>>(async context =>
        {
            var fileHash = context.FileHash;
            logger.LogDebug("Loading file {Hash} into the Text Editor", fileHash);

            await using var stream = await fileStore.GetFileStream(fileHash);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var contents = await reader.ReadToEndAsync();

            return (context, contents);
        });

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(vm => vm.Context)
                .WhereNotNull()
                .OffUi()
                .InvokeCommand(_loadFileCommand)
                .DisposeWith(disposables);

            this.WhenAnyObservable(vm => vm._loadFileCommand)
                .OnUI()
                .SubscribeWithErrorLogging(output =>
                {
                    var (context, contents) = output;
                    var fileName = context.FileName;
                    TabTitle = fileName.ToString();

                    var document = new TextDocument(new StringTextSource(contents))
                    {
                        FileName = fileName.ToString(),
                    };

                    Document = document;
                })
                .DisposeWith(disposables);
        });
    }
}
