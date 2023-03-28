using NexusMods.DataModel.Abstractions.Ids;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public interface IModNameViewModel : IColumnViewModel<IId>
{
    public string Name { get; }
}
