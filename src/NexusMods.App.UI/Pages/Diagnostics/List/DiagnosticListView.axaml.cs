using Avalonia.ReactiveUI;
using JetBrains.Annotations;

namespace NexusMods.App.UI.Pages.Diagnostics;

[UsedImplicitly]
public partial class DiagnosticListView : ReactiveUserControl<IDiagnosticListViewModel>
{
    public DiagnosticListView()
    {
        InitializeComponent();
    }
}

