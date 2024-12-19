using System.Diagnostics;
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
                            AllTab.Header = string.Format(Language.DiagnosticListView_DiagnosticListView_All, total);
                            SuggestionTab.Header = string.Format(Language.DiagnosticListView_DiagnosticListView_Suggestions, numSuggestions);
                            WarningTab.Header = string.Format(Language.DiagnosticListView_DiagnosticListView_Warnings, numWarnings);
                            CriticalTab.Header = string.Format(Language.DiagnosticListView_DiagnosticListView_Critical, numCritical);
                
                            EmptyState.IsActive = total == 0;
                        }
                    )
                    .DisposeWith(disposable);
                
                // // bind all DiagnosticEntries to the AllItemsControl
                this.OneWayBind(ViewModel, vm => vm.DiagnosticEntries, view => view.HealthCheckItemsControl.ItemsSource)
                    .DisposeWith(disposable);
                
                this.WhenAnyValue(view => view.TabControl.SelectedItem)
                    .Select(selectedItem =>
                    {
                        if (ReferenceEquals(selectedItem, AllTab)) return DiagnosticFilter.Suggestions | DiagnosticFilter.Warnings | DiagnosticFilter.Critical;
                        if (ReferenceEquals(selectedItem, SuggestionTab)) return DiagnosticFilter.Suggestions;
                        if (ReferenceEquals(selectedItem, WarningTab)) return DiagnosticFilter.Warnings;
                        if (ReferenceEquals(selectedItem, CriticalTab)) return DiagnosticFilter.Critical;
                        throw new UnreachableException();
                    })
                    .Subscribe(filter =>
                    {
                        ViewModel!.Filter = filter;
                    })
                    .DisposeWith(disposable);
            }
        );
    }
}
