using NexusMods.Paths.Extensions;

namespace NexusMods.Paths.Tests.New.Conversions;

/// <summary>
/// Test logic for our vectorised change case.
/// </summary>
public class VectorisedReplaceCharacter
{
    /// <summary>
    /// Tests strings smaller than any vector variable would store.
    /// </summary>
    [Theory]
    [InlineData(@"\", "/")]
    [InlineData(@"\\", @"/\")]
    [InlineData(@"\\\", @"/\/")]
    [InlineData(@"\\\\", @"/\/\")]
    public static void ReplaceSlash_Short_InPlace(string expected, string input)
    {
        AssertStringReplaceInPlace(expected, input, '/', '\\');
    }

    /// <summary>
    /// Tests strings smaller than any vector variable would store.
    /// </summary>
    [Theory]
    [InlineData(@"\", "/")]
    [InlineData(@"\\", @"/\")]
    [InlineData(@"\\\", @"/\/")]
    [InlineData(@"\\\\", @"/\/\")]
    public static void ReplaceSlash_Short_InAnotherBuffer(string expected, string input)
    {
        AssertStringReplaceInAnotherBuffer(expected, input, '/', '\\');
    }

    private static unsafe void AssertStringReplaceInPlace(string expected, string input, char oldChar, char newChar)
    {
        // Please don't do this in production; unless lifetime of string doesn't 
        // extend past current method, this is just for convenience
        fixed (char* inputPtr = input)
        {
            var inputSpan = new Span<char>(inputPtr, input.Length);
            inputSpan.Replace(oldChar, newChar, inputSpan); // in-place
            Assert.Equal(expected, input);
        }
    }

    private static unsafe void AssertStringReplaceInAnotherBuffer(string expected, string input, char oldChar, char newChar)
    {
        // Please don't do this in production; unless lifetime of string doesn't 
        // extend past current method, this is just for convenience
        Span<char> resultSpan = stackalloc char[input.Length];
        fixed (char* inputPtr = input)
        {
            var inputSpan = new Span<char>(inputPtr, input.Length);
            inputSpan.Replace(oldChar, newChar, resultSpan); // to another buffer
            Assert.Equal(expected, resultSpan.ToString());
        }
    }
}
