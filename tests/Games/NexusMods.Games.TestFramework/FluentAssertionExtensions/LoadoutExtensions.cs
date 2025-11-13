using FluentAssertions;
using FluentAssertions.Collections;
using FluentAssertions.Execution;

using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.Sdk.Games;

namespace NexusMods.Games.TestFramework.FluentAssertionExtensions;

public static class LoadoutExtensions
{
    public static LoadoutItemCollectionAssertions Should(this Entities<LoadoutItem.ReadOnly> items)
    {
        return new LoadoutItemCollectionAssertions(items);
    }
}

public class LoadoutItemCollectionAssertions : GenericCollectionAssertions<LoadoutItem.ReadOnly>
{
    public LoadoutItemCollectionAssertions(Entities<LoadoutItem.ReadOnly> items) : base(items)
    {
    }
    
    protected override string Identifier => "LoadoutItem.ReadOnly[]";
    
    [CustomAssertion]
    public AndConstraint<LoadoutItemCollectionAssertions> ContainItemTargetingPath(GamePath path, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.Any(item =>
            {
                if (!item.TryGetAsLoadoutItemWithTargetPath(out var targetedPath))
                    return false;
                        
                return targetedPath.TargetPath == path;
                }))
            .FailWith("Expected {context:LoadoutItem.ReadOnly[]} to contain an item targeting path {0}{reason}, but it did not.", path);

        return new AndConstraint<LoadoutItemCollectionAssertions>(this);
    }
}
