using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.UI;
using NexusMods.UI.Sdk.Settings;

namespace NexusMods.App.UI.Controls.Settings.Section;

public interface ISettingSectionViewModel : IViewModelInterface
{
    SectionDescriptor Descriptor { get; }
}
