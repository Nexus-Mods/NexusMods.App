using NexusMods.CLI.DataOutputs;
using NexusMods.Common.UserInput;

namespace NexusMods.CLI;

/// <summary>
/// Implementation of an option selector for the CLI.
/// </summary>
public class CliOptionSelector : IOptionSelector
{
    private const string ReturnInput = "x";
    private static readonly string[] TableOfOptionsHeaders = { "Key", "State", "Name", "Description" };
    private static readonly object[] TableOfOptionsFooter = { ReturnInput, "", "Back", "" };
    private static readonly string[] TableOfGroupsHeaders = { "Key", "Group" };
    private static readonly object[] TableOfGroupsFooter = { ReturnInput, "Continue" };

    private readonly IRenderer _renderer;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="configurator"></param>
    public CliOptionSelector(Configurator configurator)
    {
        _renderer = configurator.Renderer;
    }

    /// <summary>
    /// Request a choice from the user.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="type"></param>
    /// <param name="options"></param>
    /// <typeparam name="TOptionId"></typeparam>
    /// <returns></returns>
    public Task<IEnumerable<TOptionId>> RequestChoice<TOptionId>(string query, ChoiceType type, IEnumerable<Option<TOptionId>> options)
    {
        var done = false;
        var current = options.ToList();
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

                _renderer.Render(TableOfOptions(current, query));
            }
        }
        return Task.FromResult(current.Where(_ => _.Type is OptionState.Selected or OptionState.Required).Select(_ => _.Id));
    }

    /// <inheritdoc />
    public Task<Tuple<TGroupId, IEnumerable<TOptionId>>?> RequestMultipleChoices<TGroupId, TOptionId>(IEnumerable<ChoiceGroup<TGroupId, TOptionId>> groups)
    {
        var selectedGroupIdx = -1;
        Tuple<TGroupId, IEnumerable<TOptionId>>? result = null;
        IList<Option<TOptionId>>? selectedGroup = null;
        string selectedGroupName = "";
        var groupsArr = groups.ToArray();

        while (true)
        {
            RenderOptions(groupsArr, selectedGroup, selectedGroupName);
            var input = GetUserInput();

            if (selectedGroupIdx < 0)
            {
                selectedGroupIdx = ParseNumericalUserInput(input, groupsArr.Length) ?? selectedGroupIdx;
                if (selectedGroupIdx >= 0)
                {
                    var group = groupsArr.ElementAt(selectedGroupIdx);
                    selectedGroup = group.Options.ToList();
                    selectedGroupName = group.Query;
                }
            }
            else
            {
                if (input == ReturnInput)
                    result = CreateResult(groupsArr, selectedGroupIdx, selectedGroup!);
                else
                    UpdatedSelectedGroup(ref selectedGroup, groupsArr, selectedGroupIdx, input);
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

    private static Tuple<TGroupId, IEnumerable<TOptionId>> CreateResult<TGroupId, TOptionId>(IEnumerable<ChoiceGroup<TGroupId, TOptionId>> groups,
                                                                                             int selectedGroupIdx,
                                                                                             IEnumerable<Option<TOptionId>> selectedGroup)
    {
        var updatedOptions = selectedGroup
                                .Where(_ => _.Type is OptionState.Selected or OptionState.Required)
                                .Select(_ => _.Id);

        return new Tuple<TGroupId, IEnumerable<TOptionId>>(groups.ElementAt(selectedGroupIdx).Id, updatedOptions);
    }

    private static void FixSelection<TOptionId>(ref IList<Option<TOptionId>> list, ChoiceType groupType, int lastChangeIdx)
    {
        switch (groupType)
        {
            case ChoiceType.ExactlyOne or ChoiceType.AtMostOne
            when (list[lastChangeIdx].Type == OptionState.Selected):
                DeselectAllBut(ref list, lastChangeIdx);
                return;

            case ChoiceType.ExactlyOne or ChoiceType.AtLeastOne
            when list.All(_ => _.Type != OptionState.Selected):
                list[lastChangeIdx].Type = OptionState.Selected;
                break;
        }
    }

    private static void DeselectAllBut<TOptionId>(ref IList<Option<TOptionId>> list, int lastChangeIdx)
    {
        for (var i = 0; i < list.Count; ++i)
        {
            if ((i != lastChangeIdx) && (list[lastChangeIdx].Type == OptionState.Selected))
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
            if ((idx >= 0) && (idx < upperLimit))
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
        _renderer.Render(selectedGroup == null
            ? TableOfGroups(groups)
            : TableOfOptions(selectedGroup, selectedGroupName));
    }

    private Table TableOfOptions<TOptionId>(
        IEnumerable<Option<TOptionId>> current, string selectedGroupName)
    {
        var row = new List<object[]>();
        var key = 1;
        foreach (var item in current)
            row.Add(new object[] { key++, RenderOptionState(item.Type), item.Name, item.Description ?? "" });

        row.Add(TableOfOptionsFooter);
        return new Table(TableOfOptionsHeaders, row, selectedGroupName);
    }

    private static Table TableOfGroups<TGroupId, TOptionId>(IEnumerable<ChoiceGroup<TGroupId, TOptionId>> groups)
    {
        var row = new List<object[]>();
        var key = 1;
        foreach (var item in groups)
            row.Add(new object[] { key++, item.Query });

        row.Add(TableOfGroupsFooter);
        return new Table(TableOfGroupsHeaders, row, "Select a Group");
    }

    private static OptionState ToggleState(OptionState old)
    {
        return old switch
        {
            OptionState.Available => OptionState.Selected,
            OptionState.Selected => OptionState.Available,
            _ => old
        };
    }

    private static string RenderOptionState(OptionState state)
    {
        return state switch
        {
            OptionState.Disabled => "Disabled",
            OptionState.Available => "Off",
            OptionState.Required => "Required",
            OptionState.Selected => "On",
            _ => throw new NotImplementedException()
        };
    }
}

