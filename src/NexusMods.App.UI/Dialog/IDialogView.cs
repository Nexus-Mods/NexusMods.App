namespace NexusMods.App.UI.Dialog;

public interface IDialogView<TReturn>
{
    public void Close();
    public void CloseWindow(object? sender, EventArgs eventArgs);
    public void SetCloseAction(Action closeAction);
    public void SetButtonResult(TReturn bdName);
    public TReturn GetButtonResult();
}
