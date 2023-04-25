using System.Collections;

namespace NexusMods.UI.Tests.Framework;

public class EnumData<T> : IEnumerable<object[]> where T : struct, Enum
{
    public IEnumerator<object[]> GetEnumerator()
    {
        foreach (var value in Enum.GetValues<T>())
            yield return new object[] { value };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
