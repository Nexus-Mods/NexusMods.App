using System.Diagnostics;
using System.IO.Hashing;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using AvaloniaEdit.Document;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Settings;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Settings;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TextMateSharp.Grammars;
using Hash = NexusMods.Hashing.xxHash3.Hash;

namespace NexusMods.App.UI.Pages.TextEdit;

[UsedImplicitly]
public class TextEditorPageViewModel : APageViewModel<ITextEditorPageViewModel>, ITextEditorPageViewModel
{
    [Reactive] public TextEditorPageContext? Context { get; set; }

    [Reactive] public bool IsReadOnly { get; set; } = true;

    [Reactive] public bool IsModified { get; set; }

    private static readonly TextDocument EmptyDocument = new(new StringTextSource("")) { FileName = "empty.txt" };
    [Reactive] public TextDocument Document { get; set; } = EmptyDocument;

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    private readonly ReactiveCommand<TextEditorPageContext, ValueTuple<TextEditorPageContext, string>> _loadFileCommand;

    [Reactive] public ThemeName Theme { get; set; }

    [Reactive] public double FontSize { get; set; }

    public TextEditorPageViewModel(
        ILogger<TextEditorPageViewModel> logger,
        IWindowManager windowManager,
        IFileStore fileStore,
        IConnection connection,
        ISettingsManager settingsManager) : base(windowManager)
    {
        TabIcon = IconValues.FileEdit;
        TabTitle = "Text Editor";

        var initialSettings = settingsManager.Get<TextEditorSettings>();
        Theme = initialSettings.ThemeName;
        FontSize = initialSettings.FontSize;

        _loadFileCommand = ReactiveCommand.CreateFromTask<TextEditorPageContext, ValueTuple<TextEditorPageContext, string>>(async context =>
        {
            var hash = context.FileId.Match(
                f0: loadoutFileId => LoadoutFile.Load(connection.Db, loadoutFileId).Hash,
                f1: libraryFileId => LibraryFile.Load(connection.Db, libraryFileId).Hash
            );

            await using var stream = await fileStore.GetFileStream(hash);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var contents = await reader.ReadToEndAsync();

            return (context, contents);
        });

        var canSaveObservable = this.WhenAnyValue(
            vm => vm.Document,
            vm => vm.IsModified,
            vm => vm.IsReadOnly,
            (document, isModified, isReadOnly) => document != EmptyDocument && isModified && !isReadOnly
        );

        SaveCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            Debug.Assert(Context is not null);

            var loadoutFileId = Context.FileId.AsT0;
            var text = Document.Text;

            // hash and store the new contents
            var bytes = Encoding.UTF8.GetBytes(text);
            var hash = Hash.From(XxHash3.HashToUInt64(bytes));
            var size = Size.From((ulong)bytes.Length);

            using (var streamFactory = new MemoryStreamFactory(Context.FilePath, new MemoryStream(bytes, writable: false)))
            {
                if (!await fileStore.HaveFile(hash))
                    await fileStore.BackupFiles([new ArchivedFileEntry(streamFactory, hash, size)], deduplicate: false);
            }

            // update the file
            using (var tx = connection.BeginTransaction())
            {
                tx.Add(loadoutFileId, LoadoutFile.Hash, hash);
                tx.Add(loadoutFileId, LoadoutFile.Size, size);
                // TODO: Revise
                await tx.Commit();
            }

            IsModified = false;
        }, canSaveObservable);

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(vm => vm.Context)
                .WhereNotNull()
                .Select(context =>
                {
                    var sliceDescriptor = context.FileId.Match(
                        f0: loadoutFileId => SliceDescriptor.Create(loadoutFileId),
                        f1: libraryFileId => SliceDescriptor.Create(libraryFileId)
                    );

                    return connection.ObserveDatoms(sliceDescriptor).Select(_ => context);
                })
                .Switch()
                .OffUi()
                .InvokeReactiveCommand(_loadFileCommand)
                .DisposeWith(disposables);

            this.WhenAnyObservable(vm => vm._loadFileCommand)
                .OnUI()
                .SubscribeWithErrorLogging(output =>
                {
                    var (context, contents) = output;
                    var filePath = context.FilePath;
                    var fileName = filePath.FileName.ToString();
                    TabTitle = fileName;

                    var document = new TextDocument(new StringTextSource(contents))
                    {
                        FileName = fileName,
                    };

                    Document = document;
                    IsReadOnly = context.IsReadOnly || context.FileId.IsT1;
                })
                .DisposeWith(disposables);

            settingsManager
                .GetChanges<TextEditorSettings>()
                .OnUI()
                .SubscribeWithErrorLogging(settings =>
                {
                    Theme = settings.ThemeName;
                    FontSize = settings.FontSize;
                })
                .DisposeWith(disposables);

            this.WhenAnyValue(vm => vm.Theme, vm => vm.FontSize)
                // NOTE(erri120): Sample to prevent rapid changes when using scroll wheel to change font size
                .Sample(interval: TimeSpan.FromMilliseconds(500))
                .SubscribeWithErrorLogging(_ =>
                {
                    settingsManager.Update<TextEditorSettings>(settings => settings with
                    {
                        ThemeName = Theme,
                        FontSize = FontSize,
                    });
                })
                .DisposeWith(disposables);
        });
    }
}
