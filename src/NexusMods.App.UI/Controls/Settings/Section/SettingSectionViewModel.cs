using NexusMods.UI.Sdk;
using NexusMods.UI.Sdk.Settings;

namespace NexusMods.App.UI.Controls.Settings.Section;

public class SettingSectionViewModel : AViewModel<ISettingSectionViewModel>, ISettingSectionViewModel
{
    public SectionDescriptor Descriptor { get; }

    public SettingSectionViewModel(SectionDescriptor descriptor)
    {
        Descriptor = descriptor;
    }
}
