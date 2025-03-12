using System.Net;
using System.Text;

namespace NexusMods.Telemetry;

internal static class RedactUtils
{
    private static readonly Dictionary<string, string> ValuesToRedact = new(StringComparer.OrdinalIgnoreCase);

    static RedactUtils()
    {
        Add(() => Environment.UserName, "USER_NAME");
        Add(() => Environment.UserDomainName, "USER_DOMAIN_NAME");
        Add(() => Dns.GetHostName(), "HOST_NAME");
        AddMany(() => Dns.GetHostEntry(Dns.GetHostName()).AddressList.Select(static ip => ip.ToString()), "HOST_IP_ADDRESS");

        return;
        static void Add(Func<string> func, string replacement)
        {
            try
            {
                var value = func();
                if (string.IsNullOrWhiteSpace(value)) return;
                ValuesToRedact[value] = $"<{replacement}>";
            }
            catch
            {
                // ignored
            }
        }

        static void AddMany(Func<IEnumerable<string>> func, string replacement)
        {
            try
            {
                foreach (var value in func())
                {
                    if (string.IsNullOrWhiteSpace(value)) return;
                    ValuesToRedact[value] = $"<{replacement}>";
                }
            }
            catch
            {
                // ignored
            }
        }
    }

    internal static string Redact(string input)
    {
        var sb = new StringBuilder(input);

        foreach (var kv in ValuesToRedact)
        {
            sb.Replace(kv.Key, kv.Value);
        }

        return sb.ToString();
    }
}
