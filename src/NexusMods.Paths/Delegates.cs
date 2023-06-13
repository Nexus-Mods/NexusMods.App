using JetBrains.Annotations;

namespace NexusMods.Paths;

/// <summary>
/// Useful delegates.
/// </summary>
[PublicAPI]
public static class Delegates
{
    /// <summary>
    /// Function with no return value and one <c>ref</c> input value <typeparamref name="TIn"/>.
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    public delegate void ActionRef<TIn>(ref TIn value);

    /// <summary>
    /// Function with return value <typeparamref name="TOut"/> and
    /// one input and one <c>ref</c> input value <typeparamref name="TIn"/>.
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    public delegate TOut FuncRef<TIn, out TOut>(ref TIn value);
}
