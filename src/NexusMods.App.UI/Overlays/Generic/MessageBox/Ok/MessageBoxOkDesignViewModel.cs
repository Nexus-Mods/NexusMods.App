using System.ComponentModel;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays.Generic.MessageBox.Ok;

public class MessageBoxOkDesignViewModel: IMessageBoxOkViewModel
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public ViewModelActivator Activator { get; } = null!;
    public IOverlayController Controller { get; set; } = null!;
    public Status Status { get; set; }
    public Task CompletionTask { get; } = null!;

    public void Close()
    {
        throw new NotImplementedException();
    }

    public Unit Result { get; set; }
    public void Complete(Unit result)
    {
        throw new NotImplementedException();
    }

    public string Title { get; set; } = "Title";

    public string Description { get; set; } = """
                                              Integrate seamlessly with Nexus Mods with direct access to the world’s largest modding platform. 
                                              
                                              Stay updated with automatic version checks and enjoy unmatched compatibility.
                                              """;

    public IMarkdownRendererViewModel? MarkdownRenderer { get; set; } = null;
}
