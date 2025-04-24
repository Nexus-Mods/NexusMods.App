namespace NexusMods.App.UI.Dialog;

public interface IDialogView<TResult>
{
    public void Close();
    public void CloseWindow(object? sender, EventArgs eventArgs);
    public void SetCloseAction(Action closeAction);
    public void SetButtonResult(TResult bdName);
    public TResult GetButtonResult();
}
