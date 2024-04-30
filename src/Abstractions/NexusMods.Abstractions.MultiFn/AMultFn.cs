namespace NexusMods.Abstractions.MultiFn;

/// <summary>
/// A multi-function comes from the Clojure/Lisp world, where it is a function that can have multiple implementations,
/// it's a form of polymorphism and open predicate dispatch. A overrides are added to the function along with a predicate
/// that determines if the override should be called. The function will call the first override that returns true for the
/// given input.
/// </summary>
public class AMultiFn<TInput, TOutput>
{
    private List<(Func<TInput, bool> Supports, Func<TInput, TOutput> Impl)> _impls = new();
    
    /// <summary>
    /// Adds a new override to the function, if the support function returns true, the invoke function will be called. If
    /// multiple overrides support the input, the first one added will be called, the others will be ignored.
    /// </summary>
    public void Add(Func<TInput, bool> supports, Func<TInput, TOutput> impl)
    {
        _impls.Add((supports, impl));
    }
    
    /// <summary>
    /// Returns true if some override of this function supports the input.
    /// </summary>
    public bool Supports(TInput input) => _impls.Any(impl => impl.Supports(input));
    
    /// <summary>
    /// The default implementation of the function, if no override supports the input, this will be called.
    /// </summary>
    public Func<TInput, TOutput> DefualtImpl { get; set; } = 
        input => throw new InvalidOperationException($"No implementation supports the input: {input}");
    
    /// <summary>
    /// Invokes the function with the given input.
    /// </summary>
    public TOutput Invoke(TInput input)
    {
        foreach (var impl in _impls)
        {
            if (impl.Supports(input))
                return impl.Impl(input);
        }
        return DefualtImpl(input);
    }
}
