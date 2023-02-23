using FluentAssertions;

namespace NexusMods.Common.Tests;

public class StringEncodingExtensionTests
{
    [Theory()]
    [InlineData("foobar", "Zm9vYmFy")]
    public void ConvertsStringToBase64(string input, string expected)
    {
        input.ToBase64().Should().Be(expected);
    }
}