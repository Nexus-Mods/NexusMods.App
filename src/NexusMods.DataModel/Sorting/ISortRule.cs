using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Abstractions;

namespace NexusMods.DataModel.Sorting;

/// <summary>
/// A marker interface for rules used in sorting
/// </summary>
/// <typeparam name="TType"></typeparam>
/// <typeparam name="TId"></typeparam>
public interface ISortRule<TType, TId> 
where TType : IHasEntityId<TId>
{
    
}
