using System.Diagnostics;
using JetBrains.Annotations;
using NexusMods.Abstractions.CLI;
using NexusMods.Abstractions.CLI.DataOutputs;
using NexusMods.Common.UserInput;

namespace NexusMods.CLI;

/// <summary>
/// Implementation of an option selector for the CLI.
/// </summary>
[UsedImplicitly]
public class CliOptionSelector : IOptionSelector
{
    private const string ReturnInput = "x";
    private static readonly string[] TableOfOptionsHeaders = { "Key", "State", "Name", "Description" };
    private static readonly object[] TableOfOptionsFooter = { ReturnInput, "", "Back", "" };
    private static readonly string[] TableOfGroupsHeaders = { "Key", "Group" };
    private static readonly object[] TableOfGroupsFooter = { ReturnInput, "Continue" };

    /// <summary>
    /// The renderer to use for rendering the options.
    /// </summary>
    private IRenderer Renderer { get; set; } = null!;

    /// <summary>
    /// If true, all option selection will throw an error, useful for automated installers and tests
    /// </summary>
    public bool AutoFail { get; set; }

    /// <summary>
    /// Request a choice from the user.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="type"></param>
    /// <param name="options"></param>
    /// <typeparam name="TOptionId"></typeparam>
    /// <returns></returns>
    public Task<TOptionId[]> RequestChoice<TOptionId>(string query, ChoiceType type, Option<TOptionId>[] options)
    {
        var done = false;
        var current = options;
        while (!done)
        {
            var input = GetUserInput();
            if (input == ReturnInput)
            {
                done = true;
            }
            else
            {
                var idx = ParseNumericalUserInput(input, current.Count());
                if (idx != null)
                    current[idx.Value].Type = ToggleState(current[idx.Value].Type);

                Renderer.Render(TableOfOptions(current, query));
            }
        }

        var res = current
            .Where(x => x.Type is OptionState.Selected or OptionState.Required)
            .Select(x => x.Id)
            .ToArray();

        return Task.FromResult(res);
    }

    /// <inheritdoc />
    public Task<Tuple<TGroupId, TOptionId[]>?> RequestMultipleChoices<TGroupId, TOptionId>(ChoiceGroup<TGroupId, TOptionId>[] groups)
    {
        if (AutoFail) throw new Exception("AutoFail is enabled for this option selector.");

        var selectedGroupIdx = -1;
        Tuple<TGroupId, TOptionId[]>? result = null;
        IList<Option<TOptionId>>? selectedGroup = null;
        var selectedGroupName = "";

        while (true)
        {
            RenderOptions(groups, selectedGroup, selectedGroupName);
            var input = GetUserInput();

            if (selectedGroupIdx < 0)
            {
                selectedGroupIdx = ParseNumericalUserInput(input, groups.Length) ?? selectedGroupIdx;
                if (selectedGroupIdx >= 0)
                {
                    var group = groups.ElementAt(selectedGroupIdx);
                    selectedGroup = group.Options;
                    selectedGroupName = group.Query;
                }
            }
            else
            {
                if (input == ReturnInput)
                    result = CreateResult(groups, selectedGroupIdx, selectedGroup!);
                else
                    UpdatedSelectedGroup(ref selectedGroup, groups, selectedGroupIdx, input);
            }

            if (input == ReturnInput)
                break;
        }

        return Task.FromResult(result);
    }

    private static void UpdatedSelectedGroup<TGroupId, TOptionId>(ref IList<Option<TOptionId>>? selectedGroup,
                                                           IEnumerable<ChoiceGroup<TGroupId, TOptionId>> groups,
                                                           int selectedGroupIdx,
                                                           string input)
    {
        var idx = ParseNumericalUserInput(input, selectedGroup!.Count) ?? -1;
        if (idx < 0)
            return;

        var oldState = selectedGroup[idx].Type;
        selectedGroup[idx].Type = ToggleState(selectedGroup[idx].Type);
        if (oldState == selectedGroup[idx].Type)
            return;

        var groupType = groups.ElementAt(selectedGroupIdx).Type;
        FixSelection(ref selectedGroup, groupType, idx);
    }

    private static string GetUserInput()
    {
        return (Console.ReadLine() ?? "").Trim();
    }

    private static Tuple<TGroupId, TOptionId[]> CreateResult<TGroupId, TOptionId>(
        IEnumerable<ChoiceGroup<TGroupId, TOptionId>> groups,
        int selectedGroupIdx,
        IEnumerable<Option<TOptionId>> selectedGroup)
    {
        var updatedOptions = selectedGroup
            .Where(x => x.Type is OptionState.Selected or OptionState.Required)
            .Select(x => x.Id);

        return new Tuple<TGroupId, TOptionId[]>(groups.ElementAt(selectedGroupIdx).Id, updatedOptions.ToArray());
    }

    private static void FixSelection<TOptionId>(ref IList<Option<TOptionId>> list, ChoiceType groupType, int lastChangeIdx)
    {
        switch (groupType)
        {
            case ChoiceType.ExactlyOne or ChoiceType.AtMostOne
            when list[lastChangeIdx].Type == OptionState.Selected:
                DeselectAllBut(ref list, lastChangeIdx);
                return;

            case ChoiceType.ExactlyOne or ChoiceType.AtLeastOne
            when list.All(x => x.Type != OptionState.Selected):
                list[lastChangeIdx].Type = OptionState.Selected;
                break;
        }
    }

    private static void DeselectAllBut<TOptionId>(ref IList<Option<TOptionId>> list, int lastChangeIdx)
    {
        for (var i = 0; i < list.Count; ++i)
        {
            if (i != lastChangeIdx && list[lastChangeIdx].Type == OptionState.Selected)
            {
                list[i].Type = OptionState.Available;
            }
        }
    }

    private static int? ParseNumericalUserInput(string input, int upperLimit)
    {
        try
        {
            var idx = int.Parse(input) - 1;
            if (idx >= 0 && idx < upperLimit)
                return idx;
        }
        catch (FormatException) { /* ignored */ }

        // input invalid or out of range
        return null;
    }

    private void RenderOptions<TGroupId, TOptionId>(
        IEnumerable<ChoiceGroup<TGroupId, TOptionId>> groups,
        IList<Option<TOptionId>>? selectedGroup, string selectedGroupName)
    {
        Renderer.Render(selectedGroup == null
            ? TableOfGroups(groups)
            : TableOfOptions(selectedGroup, selectedGroupName));
    }

    private Table TableOfOptions<TOptionId>(
        IEnumerable<Option<TOptionId>> current, string selectedGroupName)
    {
        var key = 1;
        var row = current
            .Select(item => new object[] { key++, RenderOptionState(item.Type), item.Name, item.Description ?? "" })
            .ToList();

        row.Add(TableOfOptionsFooter);
        return new Table(TableOfOptionsHeaders, row, selectedGroupName);
    }

    private static Table TableOfGroups<TGroupId, TOptionId>(IEnumerable<ChoiceGroup<TGroupId, TOptionId>> groups)
    {
        var key = 1;
        var row = groups
            .Select(item => new object[] { key++, item.Query })
            .ToList();

        row.Add(TableOfGroupsFooter);
        return new Table(TableOfGroupsHeaders, row, "Select a Group");
    }

    private static OptionState ToggleState(OptionState old)
    {
        Debug.Assert(Enum.IsDefined(typeof(OptionState), old));

        return old switch
        {
            OptionState.Available => OptionState.Selected,
            OptionState.Selected => OptionState.Available,
            _ => old
        };
    }

    private static string RenderOptionState(OptionState state)
    {
        Debug.Assert(Enum.IsDefined(typeof(OptionState), state));

        return state switch
        {
            OptionState.Disabled => "Disabled",
            OptionState.Available => "Off",
            OptionState.Required => "Required",
            OptionState.Selected => "On",
            _ => throw new UnreachableException()
        };
    }
}

