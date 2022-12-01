namespace NexusMods.CLI;

public interface IRenderer
{ 
    Task Render<T>(T o);
    public string Name { get; }
    void RenderBanner();
    public Task<T> WithProgress<T>(CancellationToken token, Func<Task<T>> f, bool showSize = true);
}