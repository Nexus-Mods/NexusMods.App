﻿using JetBrains.Annotations;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;

namespace NexusMods.Games.FOMOD;

[UsedImplicitly]
public class TypeFinder : ITypeFinder
{
    public IEnumerable<Type> DescendentsOf(Type type)
    {
        return AllTypes.Where(t => t.IsAssignableTo(type));
    }

    private IEnumerable<Type> AllTypes => new[]
    {
        typeof(FomodAnalyzerInfo)
    };
}
