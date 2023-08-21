using System.Windows.Input;
using NexusMods.App.UI.Icons;
using NexusMods.App.UI.Localization;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class IconViewModel : AViewModel<IIconViewModel>, IIconViewModel, IDisposable
{
    [Reactive]
    public string Name { get; set; } = "";

    [Reactive]
    public IconType Icon { get; set; }

    [Reactive] public ICommand Activate { get; set; } = Initializers.ICommand;

    private readonly LocalizedStringUpdater _nameUpdater;

    public IconViewModel(Func<string> getName) => _nameUpdater = new LocalizedStringUpdater(() => Name = getName());

    public void Dispose() => _nameUpdater.Dispose();
}
