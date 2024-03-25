using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls.Diagnostics;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Reloaded.Memory.Extensions;

namespace NexusMods.App.UI.Pages.Diagnostics;

[UsedImplicitly]
internal class DiagnosticListViewModel : APageViewModel<IDiagnosticListViewModel>, IDiagnosticListViewModel
{
    private readonly SourceList<IDiagnosticEntryViewModel> _sourceList = new();
    private readonly ReadOnlyObservableCollection<IDiagnosticEntryViewModel> _entries;
    public ReadOnlyObservableCollection<IDiagnosticEntryViewModel> DiagnosticEntries => _entries;

    [Reactive] public LoadoutId LoadoutId { get; set; }

    [Reactive] public int NumCritical { get; private set; }
    [Reactive] public int NumWarnings { get; private set; }
    [Reactive] public int NumSuggestions { get; private set; }

    [Reactive] public DiagnosticFilter Filter { get; private set; }

    public ReactiveCommand<DiagnosticSeverity, Unit> ToggleSeverityCommand { get; }

    public ReactiveCommand<Unit, Unit> ShowAllCommand { get; }

    private const DiagnosticFilter AllFilter = DiagnosticFilter.Critical | DiagnosticFilter.Warnings | DiagnosticFilter.Suggestions;

    public DiagnosticListViewModel(
        IServiceProvider serviceProvider,
        IWindowManager windowManager,
        IDiagnosticManager diagnosticManager) : base(windowManager)
    {
        _sourceList
            .Connect()
            .AutoRefreshOnObservable(_ => this.WhenAnyValue(vm => vm.Filter))
            .Filter(entry => Filter.HasFlagFast(SeverityToFilter(entry.Severity)))
            .Bind(out _entries)
            .Subscribe();

        ToggleSeverityCommand = ReactiveCommand.Create<DiagnosticSeverity>(severity =>
        {
            var flag = SeverityToFilter(severity);

            if (Filter == AllFilter) Filter = flag;
            else Filter = flag;

            if (Filter == DiagnosticFilter.None) Filter = AllFilter;
        });

        ShowAllCommand = ReactiveCommand.Create(() =>
        {
            Filter = AllFilter;
        });

        this.WhenActivated(disposable =>
        {
            {
                var workspaceController = GetWorkspaceController();
                workspaceController.SetTabTitle("Diagnostics", WorkspaceId, PanelId, TabId);
                workspaceController.SetIcon(DiagnosticIcons.DiagnosticIcon1, WorkspaceId, PanelId, TabId);
            }

            Filter = AllFilter;

            var serialDisposable = new SerialDisposable();
            serialDisposable.DisposeWith(disposable);

            // diagnostics to entry view models
            this.WhenAnyValue(vm => vm.LoadoutId)
                .Do(loadoutId =>
                {
                    serialDisposable.Disposable = diagnosticManager
                        .GetLoadoutDiagnostics(loadoutId)
                        .Select(diagnostics => diagnostics
                            .Select(diagnostic => new DiagnosticEntryViewModel(diagnostic, serviceProvider.GetRequiredService<IDiagnosticWriter>()))
                            .ToArray()
                        )
                        .OnUI()
                        .SubscribeWithErrorLogging(entries =>
                        {
                            int numSuggestions = 0, numWarnings = 0, numCritical = 0;
                            foreach (var entry in entries)
                            {
                                switch (entry.Severity)
                                {
                                    case DiagnosticSeverity.Suggestion:
                                        numSuggestions += 1;
                                        break;
                                    case DiagnosticSeverity.Warning:
                                        numWarnings += 1;
                                        break;
                                    case DiagnosticSeverity.Critical:
                                        numCritical += 1;
                                        break;
                                    default: break;
                                }
                            }

                            _sourceList.Edit(updater =>
                            {
                                updater.Clear();
                                updater.AddRange(entries);
                            });

                            NumSuggestions = numSuggestions;
                            NumWarnings = numWarnings;
                            NumCritical = numCritical;
                        });
                })
                .SubscribeWithErrorLogging()
                .DisposeWith(disposable);

            // see details command
            _sourceList
                .Connect()
                .MergeMany(entry => entry.SeeDetailsCommand)
                .SubscribeWithErrorLogging(diagnostic =>
                {
                    var workspaceController = GetWorkspaceController();

                    var pageData = new PageData
                    {
                        FactoryId = DiagnosticDetailsPageFactory.StaticId,
                        Context = new DiagnosticDetailsPageContext
                        {
                            Diagnostic = diagnostic,
                        },
                    };

                    // TODO: use https://github.com/Nexus-Mods/NexusMods.App/issues/942
                    var input = NavigationInput.Default;

                    var behavior = workspaceController.GetDefaultOpenPageBehavior(pageData, input, IdBundle);
                    workspaceController.OpenPage(WorkspaceId, pageData, behavior);
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
