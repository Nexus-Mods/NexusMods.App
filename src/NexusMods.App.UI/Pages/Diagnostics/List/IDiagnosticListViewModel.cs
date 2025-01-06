using System.Collections.ObjectModel;
using System.Reactive;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.App.UI.Controls.Diagnostics;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Diagnostics;

public interface IDiagnosticListViewModel : IPageViewModelInterface
{
    public LoadoutId LoadoutId { get; set; }

    public IDiagnosticEntryViewModel[] DiagnosticEntries { get; }

    public int NumCritical { get; }
    public int NumWarnings { get; }
    public int NumSuggestions { get; }

    public DiagnosticFilter Filter { get; set; }


}

[Flags]
public enum DiagnosticFilter
{
    None = 0,
    Critical = 1 << 0,
    Warnings = 1 << 1,
    Suggestions = 1 << 2,
}
