using CounterStrikeSharp.API.Core;
using CS2Retake.Allocators.Implementations.CommandAllocator.Configs;
using CS2Retake.Configs;
using CS2Retake.Managers;
using CSZoneNet.Plugin.CS2BaseAllocator.Configs.Interfaces;
using CSZoneNet.Plugin.CS2BaseAllocator;
using CSZoneNet.Plugin.Utils.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CS2Retake.Utils;
using Microsoft.Extensions.Logging;
using CS2Retake.Allocators.Implementations.CommandAllocator.Menus;
using CS2Retake.Allocators.Implementations.CommandAllocator.Manager;
using CounterStrikeSharp.API.Modules.Utils;
using CS2Retake.Allocators.Implementations.CommandAllocator.Entities;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Timers;

namespace CS2Retake.Allocators.Implementations.CommandAllocator
{
    //NOTE: the native buy menu integration (buy command interception, item_pickup capture, numeric
    //payload resolvers) has been removed on 2026-07-03. CS2Retake.OnCommandBuy() blocks every "buy"
    //command and weapon selection goes through !guns only. See docs/MODIFICATIONS.md (2026-03-21
    //hotfix and 2026-07-03 cleanup) before reintroducing any native buy path.
    public class CommandAllocator : BaseGrenadeAllocator, IAllocatorConfig<CommandAllocatorConfig>, IDisposable
    {
        private static CommandAllocator? _instance = null;

        public static CommandAllocator? Instance => _instance;

        public CommandAllocatorConfig Config { get; set; } = new CommandAllocatorConfig();

        private ChanceEntity _awpChanceCT { get; set; } = null!;
        private ChanceEntity _awpChanceT { get; set; } = null!;

        private int _awpInUseCountCT { get; set; } = 0;
        private int _awpInUseCountT { get; set; } = 0;

        private CounterStrikeSharp.API.Modules.Timers.Timer? _howToTimer { get; set; } = null;
        private HashSet<ulong> _awpRecipients = new HashSet<ulong>();
        private bool _awpRecipientsInitialized = false;
        private HashSet<ulong> _defuseKitRecipients = new HashSet<ulong>();
        private bool _defuseKitRecipientsInitialized = false;

        public CommandAllocator()
        {
            _instance = this;
        }

        public override (string primaryWeapon, string secondaryWeapon, KevlarEnum kevlar, bool kit, bool zeus, List<GrenadeEnum> grenades) Allocate(CCSPlayerController player, RoundTypeEnum roundType = RoundTypeEnum.Undefined)
        {
            (string primaryWeapon, string secondaryWeapon, KevlarEnum kevlar, bool kit, bool zeus, List<GrenadeEnum> grenades) returnValue = ("", "weapon_deagle", KevlarEnum.KevlarHelmet, true, false, new List<GrenadeEnum>());

            if (player == null || !player.IsValid || player.PlayerPawn == null || !player.PlayerPawn.IsValid || player.PlayerPawn.Value == null || !player.PlayerPawn.Value!.IsValid)
            {
                return returnValue;
            }

            var random = new Random();

            //GRENADES

            var grenades = this.AllocateGrenades(player, roundType);

            if (grenades == null)
            {
                return returnValue;
            }

            returnValue.grenades = grenades;

            //RIFLES AND PISTOLS

            (string? primary, string? secondary, int? awpChance) weapons = ("", "", null);
            (string primary, string secondary, int? awpChance) defaultWeapons = ("", "weapon_deagle", 0);
            switch (roundType)
            {
                case RoundTypeEnum.FullBuy:
                    weapons = CacheManager.Instance.GetFullBuyWeapons(player);
                    defaultWeapons.primary = player.Team == CsTeam.CounterTerrorist ? "weapon_m4a1" : "weapon_ak47";

                    if(this.ShouldReceiveAwp(player, weapons.awpChance))
                    {
                        weapons.primary = "weapon_awp";
                    }

                    break;
                case RoundTypeEnum.Mid:
                    weapons = CacheManager.Instance.GetMidWeapons(player);
                    defaultWeapons.primary = player.Team == CsTeam.CounterTerrorist ? "weapon_mp9" : "weapon_mac10";
                    break;
                case RoundTypeEnum.Pistol:
                    weapons = CacheManager.Instance.GetPistolWeapons(player);
                    defaultWeapons.secondary = player.Team == CsTeam.CounterTerrorist ? "weapon_usp_silencer" : "weapon_glock";
                    returnValue.kevlar = KevlarEnum.Kevlar;
                    break;

            }

            MessageUtils.LogDebug($"SteamID: {player.SteamID} - Default: {defaultWeapons.primary}, {defaultWeapons.secondary}, {defaultWeapons.awpChance} - Selected: {weapons.primary}, {weapons.secondary}, {weapons.awpChance}");

            if (string.IsNullOrWhiteSpace(weapons.primary))
            {
                returnValue.primaryWeapon = defaultWeapons.primary;
            }
            else
            {
                returnValue.primaryWeapon = weapons.primary;
            }
            if (string.IsNullOrWhiteSpace(weapons.secondary))
            {
                returnValue.secondaryWeapon = defaultWeapons.secondary;
            }
            else
            {
                returnValue.secondaryWeapon = weapons.secondary;
            }


            //ZEUS
            if (this.Config.EnableZeus)
            {
                returnValue.zeus = random.Next(1,100) <= this.Config.ZeusChance;
            }


            //DEFUSEKIT
            returnValue.kit = this.ShouldGiveDefuseKit(player, roundType);



            return returnValue;
        }

        private bool ShouldReceiveAwp(CCSPlayerController player, int? awpChance)
        {
            if (player.Team != CsTeam.CounterTerrorist && player.Team != CsTeam.Terrorist)
            {
                return false;
            }

            if (awpChance.GetValueOrDefault() <= 0)
            {
                return false;
            }

            if (!this._awpRecipientsInitialized)
            {
                this.InitializeAwpRecipients();
            }

            return this._awpRecipients.Contains(player.SteamID);
        }

        private void InitializeAwpRecipients()
        {
            this._awpRecipientsInitialized = true;
            this._awpRecipients.Clear();

            var activePlayers = PlayerUtils.GetCounterTerroristPlayers().Count + PlayerUtils.GetTerroristPlayers().Count;

            if (activePlayers <= 4)
            {
                return;
            }

            this.InitializeAwpRecipientsForTeam(CsTeam.CounterTerrorist);
            this.InitializeAwpRecipientsForTeam(CsTeam.Terrorist);
        }

        private void InitializeAwpRecipientsForTeam(CsTeam team)
        {
            var volunteers = PlayerUtils.GetPlayerControllersOfTeam(team)
                .Where(player => CacheManager.Instance.GetFullBuyWeapons(player).awpChance.GetValueOrDefault() > 0)
                .ToList();

            if (!volunteers.Any())
            {
                return;
            }

            var selectedVolunteer = this.PickRandomPlayers(volunteers, 1).FirstOrDefault();

            if (selectedVolunteer == null)
            {
                return;
            }

            var selectedChance = CacheManager.Instance.GetFullBuyWeapons(selectedVolunteer).awpChance.GetValueOrDefault();

            if (this.RollChance(selectedChance))
            {
                this._awpRecipients.Add(selectedVolunteer.SteamID);
            }
        }

        private bool ShouldGiveDefuseKit(CCSPlayerController player, RoundTypeEnum roundType)
        {
            if (player.Team != CsTeam.CounterTerrorist)
            {
                return false;
            }

            if (!this._defuseKitRecipientsInitialized)
            {
                this.InitializeDefuseKitRecipients(roundType);
            }

            return this._defuseKitRecipients.Contains(player.SteamID);
        }

        private void InitializeDefuseKitRecipients(RoundTypeEnum roundType)
        {
            this._defuseKitRecipientsInitialized = true;
            this._defuseKitRecipients.Clear();

            var ctPlayers = PlayerUtils.GetCounterTerroristPlayers();

            if (!ctPlayers.Any())
            {
                return;
            }

            if (roundType == RoundTypeEnum.Pistol)
            {
                this.InitializePistolDefuseKitRecipients(ctPlayers);
                return;
            }

            switch (this.Config.DefuseKitMode)
            {
                case DefuseKitModeEnum.All:
                    foreach (var ctPlayer in ctPlayers)
                    {
                        this._defuseKitRecipients.Add(ctPlayer.SteamID);
                    }
                    break;
                case DefuseKitModeEnum.Quota:
                    foreach (var ctPlayer in this.PickRandomPlayers(ctPlayers, this.Config.DefuseKitQuota))
                    {
                        this._defuseKitRecipients.Add(ctPlayer.SteamID);
                    }
                    break;
                case DefuseKitModeEnum.Chance:
                    foreach (var ctPlayer in ctPlayers.Where(this.RollStandardDefuseKitChance))
                    {
                        this._defuseKitRecipients.Add(ctPlayer.SteamID);
                    }
                    break;
            }
        }

        private void InitializePistolDefuseKitRecipients(List<CCSPlayerController> ctPlayers)
        {
            foreach (var ctPlayer in ctPlayers.Where(this.RollPistolDefuseKitChance))
            {
                this._defuseKitRecipients.Add(ctPlayer.SteamID);
            }

            if (this.Config.PistolDefuseKitGuaranteeMinimum && !this._defuseKitRecipients.Any())
            {
                var guaranteedPlayer = this.PickRandomPlayers(ctPlayers, 1).FirstOrDefault();

                if (guaranteedPlayer != null)
                {
                    this._defuseKitRecipients.Add(guaranteedPlayer.SteamID);
                }
            }
        }

        private bool RollStandardDefuseKitChance(CCSPlayerController player)
        {
            return this.RollChance(this.Config.DefuseKitChance);
        }

        private bool RollPistolDefuseKitChance(CCSPlayerController player)
        {
            return this.RollChance(this.Config.PistolDefuseKitChance);
        }

        private bool RollChance(double chancePercent)
        {
            if (chancePercent <= 0.0d)
            {
                return false;
            }

            if (chancePercent >= 100.0d)
            {
                return true;
            }

            return Random.Shared.NextDouble() * 100.0d <= chancePercent;
        }

        private List<CCSPlayerController> PickRandomPlayers(List<CCSPlayerController> players, int quota)
        {
            if (quota <= 0)
            {
                return new List<CCSPlayerController>();
            }

            return players
                .OrderBy(_ => Random.Shared.Next())
                .Take(Math.Min(quota, players.Count))
                .ToList();
        }

        public void OnAllocatorConfigParsed(CommandAllocatorConfig config)
        {
            if (this.Config.Version > config.Version)
            {
                MessageUtils.Log(Microsoft.Extensions.Logging.LogLevel.Warning, $"The command allocator configuration is out of date. Consider updating the config. [Current Version: {config.Version} - Allocator Version: {this.Config.Version}]");
            }

            this.Config = config;

            var fullBuyConfig = FullBuyMenu.Instance.Config;
            _ = MidMenu.Instance;
            _ = PistolMenu.Instance;

            this.EnsureBaselineWeaponCoverage();

            this._awpChanceCT = fullBuyConfig.AWPChanceCT;
            this._awpChanceT = fullBuyConfig.AWPChanceT;

            MessageUtils.Log(LogLevel.Information, "Initializing weapon preference persistence backend...");

            DBManager.Instance.DBType = Config.DatabaseType;
            DBManager.Instance.AllocatorConfigDirectoryPath = Config.AllocatorConfigDirectoryPath;
            DBManager.Instance.ConnectionString = Config.ConnectionString;
            DBManager.Instance.Init();

            PlayerUtils.GetValidPlayerControllers().ForEach(x => this.OnPlayerConnected(x));


            //Kill the previous timer first: a config hot reload re-enters this method and used to stack duplicates.
            this._howToTimer?.Kill();
            this._howToTimer = null;

            if(this.Config.HowToMessageDelayInMinutes > 0)
            {
                this._howToTimer = new CounterStrikeSharp.API.Modules.Timers.Timer(this.Config.HowToMessageDelayInMinutes * 60, PrintHowToMessage, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);
            }

        }

        public override void OnGunsCommand(CCSPlayerController? player)
        {
            if(this.BasePluginInstance == null)
            {
                return;
            }

            ChooserMenu.OpenMenu(player, this.BasePluginInstance, this.Config.EnableRoundTypePistolMenu, this.Config.EnableRoundTypeMidMenu, this.Config.EnableRoundTypeFullBuyMenu);
        }

        public override void OnPlayerConnected(CCSPlayerController? player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn == null || !player.PlayerPawn.IsValid || player.PlayerPawn.Value == null || !player.PlayerPawn.Value!.IsValid)
            {
                return;
            }

            var steamId = player.SteamID;

            //DB reads run off the game thread: with a remote PostgreSQL backend, 6 blocking
            //round-trips per connect would hitch the whole server. The cache is only written
            //back on the next frame, on the game thread.
            Task.Run(() =>
            {
                try
                {
                    var preferences = this.LoadPersistedPreferences(steamId);

                    Server.NextFrame(() => this.ApplyPersistedPreferences(steamId, preferences));
                }
                catch (Exception ex)
                {
                    MessageUtils.Log(LogLevel.Error, $"Failed to load persisted weapon preferences for SteamID {steamId}: {ex}");
                }
            });
        }

        private ((string? primaryWeapon, string? secondaryWeapon, int? awpChance) fullBuyCT,
                 (string? primaryWeapon, string? secondaryWeapon, int? awpChance) fullBuyT,
                 (string? primaryWeapon, string? secondaryWeapon, int? awpChance) midCT,
                 (string? primaryWeapon, string? secondaryWeapon, int? awpChance) midT,
                 (string? primaryWeapon, string? secondaryWeapon, int? awpChance) pistolCT,
                 (string? primaryWeapon, string? secondaryWeapon, int? awpChance) pistolT) LoadPersistedPreferences(ulong steamId)
        {
            return (
                DBManager.Instance.GetFullBuyWeapons(steamId, (int)CsTeam.CounterTerrorist),
                DBManager.Instance.GetFullBuyWeapons(steamId, (int)CsTeam.Terrorist),
                DBManager.Instance.GetMidWeapons(steamId, (int)CsTeam.CounterTerrorist),
                DBManager.Instance.GetMidWeapons(steamId, (int)CsTeam.Terrorist),
                DBManager.Instance.GetPistolWeapons(steamId, (int)CsTeam.CounterTerrorist),
                DBManager.Instance.GetPistolWeapons(steamId, (int)CsTeam.Terrorist)
            );
        }

        private void ApplyPersistedPreferences(
            ulong steamId,
            ((string? primaryWeapon, string? secondaryWeapon, int? awpChance) fullBuyCT,
             (string? primaryWeapon, string? secondaryWeapon, int? awpChance) fullBuyT,
             (string? primaryWeapon, string? secondaryWeapon, int? awpChance) midCT,
             (string? primaryWeapon, string? secondaryWeapon, int? awpChance) midT,
             (string? primaryWeapon, string? secondaryWeapon, int? awpChance) pistolCT,
             (string? primaryWeapon, string? secondaryWeapon, int? awpChance) pistolT) preferences)
        {
            var player = Utilities.GetPlayerFromSteamId(steamId);

            if (!PlayerUtils.IsPlayableHuman(player))
            {
                return;
            }

            //----------------------------FULLBUY--------------------------------------

            var fullBuyConfig = FullBuyMenu.Instance.Config;

            var fullBuyCT = preferences.fullBuyCT;

            if(!string.IsNullOrWhiteSpace(fullBuyCT.primaryWeapon))
            {
                CacheManager.Instance.AddOrUpdateFullBuyPrimaryCache(player, fullBuyCT.primaryWeapon, CsTeam.CounterTerrorist);
            }
            if (!string.IsNullOrWhiteSpace(fullBuyCT.secondaryWeapon))
            {
                CacheManager.Instance.AddOrUpdateFullBuySecondaryCache(player, fullBuyCT.secondaryWeapon, CsTeam.CounterTerrorist);
            }
            if(fullBuyCT.awpChance != null && fullBuyCT.awpChance.HasValue)
            {
                var highestChance = fullBuyConfig.AWPChanceCT.Chances.OrderDescending().FirstOrDefault();
                var chance = fullBuyCT.awpChance.Value > 0 ? highestChance : 0;

                if (!fullBuyConfig.EnableAWPChance)
                {
                    chance = 0;
                }

                CacheManager.Instance.AddOrUpdateFullBuyAWPChanceCache(player, chance <= highestChance ? chance : highestChance, CsTeam.CounterTerrorist);
            }

            var fullBuyT = preferences.fullBuyT;

            if (!string.IsNullOrWhiteSpace(fullBuyT.primaryWeapon))
            {
                CacheManager.Instance.AddOrUpdateFullBuyPrimaryCache(player, fullBuyT.primaryWeapon, CsTeam.Terrorist);
            }
            if (!string.IsNullOrWhiteSpace(fullBuyT.secondaryWeapon))
            {
                CacheManager.Instance.AddOrUpdateFullBuySecondaryCache(player, fullBuyT.secondaryWeapon, CsTeam.Terrorist);
            }
            if (fullBuyT.awpChance != null && fullBuyT.awpChance.HasValue)
            {
                var highestChance = fullBuyConfig.AWPChanceT.Chances.OrderDescending().FirstOrDefault();
                var chance = fullBuyT.awpChance.Value > 0 ? highestChance : 0;

                if(!fullBuyConfig.EnableAWPChance)
                {
                    chance = 0;
                }

                CacheManager.Instance.AddOrUpdateFullBuyAWPChanceCache(player, chance <= highestChance ? chance : highestChance, CsTeam.Terrorist);
            }

            MessageUtils.LogDebug($"SteamID: {player.SteamID} - CT: {fullBuyCT.primaryWeapon}, {fullBuyCT.secondaryWeapon}, {fullBuyCT.awpChance} - T: {fullBuyT.primaryWeapon}, {fullBuyT.secondaryWeapon}, {fullBuyT.awpChance}");

            //----------------------------MID--------------------------------------

            var midCT = preferences.midCT;

            if (!string.IsNullOrWhiteSpace(midCT.primaryWeapon))
            {
                CacheManager.Instance.AddOrUpdateMidPrimaryCache(player, midCT.primaryWeapon, CsTeam.CounterTerrorist);
            }
            if (!string.IsNullOrWhiteSpace(midCT.secondaryWeapon))
            {
                CacheManager.Instance.AddOrUpdateMidSecondaryCache(player, midCT.secondaryWeapon, CsTeam.CounterTerrorist);
            }

            var midT = preferences.midT;

            if (!string.IsNullOrWhiteSpace(midT.primaryWeapon))
            {
                CacheManager.Instance.AddOrUpdateMidPrimaryCache(player, midT.primaryWeapon, CsTeam.Terrorist);
            }
            if (!string.IsNullOrWhiteSpace(midT.secondaryWeapon))
            {
                CacheManager.Instance.AddOrUpdateMidSecondaryCache(player, midT.secondaryWeapon, CsTeam.Terrorist);
            }

            MessageUtils.LogDebug($"SteamID: {player.SteamID} - CT: {midCT.primaryWeapon}, {midCT.secondaryWeapon}, {midCT.awpChance} - T: {midT.primaryWeapon}, {midT.secondaryWeapon}, {midT.awpChance}");

            //----------------------------PISTOLS--------------------------------------

            var pistolCT = preferences.pistolCT;

            if (!string.IsNullOrWhiteSpace(pistolCT.secondaryWeapon))
            {
                CacheManager.Instance.AddOrUpdatePistolCache(player, pistolCT.secondaryWeapon, CsTeam.CounterTerrorist);
            }

            var pistolT = preferences.pistolT;

            if (!string.IsNullOrWhiteSpace(pistolT.secondaryWeapon))
            {
                CacheManager.Instance.AddOrUpdatePistolCache(player, pistolT.secondaryWeapon, CsTeam.Terrorist);
            }

            MessageUtils.LogDebug($"SteamID: {player.SteamID} - CT: {pistolCT.primaryWeapon}, {pistolCT.secondaryWeapon}, {pistolCT.awpChance} - T: {pistolT.primaryWeapon}, {pistolT.secondaryWeapon}, {pistolT.awpChance}");
        }

        public override void OnPlayerDisconnected(CCSPlayerController? player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn == null || !player.PlayerPawn.IsValid || player.PlayerPawn.Value == null || !player.PlayerPawn.Value!.IsValid)
            {
                return;
            }

            CacheManager.Instance.RemoveUserFromCache(player);
        }

        public override void ResetForNextRound(bool completeReset = true)
        {
            this._awpInUseCountCT = 0;
            this._awpInUseCountT = 0;
            this._awpRecipients.Clear();
            this._awpRecipientsInitialized = false;
            this._defuseKitRecipients.Clear();
            this._defuseKitRecipientsInitialized = false;
        }

        private void PrintHowToMessage()
        {
            Server.PrintToChatAll($"[{ChatColors.Gold}CommandAllocator{ChatColors.White}] {this.Config.HowToMessage}");
        }

        private string NormalizeBuyToken(string rawToken)
        {
            rawToken = rawToken.Trim().ToLowerInvariant();

            if (rawToken.StartsWith("weapon_"))
            {
                rawToken = rawToken[7..];
            }

            return new string(rawToken.Where(char.IsLetterOrDigit).ToArray());
        }

        private void EnsureBaselineWeaponCoverage()
        {
            var pistolSecondaries = PistolMenu.Instance.Config.AvailableSecondaries;
            this.EnsureWeaponExists(pistolSecondaries, "R8 Revolver", "weapon_revolver");

            var midPrimaries = MidMenu.Instance.Config.AvailablePrimaries;
            this.EnsureWeaponExists(midPrimaries, "Famas", "weapon_famas", CsTeam.CounterTerrorist);
            this.EnsureWeaponExists(midPrimaries, "AUG", "weapon_aug", CsTeam.CounterTerrorist);
            this.EnsureWeaponExists(midPrimaries, "Galil", "weapon_galilar", CsTeam.Terrorist);
            this.EnsureWeaponExists(midPrimaries, "SG-553", "weapon_sg556", CsTeam.Terrorist);

            var midSecondaries = MidMenu.Instance.Config.AvailableSecondaries;
            this.EnsureWeaponExists(midSecondaries, "R8 Revolver", "weapon_revolver");

            var fullBuyPrimaries = FullBuyMenu.Instance.Config.AvailablePrimaries;
            this.EnsureWeaponExists(fullBuyPrimaries, "MP-9", "weapon_mp9", CsTeam.CounterTerrorist);
            this.EnsureWeaponExists(fullBuyPrimaries, "Mac-10", "weapon_mac10", CsTeam.Terrorist);
            this.EnsureWeaponExists(fullBuyPrimaries, "MP7", "weapon_mp7");
            this.EnsureWeaponExists(fullBuyPrimaries, "MP5-SD", "weapon_mp5sd");
            this.EnsureWeaponExists(fullBuyPrimaries, "UMP-45", "weapon_ump45");
            this.EnsureWeaponExists(fullBuyPrimaries, "P90", "weapon_p90");
            this.EnsureWeaponExists(fullBuyPrimaries, "PP-Bizon", "weapon_bizon");
            this.EnsureWeaponExists(fullBuyPrimaries, "SSG 08", "weapon_ssg08");
            this.EnsureWeaponExists(fullBuyPrimaries, "SCAR-20", "weapon_scar20", CsTeam.CounterTerrorist);
            this.EnsureWeaponExists(fullBuyPrimaries, "G3SG1", "weapon_g3sg1", CsTeam.Terrorist);
            this.EnsureWeaponExists(fullBuyPrimaries, "MAG-7", "weapon_mag7", CsTeam.CounterTerrorist);
            this.EnsureWeaponExists(fullBuyPrimaries, "Sawed-Off", "weapon_sawedoff", CsTeam.Terrorist);
            this.EnsureWeaponExists(fullBuyPrimaries, "Nova", "weapon_nova");
            this.EnsureWeaponExists(fullBuyPrimaries, "XM1014", "weapon_xm1014");
            this.EnsureWeaponExists(fullBuyPrimaries, "M249", "weapon_m249");
            this.EnsureWeaponExists(fullBuyPrimaries, "Negev", "weapon_negev");

            var fullBuySecondaries = FullBuyMenu.Instance.Config.AvailableSecondaries;
            this.EnsureWeaponExists(fullBuySecondaries, "R8 Revolver", "weapon_revolver");
        }

        private void EnsureWeaponExists(List<WeaponEntity> weapons, string weaponName, string weaponString, CsTeam team = CsTeam.None)
        {
            var normalizedWeaponString = this.NormalizeBuyToken(weaponString);

            if (weapons.Any(weapon => weapon.Team == team && this.NormalizeBuyToken(weapon.WeaponString).Equals(normalizedWeaponString, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            weapons.Add(new WeaponEntity(weaponName, weaponString, team));
        }

        public void Dispose()
        {
            this._howToTimer?.Kill();
        }
    }
}
