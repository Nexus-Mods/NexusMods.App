using NexusMods.UI.Sdk;
using NexusMods.UI.Sdk.Settings;

namespace NexusMods.App.UI.Controls.Settings.Section;

public interface ISettingSectionViewModel : IViewModelInterface
{
    SectionDescriptor Descriptor { get; }
}
