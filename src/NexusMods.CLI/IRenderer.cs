namespace NexusMods.CLI;

public interface IRenderer
{ 
    Task Render<T>(T o);
    public string Name { get; }
    void RenderBanner();
}