using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vogen;

namespace NexusMods.Common;


[ValueObject<string>]
public partial class AssetUrl
{
}

public enum OptionState
{
    /// <summary>
    /// not selected but could be
    /// </summary>
    Available,
    /// <summary>
    /// selected, could be deselected
    /// </summary>
    Selected,
    /// <summary>
    /// not selected and can't be
    /// </summary>
    Disabled,
    /// <summary>
    /// selected and can't be deselected
    /// </summary>
    Required,
}

public record Option<IdT>
{
    public required IdT Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public AssetUrl? ImageUrl { get; init; }
    public string? HoverText { get; init; }
    public OptionState Type { get; set; } = OptionState.Available;
}

public enum ChoiceType
{
    ExactlyOne,
    AtMostOne,
    AtLeastOne,
    Any,

}

public record ChoiceGroup<IdT, OptionIdT>
{
    public required IdT Id { get; init; }
    public required string Query { get; init; }
    public required ChoiceType Type { get; init; }
    public required IEnumerable<Option<OptionIdT>> Options { get; init; }
}

public interface IUserInput
{
    public Task<IEnumerable<OptionIdT>> RequestChoice<OptionIdT>(string query, ChoiceType type, IEnumerable<Option<OptionIdT>> options);
    public Task<Tuple<GroupIdT, IEnumerable<OptionIdT>>?> RequestMultipleChoices<GroupIdT, OptionIdT>(IEnumerable<ChoiceGroup<GroupIdT, OptionIdT>> choices);
}
