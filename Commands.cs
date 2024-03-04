using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace STCustomModels
{
    public sealed partial class STCustomModels : BasePlugin
    {

        [ConsoleCommand("css_setmodel", "sets your model by index from cfg")]
        [CommandHelper(minArgs: 1, usage: "!setmodel [index]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void SetModel(CCSPlayerController? player, CommandInfo command)
        {
            _ = SetModel(player, command.GetArg(1), player.SteamID.ToString());
        }

        [ConsoleCommand("css_models", "lists all models")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void ListModels(CCSPlayerController? player, CommandInfo command)
        {
            _ = ListModels(player);
        }    
    }

}