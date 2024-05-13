using System;
using System.Collections.Generic;
using System.CommandLine.Invocation;
using System.Reflection;
using System.Threading.Tasks;

namespace NexusMods.SingleProcess;

/// <summary>
/// Command handler for linking a verb definition to the parser
/// </summary>
/// <param name="getters"></param>
/// <param name="methodInfo"></param>
internal class CommandHandler(List<Func<InvocationContext, object?>> getters, MethodInfo methodInfo)
    : ICommandHandler
{
    public int Invoke(InvocationContext context)
    {
        throw new NotSupportedException("Only async is supported");
    }

    public Task<int> InvokeAsync(InvocationContext context)
    {
        // Resolve all the parameters
        var args = GC.AllocateUninitializedArray<object?>(getters.Count);
        for (var i = 0; i < getters.Count; i++)
        {
            args[i] = getters[i](context);
        }
        // Invoke the method
        return (Task<int>)methodInfo.Invoke(null, args)!;
    }
}
