using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Resources;
using ReactiveUI;
using Reloaded.Memory.Extensions;

namespace NexusMods.App.UI.Pages.Diagnostics;

[UsedImplicitly]
public partial class DiagnosticListView : ReactiveUserControl<IDiagnosticListViewModel>
{
    public DiagnosticListView()
    {
        InitializeComponent();

        this.WhenActivated(disposable =>
        {
            this.OneWayBind(ViewModel, vm => vm.DiagnosticEntries, view => view.ItemsControl.ItemsSource)
                .DisposeWith(disposable);

            // use counts
            this.WhenAnyValue(
                view => view.ViewModel!.NumCritical,
                view => view.ViewModel!.NumWarnings,
                view => view.ViewModel!.NumSuggestions)
                .Subscribe(counts =>
                {
                    var (numCritical, numWarnings, numSuggestions) = counts;
                    var total = numCritical + numWarnings + numSuggestions;

                    // set button texts
                    AllDiagnosticsButtonText.Text = string.Format(Language.DiagnosticListView_DiagnosticListView_All, total);
                    CriticalDiagnosticsButtonText.Text = string.Format(Language.DiagnosticListView_DiagnosticListView_Critical, numCritical);
                    WarningDiagnosticsButtonText.Text = string.Format(Language.DiagnosticListView_DiagnosticListView_Warnings, numWarnings);
                    SuggestionDiagnosticsButtonText.Text = string.Format(Language.DiagnosticListView_DiagnosticListView_Suggestions, numSuggestions);

                    EmptyState.IsActive = total == 0;
                })
                .DisposeWith(disposable);

            // toggle commands
            this.BindCommand(ViewModel,
                    vm => vm.ToggleSeverityCommand,
                    view => view.CriticalDiagnosticsButton,
                    withParameter: Observable.Return(DiagnosticSeverity.Critical))
                .DisposeWith(disposable);

            this.BindCommand(ViewModel,
                    vm => vm.ToggleSeverityCommand,
                    view => view.WarningDiagnosticsButton,
                    withParameter: Observable.Return(DiagnosticSeverity.Warning))
                .DisposeWith(disposable);

            this.BindCommand(ViewModel,
                    vm => vm.ToggleSeverityCommand,
                    view => view.SuggestionDiagnosticsButton,
                    withParameter: Observable.Return(DiagnosticSeverity.Suggestion))
                .DisposeWith(disposable);

            this.BindCommand(ViewModel, vm => vm.ShowAllCommand, view => view.AllDiagnosticsButton)
                .DisposeWith(disposable);

            // filter changes
            this.WhenAnyValue(view => view.ViewModel!.Filter)
                .Subscribe(filter =>
                {
                    var showCritical = filter.HasFlagFast(DiagnosticFilter.Critical);
                    var showWarnings = filter.HasFlagFast(DiagnosticFilter.Warnings);
                    var showSuggestions = filter.HasFlagFast(DiagnosticFilter.Suggestions);

                    const string selectedClass = "Selected";

                    if (showCritical && showWarnings && showSuggestions)
                    {
                        AllDiagnosticsButton.Classes.Add(selectedClass);
                        CriticalDiagnosticsButton.Classes.Remove(selectedClass);
                        WarningDiagnosticsButton.Classes.Remove(selectedClass);
                        SuggestionDiagnosticsButton.Classes.Remove(selectedClass);
                    }
                    else
                    {
                        AllDiagnosticsButton.Classes.Remove(selectedClass);
                        CriticalDiagnosticsButton.Classes.ToggleIf(selectedClass, showCritical);
                        WarningDiagnosticsButton.Classes.ToggleIf(selectedClass, showWarnings);
                        SuggestionDiagnosticsButton.Classes.ToggleIf(selectedClass, showSuggestions);
                    }
                })
                .DisposeWith(disposable);
        });
    }
}

