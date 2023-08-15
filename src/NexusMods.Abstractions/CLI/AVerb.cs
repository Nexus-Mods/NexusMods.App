

// ReSharper disable InconsistentNaming

namespace NexusMods.Abstractions.CLI;

/// <summary>
/// Abstract class for a verb that takes no arguments
/// </summary>
public interface AVerb : IVerb
{
    Delegate IVerb.Delegate => Run;

    /// <summary>
    /// Runs the verb
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<int> Run(CancellationToken token);
}

/// <summary>
/// Abstract class for a verb that takes a single argument
/// </summary>
/// <typeparam name="T"></typeparam>
public interface AVerb<in T> : IVerb
{
    Delegate IVerb.Delegate => Run;

    /// <summary>
    /// Runs the verb
    /// </summary>
    /// <param name="a"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<int> Run(T a, CancellationToken token);
}

/// <summary>
/// Abstract class for a verb that takes two arguments
/// </summary>
/// <typeparam name="T1"></typeparam>
/// <typeparam name="T2"></typeparam>
public interface AVerb<in T1, in T2> : IVerb
{
    Delegate IVerb.Delegate => Run;

    /// <summary>
    /// Runs the verb
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<int> Run(T1 a, T2 b, CancellationToken token);
}

/// <summary>
/// Abstract class for a verb that takes three arguments
/// </summary>
/// <typeparam name="T1"></typeparam>
/// <typeparam name="T2"></typeparam>
/// <typeparam name="T3"></typeparam>
public interface AVerb<in T1, in T2, in T3> : IVerb
{
    Delegate IVerb.Delegate => Run;

    /// <summary>
    /// Runs the verb
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<int> Run(T1 a, T2 b, T3 c, CancellationToken token);
}

/// <summary>
/// Abstract class for a verb that takes four arguments
/// </summary>
/// <typeparam name="T1"></typeparam>
/// <typeparam name="T2"></typeparam>
/// <typeparam name="T3"></typeparam>
/// <typeparam name="T4"></typeparam>
public interface AVerb<in T1, in T2, in T3, in T4> : IVerb
{
    Delegate IVerb.Delegate => Run;

    /// <summary>
    /// Runs the verb
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <param name="d"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<int> Run(T1 a, T2 b, T3 c, T4 d, CancellationToken token);
}

