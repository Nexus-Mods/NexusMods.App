using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using NexusMods.App.UI;
using NexusMods.App.UI.Extensions;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;

internal class PreviewDesignViewModel : AViewModel<IPreviewViewModel>, IPreviewViewModel
{
    // Design filler data
    public virtual ILocationPreviewTreeViewModel[] Locations { get; } = GetTestData();

    private static ILocationPreviewTreeViewModel[] GetTestData()
    {
        return new[]
        {
            new LocationPreviewTreeDesignViewModel(),
            new LocationPreviewTreeDesignViewModel(),
        };
    }
}
