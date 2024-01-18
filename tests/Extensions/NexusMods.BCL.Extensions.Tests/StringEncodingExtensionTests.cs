using FluentAssertions;
using Xunit;

namespace NexusMods.BCL.Extensions.Tests;

public class StringEncodingExtensionTests
{
    [Theory]
    [InlineData("foobar", "Zm9vYmFy")]
    public void ConvertsStringToBase64(string input, string expected)
    {
        input.ToBase64().Should().Be(expected);
    }
}
