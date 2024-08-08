using FluentAssertions;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.UI.Tests;

public class LoadoutNameProviderTests
{
    [Fact]
    public async Task TestNames()
    {
        const int numChars = 'Z' - 'A' + 1;
        const int count = numChars * numChars + numChars;
        var names = new string[count];
        Array.Fill(names, string.Empty);

        for (var i = 0; i < count; i++)
        {
            names[i] = LoadoutNameProvider.GetNewShortName(names.AsSpan(start: 0, length: i));
        }

        var result = names.Aggregate((a, b) => $"{a}\n{b}");
        await Verify(result);
    }
}

