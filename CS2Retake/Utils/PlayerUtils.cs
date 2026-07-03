using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS2Retake.Utils
{
    public static class PlayerUtils
    {
        public static bool IsPlayableHuman(CCSPlayerController? player)
        {
            return player != null
                && player.IsValid
                && !player.IsBot
                && !player.IsHLTV
                && player.UserId.HasValue
                && player.PlayerPawn != null
                && player.PlayerPawn.IsValid
                && player.PlayerPawn.Value != null
                && player.PlayerPawn.Value.IsValid;
        }

        public static List<CCSPlayerController> GetPlayerControllersOfTeam(CsTeam team)
        {
            var playerList = Utilities.GetPlayers();

            //Valid human players only
            playerList = playerList.FindAll(IsPlayableHuman);

            //Team specific players
            playerList = playerList.FindAll(x => x.TeamNum == (int)team);

            return playerList ?? new List<CCSPlayerController>();
        }

        public static List<CCSPlayerController> GetCounterTerroristPlayers() => GetPlayerControllersOfTeam(CsTeam.CounterTerrorist);
        public static List<CCSPlayerController> GetTerroristPlayers() => GetPlayerControllersOfTeam(CsTeam.Terrorist);

        public static List<CCSPlayerController> GetValidPlayerControllers() => Utilities.GetPlayers().Where(IsPlayableHuman).ToList();

        public static bool AreMoreThenPlayersConnected(int playerCount) => GetValidPlayerControllers().Count() >= playerCount;

        public static bool AreMoreThenOrEqualPlayersConnected(int playerCount) => GetValidPlayerControllers().Count() >= playerCount;

        public static void SuicideAll() => GetValidPlayerControllers().ForEach(x => x.CommitSuicide(true, true));
    }
}
