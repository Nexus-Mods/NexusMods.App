using NexusMods.App.UI.MessageBox.Enums;

namespace NexusMods.App.UI.MessageBox;

public interface IMessageBoxViewModel<T>
{
    void SetView(IMessageBoxView<T> view);
    
    public MessageBoxButtonDefinition[] ButtonDefinitions { get; }
        
    public string ContentTitle { get; }
    public string ContentMessage { get; set; }
    public MessageBoxSize MessageBoxSize { get; }
}
