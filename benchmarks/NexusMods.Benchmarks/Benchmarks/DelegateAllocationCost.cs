using BenchmarkDotNet.Attributes;
using NexusMods.Benchmarks.Interfaces;

namespace NexusMods.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[BenchmarkInfo("Delegate Allocation Cost", "Compares the cost of delegates using various methods")]
public class DelegateAllocationCost : IBenchmark
{
    private readonly int _valueType;
    private readonly string _referenceType;

    public DelegateAllocationCost()
    {
        _valueType = Random.Shared.Next();
        _referenceType = Guid.NewGuid().ToString("N");
    }

    [Benchmark]
    public string NoState_ReferenceType()
    {
        return NoState(() => _referenceType);
    }

    [Benchmark]
    public int NoState_ValueType()
    {
        return NoState(() => _valueType * 2);
    }

    [Benchmark]
    public string WithRefState_ReferenceType()
    {
        return WithRefState((ref string state) => state, _referenceType);
    }

    [Benchmark]
    public int WithRefState_ValueType()
    {
        return WithRefState((ref int state) => state * 2, _valueType);
    }

    private static TOut NoState<TOut>(Func<TOut> act)
    {
        return act();
    }

    private static TOut WithRefState<TState, TOut>(FuncRef<TState, TOut> act, TState state)
    {
        return act(ref state);
    }

    private delegate TOut FuncRef<TState, out TOut>(ref TState state);
}
