using NexusMods.App.UI.MessageBox.Enums;

namespace NexusMods.App.UI.MessageBox;

public interface IMessageBoxViewModel<T>
{
    public string ContentTitle { get; }
    public string ContentMessage { get; set; }
}
