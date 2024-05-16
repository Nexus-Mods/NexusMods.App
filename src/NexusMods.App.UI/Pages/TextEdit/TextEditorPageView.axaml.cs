using System.Reactive.Disposables;
using Avalonia.Input;
using Avalonia.Interactivity;
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
        var allThemes = Enum.GetValues<ThemeName>();
        ThemeSelector.ItemsSource = allThemes;
        ThemeSelector.SelectedItem = ThemeName.Dark;

        var textMate = TextEditor.InstallTextMate(registryOptions);

        this.WhenActivated(disposables =>
        {
            Disposable.Create(textMate, x => x.Dispose()).DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.Theme, view => view.ThemeSelector.SelectedItem)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.Theme)
                .SubscribeWithErrorLogging(theme => textMate.SetTheme(registryOptions.LoadTheme(theme)));

            this.BindCommand(ViewModel, vm => vm.SaveCommand, view => view.SaveButton)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.IsReadOnly, view => view.TextEditor.IsReadOnly)
                .DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.IsModified, view => view.TextEditor.IsModified)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.Document)
                .SubscribeWithErrorLogging(document =>
                {
                    TextEditor.Document = document;
                    if (document is null) return;

                    var extension = Extension.FromPath(document.FileName);
                    var language = registryOptions.GetLanguageByExtension(extension.ToString());
                    LanguageNameText.Text = language.ToString();

                    var scopeName = registryOptions.GetScopeByLanguageId(language.Id);
                    textMate.SetGrammar(scopeName);
                })
                .DisposeWith(disposables);

            // change font size using the scroll wheel
            // NOTE(erri120): Using this method allows us to respond to handled events as well.
            // Without this, the scrollbar will handle all wheel related events.
            this.AddDisposableHandler(PointerWheelChangedEvent, (_, args) =>
            {
                if (args.KeyModifiers != KeyModifiers.Control) return;
                if (args.Delta.Y > 0) TextEditor.FontSize++;
                else TextEditor.FontSize = Math.Max(2, TextEditor.FontSize - 1);
            }, routes: RoutingStrategies.Bubble, handledEventsToo: true).DisposeWith(disposables);
        });
    }
}

