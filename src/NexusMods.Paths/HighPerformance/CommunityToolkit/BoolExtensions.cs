// Adapted from Microsoft Community Toolkit which is licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace NexusMods.Paths.HighPerformance.CommunityToolkit;

[ExcludeFromCodeCoverage(Justification = "Code taken from external library.")]
internal static class BoolExtensions
{
    /// <summary>
    /// Converts the given <see cref="bool"/> value into a <see cref="byte"/>.
    /// </summary>
    /// <param name="flag">The input value to convert.</param>
    /// <returns>1 if <paramref name="flag"/> is <see langword="true"/>, 0 otherwise.</returns>
    /// <remarks>This method does not contain branching instructions.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe byte ToByte(this bool flag)
    {
        // Whenever we need to take the address of an argument, we make a local copy first.
        // This will be removed by the JIT anyway, but it can help produce better codegen and
        // remove unwanted stack spills if the caller is using constant arguments. This is
        // because taking the address of an argument can interfere with some of the flow
        // analysis executed by the JIT, which can in some cases block constant propagation.
        var copy = flag;

        return *(byte*)&copy;
    }
}
