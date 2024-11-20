using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.UI;

namespace NexusMods.App.UI.Controls.Settings.Section;

public interface ISettingSectionViewModel : IViewModelInterface
{
    ISettingsSectionDescriptor Descriptor { get; }
}
