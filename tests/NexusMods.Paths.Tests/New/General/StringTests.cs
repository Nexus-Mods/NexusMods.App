using NexusMods.Paths.Extensions;
using NexusMods.Paths.Tests.New.Helpers;

namespace NexusMods.Paths.Tests.New.General;

public class StringTests
{
    /// <summary>
    /// Test for hashing long strings.
    /// This is just a baseline quick test.
    /// </summary>
    [Fact]
    public void HashRandomString()
    {
        for (var x = 0; x < 257; x++)
        {
            var text = TestStringHelpers.RandomStringUpperWithEmoji(x);
            var expected = text.AsSpan().GetNonRandomizedHashCode();

            for (var y = 0; y < 10; y++)
                Assert.Equal(expected, text.AsSpan().GetNonRandomizedHashCode());
        }
    }
}
