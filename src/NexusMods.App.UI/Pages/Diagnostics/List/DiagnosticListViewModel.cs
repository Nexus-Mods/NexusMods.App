using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using JetBrains.Annotations;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Settings;
using NexusMods.App.UI.Controls.Diagnostics;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Reloaded.Memory.Extensions;

namespace NexusMods.App.UI.Pages.Diagnostics;

[UsedImplicitly]
internal class DiagnosticListViewModel : APageViewModel<IDiagnosticListViewModel>, IDiagnosticListViewModel
{
    [Reactive] private Diagnostic[] Diagnostics { get; set; } = Array.Empty<Diagnostic>();

    [Reactive] public IDiagnosticEntryViewModel[] DiagnosticEntries { get; private set; } = Array.Empty<IDiagnosticEntryViewModel>();

    [Reactive] public LoadoutId LoadoutId { get; set; }

    [Reactive] public int NumCritical { get; private set; }
    [Reactive] public int NumWarnings { get; private set; }
    [Reactive] public int NumSuggestions { get; private set; }

    [Reactive] public DiagnosticFilter Filter { get; set; }



    private const DiagnosticFilter AllFilter = DiagnosticFilter.Critical | DiagnosticFilter.Warnings | DiagnosticFilter.Suggestions;

    [Reactive] private DiagnosticSettings Settings { get; set; }

    public DiagnosticListViewModel(
        IServiceProvider serviceProvider,
        IWindowManager windowManager,
        IDiagnosticManager diagnosticManager,
        IDiagnosticWriter diagnosticWriter,
        ISettingsManager settingsManager) : base(windowManager)
    {
        TabIcon = IconValues.Cardiology;
        TabTitle = Language.DiagnosticListViewModel_DiagnosticListViewModel_Diagnostics;

        Settings = settingsManager.Get<DiagnosticSettings>();
        settingsManager.GetChanges<DiagnosticSettings>().OnUI().BindToVM(this, vm => vm.Settings);


        this.WhenActivated(disposable =>
        {
            Filter = AllFilter;

            var serialDisposable = new SerialDisposable();
            serialDisposable.DisposeWith(disposable);

            // get diagnostics from the manager
            this.WhenAnyValue(vm => vm.LoadoutId)
                .Do(loadoutId =>
                {
                    serialDisposable.Disposable = diagnosticManager
                        .GetLoadoutDiagnostics(loadoutId)
                        .OnUI()
                        .BindToVM(this, vm => vm.Diagnostics);
                })
                .SubscribeWithErrorLogging()
                .DisposeWith(disposable);

            // filter diagnostics
            var filteredDiagnostics = this.WhenAnyValue(
                    vm => vm.Diagnostics,
                    vm => vm.Filter,
                    vm => vm.Settings)
                .Select(tuple =>
                {
                    var (diagnostics, filter, settings) = tuple;

                    return diagnostics
                        .Where(diagnostic => filter.HasFlagFast(SeverityToFilter(diagnostic.Severity)))
                        .Where(diagnostic => diagnostic.Severity >= settings.MinimumSeverity)
                        .ToArray();
                })
                .Replay(bufferSize: 1);

            filteredDiagnostics.Connect().DisposeWith(disposable);

            // diagnostics to entries
            filteredDiagnostics
                .Select(diagnostics => diagnostics
                    .Select(diagnostic => (IDiagnosticEntryViewModel)new DiagnosticEntryViewModel(diagnostic, diagnosticWriter))
                    .ToArray()
                )
                .BindToVM(this, vm => vm.DiagnosticEntries)
                .DisposeWith(disposable);

            var severityCountObservable = this.WhenAnyValue(vm => vm.Diagnostics)
                .Select(diagnostics => diagnostics
                    .Select(diagnostic => diagnostic.Severity)
                    .GroupBy(x => x)
                    .ToDictionary(group => group.Key, group => group.Count())
                )
                .Replay(bufferSize: 1);

            severityCountObservable.Connect().DisposeWith(disposable);

            // diagnostic counts
            severityCountObservable
                .Select(dict => dict.GetValueOrDefault(DiagnosticSeverity.Suggestion, defaultValue: 0))
                .Select(num => Settings.MinimumSeverity > DiagnosticSeverity.Suggestion ? 0 : num)
                .BindToVM(this, vm => vm.NumSuggestions)
                .DisposeWith(disposable);

            severityCountObservable
                .Select(dict => dict.GetValueOrDefault(DiagnosticSeverity.Warning, defaultValue: 0))
                .Select(num => Settings.MinimumSeverity > DiagnosticSeverity.Warning ? 0 : num)
                .BindToVM(this, vm => vm.NumWarnings)
                .DisposeWith(disposable);

            severityCountObservable
                .Select(dict => dict.GetValueOrDefault(DiagnosticSeverity.Critical, defaultValue: 0))
                .Select(num => Settings.MinimumSeverity > DiagnosticSeverity.Critical ? 0 : num)
                .BindToVM(this, vm => vm.NumCritical)
                .DisposeWith(disposable);

            var entriesSerialDisposable = new SerialDisposable();
            entriesSerialDisposable.DisposeWith(disposable);

            // see details command
            this.WhenAnyValue(vm => vm.DiagnosticEntries)
                .SubscribeWithErrorLogging(entries =>
                {
                    entriesSerialDisposable.Disposable = null;
                    var compositeDisposable = new CompositeDisposable();

                    foreach (var entry in entries)
                    {
                        entry
                            .WhenAnyObservable(x => x.SeeDetailsCommand)
                            .SubscribeWithErrorLogging(tuple =>
                            {
                                var (diagnostic, info) = tuple;
                                var workspaceController = GetWorkspaceController();

                                var pageData = new PageData
                                {
                                    FactoryId = DiagnosticDetailsPageFactory.StaticId,
                                    Context = new DiagnosticDetailsPageContext
                                    {
                                        Diagnostic = diagnostic,
                                    },
                                };

                                var behavior = workspaceController.GetOpenPageBehavior(pageData, info);
                                workspaceController.OpenPage(WorkspaceId, pageData, behavior);
                            })
                            .DisposeWith(compositeDisposable);
                    }

                    entriesSerialDisposable.Disposable = compositeDisposable;
                })
                .DisposeWith(disposable);
        });
    }

    private static DiagnosticFilter SeverityToFilter(DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.Critical => DiagnosticFilter.Critical,
            DiagnosticSeverity.Warning => DiagnosticFilter.Warnings,
            DiagnosticSeverity.Suggestion => DiagnosticFilter.Suggestions,
            _ => DiagnosticFilter.None,
        };
    }
}
