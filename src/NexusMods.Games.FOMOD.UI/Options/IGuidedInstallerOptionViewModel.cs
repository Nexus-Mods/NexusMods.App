using NexusMods.Abstractions.GuidedInstallers;
using NexusMods.UI.Sdk;

namespace NexusMods.Games.FOMOD.UI;

public interface IGuidedInstallerOptionViewModel : IViewModelInterface
{
    public Option Option { get; }

    public OptionGroup Group { get; }

    /// <summary>
    /// Gets or sets whether the user can interact with the input control.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether the input control is checked.
    /// </summary>
    public bool IsChecked { get; set; }

    /// <summary>
    /// Gets or sets whether the user selection is invalid.
    /// </summary>
    public bool IsValid { get; set; }
}
