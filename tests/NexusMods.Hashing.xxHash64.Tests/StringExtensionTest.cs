using FluentAssertions;

namespace NexusMods.Hashing.xxHash64.Tests;

public class StringExtensionTest
{
    [Fact]
    public void CanConvertReadOnlySpans()
    {
        var arr = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        ((ReadOnlySpan<byte>)arr.AsSpan()).ToHex().Should().Be("DEADBEEF");

    }
}