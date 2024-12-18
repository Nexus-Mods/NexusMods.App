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
                // update headers whenever the counts change
                this.WhenAnyValue(
                        view => view.ViewModel!.NumCritical,
                        view => view.ViewModel!.NumWarnings,
                        view => view.ViewModel!.NumSuggestions
                    )
                    .Subscribe(counts =>
                        {
                            var (numCritical, numWarnings, numSuggestions) = counts;
                            var total = numCritical + numWarnings + numSuggestions;

                            // set tab headers
                            AllDiagnosticsTab.Header = string.Format(Language.DiagnosticListView_DiagnosticListView_All, total);
                            SuggestionDiagnosticsTab.Header = string.Format(Language.DiagnosticListView_DiagnosticListView_Suggestions, numSuggestions);
                            WarningDiagnosticsTab.Header = string.Format(Language.DiagnosticListView_DiagnosticListView_Warnings, numWarnings);
                            CriticalDiagnosticsTab.Header = string.Format(Language.DiagnosticListView_DiagnosticListView_Critical, numCritical);

                            EmptyState.IsActive = total == 0;
                        }
                    )
                    .DisposeWith(disposable);

                // bind all DiagnosticEntries to the AllItemsControl
                this.OneWayBind(ViewModel, vm => vm.DiagnosticEntries, view => view.AllItemsControl.ItemsSource)
                    .DisposeWith(disposable);

                // need to filter DiagnosticEntries and Bind them to the correct ItemsControl

                this.WhenAnyValue(view => view.ViewModel!.DiagnosticEntries)
                    .Select(entries => entries.Where(e => e.Severity == DiagnosticSeverity.Critical))
                    .BindTo(this, view => view.CriticalItemsControl.ItemsSource)
                    .DisposeWith(disposable);

                this.WhenAnyValue(view => view.ViewModel!.DiagnosticEntries)
                    .Select(entries => entries.Where(e => e.Severity == DiagnosticSeverity.Warning))
                    .BindTo(this, view => view.WarningItemsControl.ItemsSource)
                    .DisposeWith(disposable);

                this.WhenAnyValue(view => view.ViewModel!.DiagnosticEntries)
                    .Select(entries => entries.Where(e => e.Severity == DiagnosticSeverity.Suggestion))
                    .BindTo(this, view => view.SuggestionItemsControl.ItemsSource)
                    .DisposeWith(disposable);
            }
        );
    }
}
