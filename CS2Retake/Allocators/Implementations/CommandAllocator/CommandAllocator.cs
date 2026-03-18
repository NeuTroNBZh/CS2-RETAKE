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
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using System.Collections.ObjectModel;

namespace CS2Retake.Allocators.Implementations.CommandAllocator
{
    public class CommandAllocator : BaseGrenadeAllocator, IAllocatorConfig<CommandAllocatorConfig>, IDisposable
    {
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

        public HookResult HandleBuyCommand(CCSPlayerController? player, CommandInfo commandInfo)
        {
            if (player == null || !player.IsValid || player.Team != CsTeam.CounterTerrorist && player.Team != CsTeam.Terrorist)
            {
                return HookResult.Continue;
            }

            if (GameRuleManager.Instance.IsWarmup)
            {
                return HookResult.Continue;
            }

            if (commandInfo.ArgCount < 2)
            {
                return HookResult.Handled;
            }

            var buyToken = this.NormalizeBuyToken(commandInfo.GetArg(1));

            if (string.IsNullOrWhiteSpace(buyToken))
            {
                return HookResult.Handled;
            }

            this.HandleNativeBuySelection(player, buyToken);

            return HookResult.Handled;
        }

        private void HandleNativeBuySelection(CCSPlayerController player, string buyToken)
        {
            if (this.IsAutoManagedItem(buyToken))
            {
                MessageUtils.PrintToPlayerOrServer(this.GetAutoManagedItemMessage(), player);
                return;
            }

            switch (RoundTypeManager.Instance.RoundType)
            {
                case RoundTypeEnum.Pistol:
                    this.HandlePistolBuySelection(player, buyToken);
                    return;
                case RoundTypeEnum.Mid:
                    this.HandleMidBuySelection(player, buyToken);
                    return;
                case RoundTypeEnum.FullBuy:
                    this.HandleFullBuySelection(player, buyToken);
                    return;
                default:
                    MessageUtils.PrintToPlayerOrServer(this.GetUnavailableForRoundTypeMessage(), player);
                    return;
            }
        }

        private void HandlePistolBuySelection(CCSPlayerController player, string buyToken)
        {
            var weapon = this.FindMatchingWeapon(PistolMenu.Instance.Config.AvailableSecondaries, buyToken, player.Team);

            if (weapon == null)
            {
                MessageUtils.PrintToPlayerOrServer(this.GetUnavailableForRoundTypeMessage(), player);
                return;
            }

            CacheManager.Instance.AddOrUpdatePistolCache(player, weapon.WeaponString, player.Team);
            DBManager.Instance.InsertOrUpdatePistolWeaponString(player.SteamID, weapon.WeaponString, (int)player.Team);

            MessageUtils.PrintToPlayerOrServer(this.GetWeaponSelectedMessage(weapon.WeaponName, RoundTypeEnum.Pistol, player.Team), player);
        }

        private void HandleMidBuySelection(CCSPlayerController player, string buyToken)
        {
            var primary = this.FindMatchingWeapon(MidMenu.Instance.Config.AvailablePrimaries, buyToken, player.Team);

            if (primary != null)
            {
                CacheManager.Instance.AddOrUpdateMidPrimaryCache(player, primary.WeaponString, player.Team);
                DBManager.Instance.InsertOrUpdateMidPrimaryWeaponString(player.SteamID, primary.WeaponString, (int)player.Team);
                MessageUtils.PrintToPlayerOrServer(this.GetWeaponSelectedMessage(primary.WeaponName, RoundTypeEnum.Mid, player.Team), player);
                return;
            }

            var secondary = this.FindMatchingWeapon(MidMenu.Instance.Config.AvailableSecondaries, buyToken, player.Team);

            if (secondary != null)
            {
                CacheManager.Instance.AddOrUpdateMidSecondaryCache(player, secondary.WeaponString, player.Team);
                DBManager.Instance.InsertOrUpdateMidSecondaryWeaponString(player.SteamID, secondary.WeaponString, (int)player.Team);
                MessageUtils.PrintToPlayerOrServer(this.GetWeaponSelectedMessage(secondary.WeaponName, RoundTypeEnum.Mid, player.Team), player);
                return;
            }

            MessageUtils.PrintToPlayerOrServer(this.GetUnavailableForRoundTypeMessage(), player);
        }

        private void HandleFullBuySelection(CCSPlayerController player, string buyToken)
        {
            var fullBuyConfig = FullBuyMenu.Instance.Config;

            if (buyToken == this.NormalizeBuyToken("weapon_awp"))
            {
                if (!fullBuyConfig.EnableAWPChance)
                {
                    MessageUtils.PrintToPlayerOrServer(this.GetAwpUnavailableMessage(), player);
                    return;
                }

                this.ToggleAwpPreference(player, player.Team);
                return;
            }

            var primary = this.FindMatchingWeapon(fullBuyConfig.AvailablePrimaries, buyToken, player.Team);

            if (primary != null)
            {
                CacheManager.Instance.AddOrUpdateFullBuyPrimaryCache(player, primary.WeaponString, player.Team);
                DBManager.Instance.InsertOrUpdateFullBuyPrimaryWeaponString(player.SteamID, primary.WeaponString, (int)player.Team);
                MessageUtils.PrintToPlayerOrServer(this.GetWeaponSelectedMessage(primary.WeaponName, RoundTypeEnum.FullBuy, player.Team), player);
                return;
            }

            var secondary = this.FindMatchingWeapon(fullBuyConfig.AvailableSecondaries, buyToken, player.Team);

            if (secondary != null)
            {
                CacheManager.Instance.AddOrUpdateFullBuySecondaryCache(player, secondary.WeaponString, player.Team);
                DBManager.Instance.InsertOrUpdateFullBuySecondaryWeaponString(player.SteamID, secondary.WeaponString, (int)player.Team);
                MessageUtils.PrintToPlayerOrServer(this.GetWeaponSelectedMessage(secondary.WeaponName, RoundTypeEnum.FullBuy, player.Team), player);
                return;
            }

            MessageUtils.PrintToPlayerOrServer(this.GetUnavailableForRoundTypeMessage(), player);
        }

        private void ToggleAwpPreference(CCSPlayerController player, CsTeam team)
        {
            if (!FullBuyMenu.Instance.Config.EnableAWPChance)
            {
                MessageUtils.PrintToPlayerOrServer(this.GetAwpUnavailableMessage(), player);
                return;
            }

            var currentChance = CacheManager.Instance.GetFullBuyWeapons(player, team).awpChance ?? 0;
            var enabledChance = (team == CsTeam.CounterTerrorist ? this._awpChanceCT : this._awpChanceT).Chances.OrderDescending().FirstOrDefault();
            var nextChance = currentChance > 0 ? 0 : enabledChance;

            CacheManager.Instance.AddOrUpdateFullBuyAWPChanceCache(player, nextChance, team);
            DBManager.Instance.InsertOrUpdateFullBuyAWPChance(player.SteamID, nextChance, (int)team);

            MessageUtils.PrintToPlayerOrServer(this.GetAwpToggleMessage(nextChance > 0, enabledChance), player);
        }

        private WeaponEntity? FindMatchingWeapon(IEnumerable<WeaponEntity> weapons, string buyToken, CsTeam team)
        {
            return weapons.FirstOrDefault(weapon => (weapon.Team == team || weapon.Team == CsTeam.None) && this.MatchesBuyToken(weapon, buyToken));
        }

        private bool MatchesBuyToken(WeaponEntity weapon, string buyToken)
        {
            var candidates = new HashSet<string>
            {
                this.NormalizeBuyToken(weapon.WeaponString),
                this.NormalizeBuyToken(weapon.WeaponName),
            };

            switch (this.NormalizeBuyToken(weapon.WeaponString))
            {
                case "m4a1silencer":
                    candidates.Add("m4a1s");
                    break;
                case "uspsilencer":
                    candidates.Add("usps");
                    break;
                case "mp5sd":
                    candidates.Add("mp5");
                    break;
                case "galilar":
                    candidates.Add("galil");
                    break;
                case "sg556":
                    candidates.Add("sg553");
                    break;
                case "hkp2000":
                    candidates.Add("p2000");
                    break;
                case "cz75a":
                    candidates.Add("cz75");
                    break;
            }

            return candidates.Contains(buyToken);
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

        private bool IsAutoManagedItem(string buyToken)
        {
            return new HashSet<string>
            {
                "vest",
                "vesthelm",
                "assaultsuit",
                "kevlar",
                "helmet",
                "defuser",
                "hegrenade",
                "incgrenade",
                "molotov",
                "flashbang",
                "smokegrenade",
                "decoy",
                "taser",
            }.Contains(buyToken);
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

        private string GetWeaponSelectedMessage(string weaponName, RoundTypeEnum roundType, CsTeam team)
        {
            var roundTypeName = roundType switch
            {
                RoundTypeEnum.FullBuy => "Full Buy",
                RoundTypeEnum.Mid => "Mid",
                _ => "Pistol",
            };

            var teamName = team == CsTeam.CounterTerrorist ? "CT" : "T";

            return FeatureConfig.MessageLanguage switch
            {
                RetakeMessageLanguageEnum.French => $"{ChatColors.Green}{weaponName}{ChatColors.White} selectionne pour les rounds {ChatColors.Green}{roundTypeName}{ChatColors.White} en {ChatColors.Green}{teamName}{ChatColors.White}.",
                _ => $"{ChatColors.Green}{weaponName}{ChatColors.White} selected for {ChatColors.Green}{roundTypeName}{ChatColors.White} rounds as {ChatColors.Green}{teamName}{ChatColors.White}.",
            };
        }

        private string GetAwpToggleMessage(bool enabled, int chance)
        {
            return FeatureConfig.MessageLanguage switch
            {
                RetakeMessageLanguageEnum.French => enabled
                    ? $"AWP active ({ChatColors.Green}{chance}%{ChatColors.White} de chance)."
                    : "AWP desactive.",
                _ => enabled
                    ? $"AWP enabled ({ChatColors.Green}{chance}%{ChatColors.White} chance)."
                    : "AWP disabled.",
            };
        }

        private string GetUnavailableForRoundTypeMessage()
        {
            return FeatureConfig.MessageLanguage switch
            {
                RetakeMessageLanguageEnum.French => "Cette arme n'est pas disponible pour ce type de round.",
                _ => "This weapon is not available for the current round type.",
            };
        }

        private string GetAwpUnavailableMessage()
        {
            return FeatureConfig.MessageLanguage switch
            {
                RetakeMessageLanguageEnum.French => "L'AWP est desactive par la configuration du serveur.",
                _ => "AWP is disabled by the server configuration.",
            };
        }

        private string GetAutoManagedItemMessage()
        {
            return FeatureConfig.MessageLanguage switch
            {
                RetakeMessageLanguageEnum.French => "Cet objet est gere automatiquement par Retake.",
                _ => "This item is managed automatically by Retake.",
            };
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

            this._awpChanceCT = fullBuyConfig.AWPChanceCT;
            this._awpChanceT = fullBuyConfig.AWPChanceT;

            MessageUtils.Log(LogLevel.Warning, $"For some reason sometimes an exception happens with the sqlite stuff. It still works. So no worries needed.");

            DBManager.Instance.DBType = Config.DatabaseType;
            DBManager.Instance.AllocatorConfigDirectoryPath = Config.AllocatorConfigDirectoryPath;
            DBManager.Instance.ConnectionString = Config.ConnectionString;
            DBManager.Instance.Init();

            PlayerUtils.GetValidPlayerControllers().ForEach(x => this.OnPlayerConnected(x));


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

            //----------------------------FULLBUY--------------------------------------

            var fullBuyConfig = FullBuyMenu.Instance.Config;

            (string? primaryWeapon, string? secondaryWeapon, int? awpChance) fullBuyCT = DBManager.Instance.GetFullBuyWeapons(player.SteamID, (int)CsTeam.CounterTerrorist);

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

            var fullBuyT = DBManager.Instance.GetFullBuyWeapons(player.SteamID, (int)CsTeam.Terrorist);

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

            var midCT = DBManager.Instance.GetMidWeapons(player.SteamID, (int)CsTeam.CounterTerrorist);

            if (!string.IsNullOrWhiteSpace(midCT.primaryWeapon))
            {
                CacheManager.Instance.AddOrUpdateMidPrimaryCache(player, midCT.primaryWeapon, CsTeam.CounterTerrorist);
            }
            if (!string.IsNullOrWhiteSpace(midCT.secondaryWeapon))
            {
                CacheManager.Instance.AddOrUpdateMidSecondaryCache(player, midCT.secondaryWeapon, CsTeam.CounterTerrorist);
            }

            var midT = DBManager.Instance.GetMidWeapons(player.SteamID, (int)CsTeam.Terrorist);

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

            var pistolCT = DBManager.Instance.GetPistolWeapons(player.SteamID, (int)CsTeam.CounterTerrorist);

            if (!string.IsNullOrWhiteSpace(pistolCT.secondaryWeapon))
            {
                CacheManager.Instance.AddOrUpdatePistolCache(player, pistolCT.secondaryWeapon, CsTeam.CounterTerrorist);
            }

            var pistolT = DBManager.Instance.GetPistolWeapons(player.SteamID, (int)CsTeam.Terrorist);

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

        public void Dispose()
        {
            this._howToTimer?.Kill();
        }
    }
}
