using System.Diagnostics.Metrics;
using OneOf;

namespace NexusMods.Abstractions.Telemetry;
using MembershipStatusUnion = OneOf<MembershipStatus.None, MembershipStatus.Premium>;

public static class MembershipStatus
{
    public record struct None;
    public record struct Premium;
}

public static partial class Counters
{
    public delegate MembershipStatusUnion GetMembershipDelegate();

    /// <summary>
    /// Creates a counter for the number of active users per operating system.
    /// </summary>
    public static void CreateUsersPerMembershipCounter(
        this IMeterConfig meterConfig,
        GetMembershipDelegate getMembershipFunc)
    {
        GetMeter(meterConfig).CreateObservableUpDownCounter(
            name: InstrumentConstants.NameUsersPerMembership,
            observeValue: () => ObserveMembership(getMembershipFunc)
        );
    }

    private static Measurement<int> ObserveMembership(GetMembershipDelegate getMembershipFunc)
    {
        var membership = getMembershipFunc();
        var membershipString = membership.Match(
            f0: _ => "none",
            f1: _ => "premium"
        );

        return new Measurement<int>(
            value: 1,
            tags: new KeyValuePair<string, object?>(InstrumentConstants.TagMembership, membershipString)
        );
    }
}
