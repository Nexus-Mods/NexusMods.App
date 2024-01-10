using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using FluentAssertions;
using JetBrains.Annotations;
using NexusMods.App.UI.WorkspaceSystem;
using NSubstitute;

namespace NexusMods.UI.Tests.WorkspaceSystem;

[UsedImplicitly]
[SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
public partial class GridUtilsTests
{
    private static WorkspaceGridState CreateState(bool isHorizontal, params PanelGridState[] panels)
    {
        return WorkspaceGridState.From(panels, isHorizontal);
    }
}
