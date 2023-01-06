namespace NexusMods.CLI;

/// <summary>
/// Generic interface used by CLI verbs to output data and progress updates
/// </summary>
public interface IRenderer
{ 
    Task Render<T>(T o);
    public string Name { get; }
    void RenderBanner();
    public Task<T> WithProgress<T>(CancellationToken token, Func<Task<T>> f, bool showSize = true);
}