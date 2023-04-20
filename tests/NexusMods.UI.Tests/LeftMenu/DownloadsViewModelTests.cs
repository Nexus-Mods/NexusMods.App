using FluentAssertions;
using NexusMods.App.UI.LeftMenu.Downloads;
using NexusMods.App.UI.ViewModels.Helpers.ViewModelSelector;
using NexusMods.UI.Tests.Framework;

namespace NexusMods.UI.Tests.LeftMenu;

public class DownloadsViewModelTests : AVmTest<IDownloadsViewModel>
{
    public DownloadsViewModelTests(IServiceProvider provider) : base(provider)
    {
        
    }
    
    [Theory]
    [ClassData(typeof(EnumData<Options>))]
    public void DownloadsViewModel_CanSelectMenuOptions(Options current)
    {
        var mappings = AViewModelAttribute.GetAttributes<Options>();

        var cmd = Vm.CommandFor(current);
        cmd.Execute(null);
        
        foreach (var check in Enum.GetValues<Options>())
        {
            if (check == current)
            {
                Vm.Current.Should().Be(check);
                Vm.CurrentViewModel.Should().BeAssignableTo(mappings[check]);
            }
            else
            {
                Vm.Current.Should().NotBe(check);
                Vm.CurrentViewModel.Should().NotBeAssignableTo(mappings[check]);
            }
        }
        
    }
    
}
