using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Controls.Spine;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.Games.RedEngine;
using Type = NexusMods.App.UI.Controls.Spine.Type;

namespace NexusMods.UI.Tests;


[Trait("RequiresNetworking", "True")]
public class SpineTests : AUiTest
{
    private readonly IGame _game;

    public SpineTests(IServiceProvider provider) : base(provider)
    {
        _game = provider.GetRequiredService<Cyberpunk2077>();
    }

    [Fact]
    public async Task ActivatingButtonsDeactivatesOtherButtons()
    {
        void ValidateButtons(ISpineViewModel vm, SpineButtonAction action)
        {
            if (action.Type == Type.Game)
            {
                vm.Add.IsActive.Should().BeFalse("game button was clicked");
                vm.Home.IsActive.Should().BeFalse("game button was clicked");
                foreach (var game in vm.Games)
                {
                    if (ReferenceEquals(game.Tag, action.Game))
                        game.IsActive.Should().BeTrue("button for this game was clicked");
                    else
                        game.IsActive.Should().BeFalse("button for other game was clicked");
                }
            }
            else if (action.Type == Type.Add)
            {
                vm.Add.IsActive.Should().BeTrue("add button was clicked");
                vm.Home.IsActive.Should().BeFalse("add button was clicked");
                foreach (var game in vm.Games)
                {
                    game.IsActive.Should().BeFalse("add button was clicked");
                }
            }
            else if (action.Type == Type.Home)
            {
                vm.Add.IsActive.Should().BeFalse("home button was clicked");
                vm.Home.IsActive.Should().BeTrue("home button was clicked");
                foreach (var game in vm.Games)
                {
                    game.IsActive.Should().BeFalse("home button was clicked");
                }
            }
            else
            {
                throw new Exception($"Unknown action type {action}");
            }

        }

        using var vm = GetActivatedViewModel<ISpineViewModel>();
        var loadout = await LoadoutRegistry.Manage(_game.Installations.First(), "Cyberpunk 2077");

        using var _ = vm.VM.Actions.Subscribe(vm.VM.Activations);

        await Task.Delay(1000);

        vm.VM.Games.Select(g => g.Name).Should().Contain("Cyberpunk 2077");

        vm.VM.Games.First().Click.CanExecute(null).Should().BeTrue();
        vm.VM.Games.First().Click.Execute(null);
        vm.VM.Games.First().IsActive.Should().BeTrue("Game button was clicked");

        ValidateButtons(vm.VM, new SpineButtonAction(Type.Game, (IGame)vm.VM.Games.First().Tag!));

        vm.VM.Home.Click.CanExecute(null).Should().BeTrue();
        vm.VM.Home.Click.Execute(null);
        ValidateButtons(vm.VM, new SpineButtonAction(Type.Home));

        vm.VM.Add.Click.CanExecute(null).Should().BeTrue();
        vm.VM.Add.Click.Execute(null);
        ValidateButtons(vm.VM, new SpineButtonAction(Type.Add));
    }

}
