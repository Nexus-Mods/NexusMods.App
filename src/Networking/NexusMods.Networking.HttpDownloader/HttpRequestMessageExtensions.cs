namespace NexusMods.Networking.HttpDownloader;

public static class HttpRequestMessageExtensions
{
    /// <summary>
    /// Copies the request message so that it can be sent again.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static HttpRequestMessage Copy(this HttpRequestMessage input)
    {
        var newRequest = new HttpRequestMessage(input.Method, input.RequestUri);
        foreach (var option in input.Options)
        {
            newRequest.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }

        foreach (var header in input.Headers)
        {
            newRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return newRequest;
    }
}
