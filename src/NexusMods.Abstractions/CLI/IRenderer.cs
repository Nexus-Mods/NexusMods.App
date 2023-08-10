namespace NexusMods.Abstractions.CLI;

/// <summary>
/// Generic interface used by CLI verbs to output data and progress updates.
/// </summary>
public interface IRenderer
{
    /// <summary>
    /// Renders the results of an operation.
    /// </summary>
    /// <param name="o">
    ///    The object responsible for the rendering.
    /// </param>
    /// <typeparam name="T">
    ///    One of the types defined in <see cref="DataOutputs"/>.
    /// </typeparam>
    Task Render<T>(T o);

    /// <summary>
    /// Name of the renderer used to display the data.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Renders a logo/welcome screen banner to the user.
    /// </summary>
    void RenderBanner();

    /// <summary>
    /// Renders the progress of an ongoing operation.
    /// </summary>
    /// <param name="token">Token which can be used to cancel the task.</param>
    /// <param name="f">
    ///     The function to execute with the progress.
    /// </param>
    /// <param name="showSize">Whether the size of the operation should be shown.</param>
    /// <typeparam name="T">Type of return value returned from the function passed.</typeparam>
    /// <remarks>
    ///     Progress is determined from the 'IResource'(s) that were passed to the Renderer.
    /// </remarks>
    public Task<T> WithProgress<T>(CancellationToken token, Func<Task<T>> f, bool showSize = true);
}
