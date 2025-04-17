using System.Text;
using FluentAssertions;
using NexusMods.Abstractions.GameLocators;

namespace NexusMods.StandardGameLocators.Tests;

public class WineParserTests
{
    [Theory]
    [MemberData(nameof(TestData_ParseEnvironmentVariable))]
    public void Test_ParseEnvironmentVariable(string input, List<WineDllOverride> expected)
    {
        var actual = WineParser.ParseEnvironmentVariable(input);
        actual.Should().HaveSameCount(expected);

        for (var i = 0; i < actual.Count; i++)
        {
            var a = actual[i];
            var b = expected[i];

            a.DllName.Should().Be(b.DllName);
            a.OverrideTypes.Should().Equal(b.OverrideTypes);
            a.IsDisabled.Should().Be(b.IsDisabled);
        }
    }

    [Theory]
    [MemberData(nameof(TestData_ParseWinetricksLogFile))]
    public void Test_ParseWinetricksLogFile(string fileContents, HashSet<string> expected)
    {
        var bytes = Encoding.UTF8.GetBytes(fileContents);
        using var ms = new MemoryStream(bytes, writable: false);

        var result = WineParser.ParseWinetricksLogFile(ms);
        result.Should().Equal(expected);
    }

    [Theory]
    [MemberData(nameof(TestData_ToString))]
    public void Test_ToString(WineDllOverride input, string expected)
    {
        var actual = input.ToString();
        actual.Should().Be(expected);
    }

    public static TheoryData<string, HashSet<string>> TestData_ParseWinetricksLogFile()
    {
        return new TheoryData<string, HashSet<string>>
        {
            {
                "d3dcompiler_47\nvcrun2022",
                ["d3dcompiler_47", "vcrun2022"]
            },
        };
    }
    
    public static TheoryData<WineDllOverride, string> TestData_ToString()
    {
        return new TheoryData<WineDllOverride, string>
        {
            {
                new WineDllOverride("comdlg32", [WineDllOverrideType.Native, WineDllOverrideType.BuiltIn]),
                "comdlg32=n,b"
            },
            {
                new WineDllOverride("shell32", [WineDllOverrideType.BuiltIn]),
                "shell32=b"
            },
            {
                new WineDllOverride("comctl32", [WineDllOverrideType.Native]),
                "comctl32=n"
            },
            {
                new WineDllOverride("oleaut32", [WineDllOverrideType.Disabled]),
                "oleaut32="
            },
        };
    }

    public static TheoryData<string, List<WineDllOverride>> TestData_ParseEnvironmentVariable()
    {
        return new TheoryData<string, List<WineDllOverride>>
        {
            {
                "WINEDLLOVERRIDES=\"comdlg32,shell32=n,b\" wine program_name",
                [
                    new WineDllOverride("comdlg32", [WineDllOverrideType.Native, WineDllOverrideType.BuiltIn]),
                    new WineDllOverride("shell32", [WineDllOverrideType.Native, WineDllOverrideType.BuiltIn]),
                ]
            },
            {
                "WINEDLLOVERRIDES=\"comdlg32,shell32=n\" wine program_name",
                [
                    new WineDllOverride("comdlg32", [WineDllOverrideType.Native]),
                    new WineDllOverride("shell32", [WineDllOverrideType.Native]),
                ]
            },
            {
                "WINEDLLOVERRIDES=\"comdlg32=b,n;shell32=b;comctl32=n;oleaut32=\" wine program_name",
                [
                    new WineDllOverride("comdlg32", [WineDllOverrideType.BuiltIn, WineDllOverrideType.Native]),
                    new WineDllOverride("shell32", [WineDllOverrideType.BuiltIn]),
                    new WineDllOverride("comctl32", [WineDllOverrideType.Native]),
                    new WineDllOverride("oleaut32", [WineDllOverrideType.Disabled]),
                ]
            },
        };
    }
}
