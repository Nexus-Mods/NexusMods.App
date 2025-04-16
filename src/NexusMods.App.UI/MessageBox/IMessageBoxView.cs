namespace NexusMods.App.UI.MessageBox;

public interface IMessageBoxView<T>
{
    public void Close();
    public void CloseWindow(object? sender, EventArgs eventArgs);
    public void SetCloseAction(Action closeAction);
    public void SetButtonResult(T bdName);
    public T GetButtonResult();
}
