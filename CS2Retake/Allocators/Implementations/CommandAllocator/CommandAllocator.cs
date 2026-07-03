using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
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
        private static CommandAllocator? _instance = null;
        private bool _attributeHandlersRegistered = false;

        public static CommandAllocator? Instance => _instance;
        public bool IsInitialized => _attributeHandlersRegistered;

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
        private HashSet<uint> _unknownNativeBuyWeaponIds = new HashSet<uint>();
        private HashSet<string> _loggedBlockedNativeBuySelections = new HashSet<string>();
        private readonly Dictionary<ulong, (float expiresAt, RoundTypeEnum roundType, CsTeam team)> _pendingNativeBuySelections = new Dictionary<ulong, (float expiresAt, RoundTypeEnum roundType, CsTeam team)>();

        public CommandAllocator()
        {
            _instance = this;
        }

        public void Initialize(IPlugin plugin)
        {
            if (!_attributeHandlersRegistered)
            {
                plugin.RegisterAllAttributes(this);
                _attributeHandlersRegistered = true;
            }
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

            MessageUtils.Log(
                LogLevel.Information,
                $"Buy command intercepted. Player='{player.PlayerName}', ArgString='{commandInfo.ArgString}'");

            var buyTokens = this.ExtractBuyTokens(player, commandInfo);

            if (this.ContainsAutoManagedItem(buyTokens))
            {
                MessageUtils.PrintToPlayerOrServer(this.GetAutoManagedItemMessage(), player);
                return HookResult.Handled;
            }

            if (this.HasNumericBuyPayload(commandInfo))
            {
                // Numeric payloads (buy unused <id>) can be ambiguous in declarative mapping.
                // Use exact capture mode to persist the weapon actually granted by the game.
                MessageUtils.Log(
                    LogLevel.Information,
                    $"Native buy numeric payload fallback enabled. Player='{player.PlayerName}', Team='{player.Team}', RoundType='{RoundTypeManager.Instance.RoundType}', ArgString='{commandInfo.ArgString}', Tokens='{string.Join(",", buyTokens.OrderBy(token => token))}'");

                this.RegisterPendingNativeBuySelection(player);
                return HookResult.Continue;
            }

            // Fast path: resolve declaratively whenever we can (no native purchase side effects).
            if (this.CanResolveNativeSelectionDeclaratively(player, buyTokens))
            {
                this.HandleNativeBuySelection(player, buyTokens, commandInfo);
                return HookResult.Handled;
            }

            // Known weapon but invalid for current team/round: block immediately.
            if (this.IsKnownConfiguredWeaponSelection(buyTokens))
            {
                this.LogUnmatchedBuySelection(player, commandInfo, buyTokens);
                MessageUtils.PrintToPlayerOrServer(this.GetUnavailableForRoundTypeMessage(), player);
                return HookResult.Handled;
            }

            // Fallback (Option 1): unresolved/ambiguous payload, capture exact weapon from item_pickup.
            this.RegisterPendingNativeBuySelection(player);
            return HookResult.Continue;
        }

        private bool CanResolveNativeSelectionDeclaratively(CCSPlayerController player, IReadOnlyCollection<string> buyTokens)
        {
            switch (RoundTypeManager.Instance.RoundType)
            {
                case RoundTypeEnum.Pistol:
                    return this.FindMatchingWeapon(PistolMenu.Instance.Config.AvailableSecondaries, buyTokens, player.Team) != null;
                case RoundTypeEnum.Mid:
                    return this.FindMatchingWeapon(MidMenu.Instance.Config.AvailablePrimaries, buyTokens, player.Team) != null
                        || this.FindMatchingWeapon(MidMenu.Instance.Config.AvailableSecondaries, buyTokens, player.Team) != null;
                case RoundTypeEnum.FullBuy:
                    return buyTokens.Contains(this.NormalizeBuyToken("weapon_awp"))
                        || buyTokens.Contains("awp")
                        || this.FindMatchingWeapon(FullBuyMenu.Instance.Config.AvailablePrimaries, buyTokens, player.Team) != null
                        || this.FindMatchingWeapon(FullBuyMenu.Instance.Config.AvailableSecondaries, buyTokens, player.Team) != null;
                default:
                    return false;
            }
        }

        private bool HasNumericBuyPayload(CommandInfo commandInfo)
        {
            for (var index = 1; index < commandInfo.ArgCount; index++)
            {
                if (uint.TryParse(this.NormalizeBuyToken(commandInfo.GetArg(index)), out _))
                {
                    return true;
                }
            }

            foreach (var segment in commandInfo.ArgString.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (uint.TryParse(this.NormalizeBuyToken(segment), out _))
                {
                    return true;
                }
            }

            return false;
        }

        private void HandleNativeBuySelection(CCSPlayerController player, IReadOnlyCollection<string> buyTokens, CommandInfo commandInfo)
        {
            if (this.ContainsAutoManagedItem(buyTokens))
            {
                MessageUtils.PrintToPlayerOrServer(this.GetAutoManagedItemMessage(), player);
                return;
            }

            switch (RoundTypeManager.Instance.RoundType)
            {
                case RoundTypeEnum.Pistol:
                    this.HandlePistolBuySelection(player, buyTokens, commandInfo);
                    return;
                case RoundTypeEnum.Mid:
                    this.HandleMidBuySelection(player, buyTokens, commandInfo);
                    return;
                case RoundTypeEnum.FullBuy:
                    this.HandleFullBuySelection(player, buyTokens, commandInfo);
                    return;
                default:
                    MessageUtils.PrintToPlayerOrServer(this.GetUnavailableForRoundTypeMessage(), player);
                    return;
            }
        }

        private void HandlePistolBuySelection(CCSPlayerController player, IReadOnlyCollection<string> buyTokens, CommandInfo commandInfo)
        {
            var weapon = this.FindMatchingWeapon(PistolMenu.Instance.Config.AvailableSecondaries, buyTokens, player.Team);

            if (weapon == null)
            {
                this.LogUnmatchedBuySelection(player, commandInfo, buyTokens);
                MessageUtils.PrintToPlayerOrServer(this.GetUnavailableForRoundTypeMessage(), player);
                return;
            }

            CacheManager.Instance.AddOrUpdatePistolCache(player, weapon.WeaponString, player.Team);
            var dbPersisted = DBManager.Instance.InsertOrUpdatePistolWeaponString(player.SteamID, weapon.WeaponString, (int)player.Team);

            MessageUtils.Log(
                LogLevel.Information,
                $"Native buy pistol selection persisted. Player='{player.PlayerName}', Team='{player.Team}', Weapon='{weapon.WeaponString}', DbPersisted='{dbPersisted}'");

            MessageUtils.PrintToPlayerOrServer(this.GetWeaponSelectedMessage(weapon.WeaponName, RoundTypeEnum.Pistol, player.Team), player);
        }

        private void HandleMidBuySelection(CCSPlayerController player, IReadOnlyCollection<string> buyTokens, CommandInfo commandInfo)
        {
            var primary = this.FindMatchingWeapon(MidMenu.Instance.Config.AvailablePrimaries, buyTokens, player.Team);

            if (primary != null)
            {
                CacheManager.Instance.AddOrUpdateMidPrimaryCache(player, primary.WeaponString, player.Team);
                var dbPersisted = DBManager.Instance.InsertOrUpdateMidPrimaryWeaponString(player.SteamID, primary.WeaponString, (int)player.Team);
                MessageUtils.Log(
                    LogLevel.Information,
                    $"Native buy mid primary persisted. Player='{player.PlayerName}', Team='{player.Team}', Weapon='{primary.WeaponString}', DbPersisted='{dbPersisted}'");
                MessageUtils.PrintToPlayerOrServer(this.GetWeaponSelectedMessage(primary.WeaponName, RoundTypeEnum.Mid, player.Team), player);
                return;
            }

            var secondary = this.FindMatchingWeapon(MidMenu.Instance.Config.AvailableSecondaries, buyTokens, player.Team);

            if (secondary != null)
            {
                CacheManager.Instance.AddOrUpdateMidSecondaryCache(player, secondary.WeaponString, player.Team);
                var dbPersisted = DBManager.Instance.InsertOrUpdateMidSecondaryWeaponString(player.SteamID, secondary.WeaponString, (int)player.Team);
                MessageUtils.Log(
                    LogLevel.Information,
                    $"Native buy mid secondary persisted. Player='{player.PlayerName}', Team='{player.Team}', Weapon='{secondary.WeaponString}', DbPersisted='{dbPersisted}'");
                MessageUtils.PrintToPlayerOrServer(this.GetWeaponSelectedMessage(secondary.WeaponName, RoundTypeEnum.Mid, player.Team), player);
                return;
            }

            this.LogUnmatchedBuySelection(player, commandInfo, buyTokens);
            MessageUtils.PrintToPlayerOrServer(this.GetUnavailableForRoundTypeMessage(), player);
        }

        private void HandleFullBuySelection(CCSPlayerController player, IReadOnlyCollection<string> buyTokens, CommandInfo commandInfo)
        {
            var fullBuyConfig = FullBuyMenu.Instance.Config;

            if (buyTokens.Contains(this.NormalizeBuyToken("weapon_awp")) || buyTokens.Contains("awp"))
            {
                if (!fullBuyConfig.EnableAWPChance)
                {
                    MessageUtils.PrintToPlayerOrServer(this.GetAwpUnavailableMessage(), player);
                    return;
                }

                this.ToggleAwpPreference(player, player.Team);
                return;
            }

            var primary = this.FindMatchingWeapon(fullBuyConfig.AvailablePrimaries, buyTokens, player.Team);

            if (primary != null)
            {
                CacheManager.Instance.AddOrUpdateFullBuyPrimaryCache(player, primary.WeaponString, player.Team);
                var dbPersisted = DBManager.Instance.InsertOrUpdateFullBuyPrimaryWeaponString(player.SteamID, primary.WeaponString, (int)player.Team);
                MessageUtils.Log(
                    LogLevel.Information,
                    $"Native buy fullbuy primary persisted. Player='{player.PlayerName}', Team='{player.Team}', Weapon='{primary.WeaponString}', DbPersisted='{dbPersisted}'");
                MessageUtils.PrintToPlayerOrServer(this.GetWeaponSelectedMessage(primary.WeaponName, RoundTypeEnum.FullBuy, player.Team), player);
                return;
            }

            var secondary = this.FindMatchingWeapon(fullBuyConfig.AvailableSecondaries, buyTokens, player.Team);

            if (secondary != null)
            {
                CacheManager.Instance.AddOrUpdateFullBuySecondaryCache(player, secondary.WeaponString, player.Team);
                var dbPersisted = DBManager.Instance.InsertOrUpdateFullBuySecondaryWeaponString(player.SteamID, secondary.WeaponString, (int)player.Team);
                MessageUtils.Log(
                    LogLevel.Information,
                    $"Native buy fullbuy secondary persisted. Player='{player.PlayerName}', Team='{player.Team}', Weapon='{secondary.WeaponString}', DbPersisted='{dbPersisted}'");
                MessageUtils.PrintToPlayerOrServer(this.GetWeaponSelectedMessage(secondary.WeaponName, RoundTypeEnum.FullBuy, player.Team), player);
                return;
            }

            this.LogUnmatchedBuySelection(player, commandInfo, buyTokens);
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
            var dbPersisted = DBManager.Instance.InsertOrUpdateFullBuyAWPChance(player.SteamID, nextChance, (int)team);

            MessageUtils.Log(
                LogLevel.Information,
                $"Native buy awp toggle persisted. Player='{player.PlayerName}', Team='{team}', NextChance='{nextChance}', DbPersisted='{dbPersisted}'");

            MessageUtils.PrintToPlayerOrServer(this.GetAwpToggleMessage(nextChance > 0, enabledChance), player);
        }

        private WeaponEntity? FindMatchingWeapon(IEnumerable<WeaponEntity> weapons, IReadOnlyCollection<string> buyTokens, CsTeam team)
        {
            return weapons.FirstOrDefault(weapon => (weapon.Team == team || weapon.Team == CsTeam.None) && this.MatchesBuyToken(weapon, buyTokens));
        }

        private bool MatchesBuyToken(WeaponEntity weapon, IReadOnlyCollection<string> buyTokens)
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
                case "tec9":
                    candidates.Add("tec");
                    break;
                case "elite":
                    candidates.Add("dualies");
                    candidates.Add("dualberettas");
                    break;
                case "fiveseven":
                    candidates.Add("five7");
                    break;
                case "mac10":
                    candidates.Add("mac10smg");
                    candidates.Add("mac");
                    break;
                case "ump45":
                    candidates.Add("ump");
                    break;
            }

            return buyTokens.Any(candidates.Contains);
        }

        private HashSet<string> ExtractBuyTokens(CCSPlayerController player, CommandInfo commandInfo)
        {
            var tokens = new HashSet<string>();

            for (var index = 1; index < commandInfo.ArgCount; index++)
            {
                this.AddBuyToken(tokens, player, commandInfo.GetArg(index));
            }

            foreach (var segment in commandInfo.ArgString.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                this.AddBuyToken(tokens, player, segment);
            }

            return tokens;
        }

        private void AddBuyToken(HashSet<string> tokens, CCSPlayerController player, string rawToken)
        {
            var normalizedToken = this.NormalizeBuyToken(rawToken);

            if (string.IsNullOrWhiteSpace(normalizedToken) || normalizedToken == "buy")
            {
                return;
            }

            if (uint.TryParse(normalizedToken, out var weaponId))
            {
                var resolvedWeaponToken = this.ResolveNumericBuyToken(player, weaponId);

                if (!string.IsNullOrWhiteSpace(resolvedWeaponToken))
                {
                    this.AddBuyToken(tokens, player, resolvedWeaponToken);
                }
                else if (this._unknownNativeBuyWeaponIds.Add(weaponId))
                {
                    MessageUtils.Log(
                        LogLevel.Warning,
                        $"Unknown native buy numeric token '{weaponId}' received from CS2 buy menu. If players cannot select this item, extend CommandAllocator.ResolveNumericBuyToken().");
                }

                return;
            }

            tokens.Add(normalizedToken);

            if (normalizedToken.StartsWith("item"))
            {
                tokens.Add(normalizedToken[4..]);
            }

            switch (normalizedToken)
            {
                case "m4a1s":
                case "m4a1silencer":
                    tokens.Add("m4a1silencer");
                    tokens.Add("m4a1s");
                    break;
                case "usp":
                case "usps":
                case "uspsilencer":
                    tokens.Add("uspsilencer");
                    tokens.Add("usps");
                    break;
                case "p2000":
                case "hkp2000":
                    tokens.Add("p2000");
                    tokens.Add("hkp2000");
                    break;
                case "galil":
                case "galilar":
                    tokens.Add("galil");
                    tokens.Add("galilar");
                    break;
                case "krieg":
                case "sg553":
                case "sg556":
                    tokens.Add("sg553");
                    tokens.Add("sg556");
                    break;
                case "mp5":
                case "mp5sd":
                    tokens.Add("mp5");
                    tokens.Add("mp5sd");
                    break;
                case "cz75":
                case "cz75a":
                    tokens.Add("cz75");
                    tokens.Add("cz75a");
                    break;
                case "r8":
                case "revolver":
                    tokens.Add("r8");
                    tokens.Add("revolver");
                    break;
                case "tec":
                case "tec9":
                    tokens.Add("tec");
                    tokens.Add("tec9");
                    break;
                case "scout":
                case "ssg08":
                    tokens.Add("scout");
                    tokens.Add("ssg08");
                    break;
                case "dualies":
                case "dualberettas":
                case "elite":
                    tokens.Add("dualies");
                    tokens.Add("dualberettas");
                    tokens.Add("elite");
                    break;
                case "five7":
                case "fiveseven":
                    tokens.Add("five7");
                    tokens.Add("fiveseven");
                    break;
            }
        }

        private string? ResolveNumericBuyToken(CCSPlayerController player, uint selectionId)
        {
            var resolvedByLoadout = this.ResolveLoadoutSlotToken(player, selectionId);

            if (!string.IsNullOrWhiteSpace(resolvedByLoadout))
            {
                return resolvedByLoadout;
            }

            return this.ResolveSelectionSlotToken(selectionId);
        }

        private string? ResolveLoadoutSlotToken(CCSPlayerController player, uint selectionId)
        {
            try
            {
                var inventoryServices = player.InventoryServices;

                if (inventoryServices == null)
                {
                    return null;
                }

                foreach (var loadoutSlot in inventoryServices.ServerAuthoritativeWeaponSlots)
                {
                    if (loadoutSlot == null || loadoutSlot.UnSlot != selectionId)
                    {
                        continue;
                    }

                    return this.ResolveItemDefinitionToken(loadoutSlot.UnItemDefIdx)
                        ?? this.ResolveSelectionSlotToken(loadoutSlot.UnItemDefIdx);
                }
            }
            catch (Exception ex)
            {
                MessageUtils.LogDebug($"Native buy loadout resolution failed for player '{player.PlayerName}' and selectionId '{selectionId}': {ex.Message}");
            }

            return null;
        }

        private string? ResolveSelectionSlotToken(uint weaponId)
        {
            return weaponId switch
            {
                0 => "glock",
                1 => "hkp2000",
                2 => "cz75a",
                3 => "elite",
                4 => "deagle",
                5 => "fiveseven",
                6 => "p250",
                7 => "revolver",
                8 => "tec9",
                9 => "usp_silencer",
                10 => "ak47",
                11 => "m4a1",
                12 => "m4a1_silencer",
                13 => "famas",
                14 => "galilar",
                15 => "aug",
                16 => "sg556",
                17 => "bizon",
                18 => "mac10",
                19 => "mp5sd",
                20 => "mp7",
                21 => "mp9",
                22 => "p90",
                23 => "ump45",
                24 => "mag7",
                25 => "nova",
                26 => "sawedoff",
                27 => "xm1014",
                28 => "awp",
                29 => "ssg08",
                30 => "g3sg1",
                31 => "scar20",
                32 => "m249",
                33 => "negev",
                34 => "taser",
                35 => "decoy",
                36 => "flashbang",
                37 => "hegrenade",
                38 => "incgrenade",
                39 => "molotov",
                40 => "smokegrenade",
                _ => null,
            };
        }

        private string? ResolveItemDefinitionToken(uint itemDefinitionIndex)
        {
            return itemDefinitionIndex switch
            {
                1 => "deagle",
                2 => "elite",
                3 => "fiveseven",
                4 => "glock",
                7 => "ak47",
                8 => "aug",
                9 => "awp",
                10 => "famas",
                11 => "g3sg1",
                13 => "galilar",
                14 => "m249",
                16 => "m4a1",
                17 => "mac10",
                19 => "p90",
                23 => "mp5sd",
                24 => "ump45",
                25 => "xm1014",
                26 => "bizon",
                27 => "mag7",
                28 => "negev",
                29 => "sawedoff",
                30 => "tec9",
                31 => "taser",
                32 => "hkp2000",
                33 => "mp7",
                34 => "mp9",
                35 => "nova",
                36 => "p250",
                38 => "scar20",
                39 => "sg556",
                40 => "ssg08",
                60 => "m4a1_silencer",
                61 => "usp_silencer",
                63 => "cz75a",
                64 => "revolver",
                _ => null,
            };
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

        private bool ContainsAutoManagedItem(IReadOnlyCollection<string> buyTokens)
        {
            var autoManagedItems = new HashSet<string>
            {
                "vest",
                "vesthelm",
                "assaultsuit",
                "kevlar",
                "itemkevlar",
                "helmet",
                "defuser",
                "itemdefuser",
                "hegrenade",
                "incgrenade",
                "molotov",
                "flashbang",
                "smokegrenade",
                "decoy",
                "taser",
            };

            return buyTokens.Any(autoManagedItems.Contains);
        }

        private void LogUnmatchedBuySelection(CCSPlayerController player, CommandInfo commandInfo, IReadOnlyCollection<string> buyTokens)
        {
            var roundType = RoundTypeManager.Instance.RoundType;
            var orderedTokens = string.Join(",", buyTokens.OrderBy(token => token));
            var logKey = $"{GameRuleManager.Instance.TotalRoundsPlayed}:{roundType}:{player.SteamID}:{commandInfo.ArgString.Trim()}:{orderedTokens}";

            if (!this._loggedBlockedNativeBuySelections.Add(logKey))
            {
                return;
            }

            MessageUtils.Log(
                LogLevel.Information,
                $"Native buy selection blocked. Player='{player.PlayerName}', Team='{player.Team}', RoundType='{roundType}', ArgString='{commandInfo.ArgString}', Tokens='{orderedTokens}'");

            if (this.IsKnownConfiguredWeaponSelection(buyTokens))
            {
                MessageUtils.LogDebug(
                    $"Blocked native buy selection outside allowed team/round. Player='{player.PlayerName}', Team='{player.Team}', RoundType='{roundType}', ArgString='{commandInfo.ArgString}', Tokens='{orderedTokens}'");
                return;
            }

            MessageUtils.Log(
                LogLevel.Information,
                $"Native buy selection did not match any configured weapon. Player='{player.PlayerName}', Team='{player.Team}', RoundType='{roundType}', ArgString='{commandInfo.ArgString}', Tokens='{orderedTokens}'");
        }

        private bool IsKnownConfiguredWeaponSelection(IReadOnlyCollection<string> buyTokens)
        {
            if (buyTokens.Contains("awp"))
            {
                return true;
            }

            IEnumerable<WeaponEntity> weapons = PistolMenu.Instance.Config.AvailableSecondaries
                .Concat(MidMenu.Instance.Config.AvailablePrimaries)
                .Concat(MidMenu.Instance.Config.AvailableSecondaries)
                .Concat(FullBuyMenu.Instance.Config.AvailablePrimaries)
                .Concat(FullBuyMenu.Instance.Config.AvailableSecondaries);

            return weapons.Any(weapon => this.MatchesBuyToken(weapon, buyTokens));
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

            this.EnsureBaselineWeaponCoverage();

            this._awpChanceCT = fullBuyConfig.AWPChanceCT;
            this._awpChanceT = fullBuyConfig.AWPChanceT;

            MessageUtils.Log(LogLevel.Information, "Initializing weapon preference persistence backend...");

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
            this._pendingNativeBuySelections.Remove(player.SteamID);
        }

        public override void ResetForNextRound(bool completeReset = true)
        {
            this._awpInUseCountCT = 0;
            this._awpInUseCountT = 0;
            this._awpRecipients.Clear();
            this._awpRecipientsInitialized = false;
            this._defuseKitRecipients.Clear();
            this._defuseKitRecipientsInitialized = false;
            this._loggedBlockedNativeBuySelections.Clear();
            this._pendingNativeBuySelections.Clear();
        }

        private void PrintHowToMessage()
        {
            Server.PrintToChatAll($"[{ChatColors.Gold}CommandAllocator{ChatColors.White}] {this.Config.HowToMessage}");
        }

        [GameEventHandler]
        public HookResult OnItemPickup(EventItemPickup @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid || player.Team != CsTeam.CounterTerrorist && player.Team != CsTeam.Terrorist)
            {
                return HookResult.Continue;
            }

            if (GameRuleManager.Instance.IsWarmup)
            {
                return HookResult.Continue;
            }

            if (!this.TryGetPendingNativeBuySelection(player, out var pendingSelection))
            {
                return HookResult.Continue;
            }

            var weaponName = @event.Item ?? "";
            var normalizedWeapon = this.ResolveNormalizedPickupWeapon(@event);

            if (string.IsNullOrWhiteSpace(normalizedWeapon) || normalizedWeapon == "knife")
            {
                return HookResult.Continue;
            }

            if (this.ContainsAutoManagedItem(new HashSet<string> { normalizedWeapon }))
            {
                this._pendingNativeBuySelections.Remove(player.SteamID);
                this.TryRemovePickedWeapon(player, normalizedWeapon);
                this.TryResetNativeBuyCash(player);
                MessageUtils.PrintToPlayerOrServer(this.GetAutoManagedItemMessage(), player);
                return HookResult.Continue;
            }

            this._pendingNativeBuySelections.Remove(player.SteamID);

            MessageUtils.Log(
                LogLevel.Information,
                $"Item pickup detected. Player='{player.PlayerName}', SteamId='{player.SteamID}', Team='{player.Team}', RoundType='{pendingSelection.roundType}', Weapon='{weaponName}', Defindex='{@event.Defindex}'");

            var persisted = this.PersistActualWeaponSelection(player, normalizedWeapon, pendingSelection.roundType);

            // Keep selection-only behavior: do not keep the instantly-bought weapon equipped.
            this.TryRemovePickedWeapon(player, normalizedWeapon);
            this.TryResetNativeBuyCash(player);

            if (!persisted)
            {
                MessageUtils.PrintToPlayerOrServer(this.GetUnavailableForRoundTypeMessage(), player);
                MessageUtils.Log(
                    LogLevel.Information,
                    $"Native buy pickup ignored (not allowed for current round setup). Player='{player.PlayerName}', Team='{player.Team}', RoundType='{pendingSelection.roundType}', Weapon='{weaponName}'");
            }

            return HookResult.Continue;
        }

        private bool PersistActualWeaponSelection(CCSPlayerController player, string normalizedWeapon, RoundTypeEnum roundType)
        {
            if (string.IsNullOrWhiteSpace(normalizedWeapon))
            {
                return false;
            }

            switch (roundType)
            {
                case RoundTypeEnum.Pistol:
                    return this.PersistPistolWeaponPickup(player, normalizedWeapon);
                case RoundTypeEnum.Mid:
                    return this.PersistMidWeaponPickup(player, normalizedWeapon);
                case RoundTypeEnum.FullBuy:
                    return this.PersistFullBuyWeaponPickup(player, normalizedWeapon);
            }

            return false;
        }

        private bool PersistPistolWeaponPickup(CCSPlayerController player, string normalizedWeapon)
        {
            var weapon = PistolMenu.Instance.Config.AvailableSecondaries
                .FirstOrDefault(w => this.NormalizeBuyToken(w.WeaponString).Equals(normalizedWeapon, StringComparison.OrdinalIgnoreCase)
                 && (w.Team == player.Team || w.Team == CsTeam.None));

            if (weapon == null)
            {
                return false;
            }

            CacheManager.Instance.AddOrUpdatePistolCache(player, weapon.WeaponString, player.Team);
            var dbPersisted = DBManager.Instance.InsertOrUpdatePistolWeaponString(player.SteamID, weapon.WeaponString, (int)player.Team);
            MessageUtils.Log(
                LogLevel.Information,
                $"Item pickup pistol persisted. Player='{player.PlayerName}', Team='{player.Team}', Weapon='{weapon.WeaponString}', DbPersisted='{dbPersisted}'");

            MessageUtils.PrintToPlayerOrServer(this.GetWeaponSelectedMessage(weapon.WeaponName, RoundTypeEnum.Pistol, player.Team), player);
            return true;
        }

        private bool PersistMidWeaponPickup(CCSPlayerController player, string normalizedWeapon)
        {
            var primary = MidMenu.Instance.Config.AvailablePrimaries
                .FirstOrDefault(w => this.NormalizeBuyToken(w.WeaponString).Equals(normalizedWeapon, StringComparison.OrdinalIgnoreCase)
                 && (w.Team == player.Team || w.Team == CsTeam.None));

            if (primary != null)
            {
                CacheManager.Instance.AddOrUpdateMidPrimaryCache(player, primary.WeaponString, player.Team);
                var dbPersisted = DBManager.Instance.InsertOrUpdateMidPrimaryWeaponString(player.SteamID, primary.WeaponString, (int)player.Team);
                MessageUtils.Log(
                    LogLevel.Information,
                    $"Item pickup mid primary persisted. Player='{player.PlayerName}', Team='{player.Team}', Weapon='{primary.WeaponString}', DbPersisted='{dbPersisted}'");
                
                MessageUtils.PrintToPlayerOrServer(this.GetWeaponSelectedMessage(primary.WeaponName, RoundTypeEnum.Mid, player.Team), player);
                return true;
            }

            var secondary = MidMenu.Instance.Config.AvailableSecondaries
                .FirstOrDefault(w => this.NormalizeBuyToken(w.WeaponString).Equals(normalizedWeapon, StringComparison.OrdinalIgnoreCase)
                 && (w.Team == player.Team || w.Team == CsTeam.None));

            if (secondary != null)
            {
                CacheManager.Instance.AddOrUpdateMidSecondaryCache(player, secondary.WeaponString, player.Team);
                var dbPersisted = DBManager.Instance.InsertOrUpdateMidSecondaryWeaponString(player.SteamID, secondary.WeaponString, (int)player.Team);
                MessageUtils.Log(
                    LogLevel.Information,
                    $"Item pickup mid secondary persisted. Player='{player.PlayerName}', Team='{player.Team}', Weapon='{secondary.WeaponString}', DbPersisted='{dbPersisted}'");
                
                MessageUtils.PrintToPlayerOrServer(this.GetWeaponSelectedMessage(secondary.WeaponName, RoundTypeEnum.Mid, player.Team), player);
                return true;
            }

            return false;
        }

        private bool PersistFullBuyWeaponPickup(CCSPlayerController player, string normalizedWeapon)
        {
            if (normalizedWeapon == "awp")
            {
                // AWP toggle handled by buy command (if needed)
                return false;
            }

            var fullBuyConfig = FullBuyMenu.Instance.Config;

            var primary = fullBuyConfig.AvailablePrimaries
                .FirstOrDefault(w => this.NormalizeBuyToken(w.WeaponString).Equals(normalizedWeapon, StringComparison.OrdinalIgnoreCase)
                 && (w.Team == player.Team || w.Team == CsTeam.None));

            if (primary != null)
            {
                CacheManager.Instance.AddOrUpdateFullBuyPrimaryCache(player, primary.WeaponString, player.Team);
                var dbPersisted = DBManager.Instance.InsertOrUpdateFullBuyPrimaryWeaponString(player.SteamID, primary.WeaponString, (int)player.Team);
                MessageUtils.Log(
                    LogLevel.Information,
                    $"Item pickup fullbuy primary persisted. Player='{player.PlayerName}', Team='{player.Team}', Weapon='{primary.WeaponString}', DbPersisted='{dbPersisted}'");
                
                MessageUtils.PrintToPlayerOrServer(this.GetWeaponSelectedMessage(primary.WeaponName, RoundTypeEnum.FullBuy, player.Team), player);
                return true;
            }

            var secondary = fullBuyConfig.AvailableSecondaries
                .FirstOrDefault(w => this.NormalizeBuyToken(w.WeaponString).Equals(normalizedWeapon, StringComparison.OrdinalIgnoreCase)
                 && (w.Team == player.Team || w.Team == CsTeam.None));

            if (secondary != null)
            {
                CacheManager.Instance.AddOrUpdateFullBuySecondaryCache(player, secondary.WeaponString, player.Team);
                var dbPersisted = DBManager.Instance.InsertOrUpdateFullBuySecondaryWeaponString(player.SteamID, secondary.WeaponString, (int)player.Team);
                MessageUtils.Log(
                    LogLevel.Information,
                    $"Item pickup fullbuy secondary persisted. Player='{player.PlayerName}', Team='{player.Team}', Weapon='{secondary.WeaponString}', DbPersisted='{dbPersisted}'");
                
                MessageUtils.PrintToPlayerOrServer(this.GetWeaponSelectedMessage(secondary.WeaponName, RoundTypeEnum.FullBuy, player.Team), player);
                return true;
            }

            return false;
        }

        private string ResolveNormalizedPickupWeapon(EventItemPickup pickupEvent)
        {
            if (pickupEvent.Defindex > 0 && pickupEvent.Defindex <= uint.MaxValue)
            {
                var resolvedByDefindex = this.ResolveItemDefinitionToken((uint)pickupEvent.Defindex)
                    ?? this.ResolveSelectionSlotToken((uint)pickupEvent.Defindex);

                if (!string.IsNullOrWhiteSpace(resolvedByDefindex))
                {
                    return this.NormalizeBuyToken(resolvedByDefindex);
                }
            }

            return this.NormalizeBuyToken(pickupEvent.Item ?? string.Empty);
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

        private void RegisterPendingNativeBuySelection(CCSPlayerController player)
        {
            this._pendingNativeBuySelections[player.SteamID] =
            (
                Server.CurrentTime + 2.0f,
                RoundTypeManager.Instance.RoundType,
                player.Team
            );
        }

        private bool TryGetPendingNativeBuySelection(CCSPlayerController player, out (float expiresAt, RoundTypeEnum roundType, CsTeam team) pendingSelection)
        {
            if (!this._pendingNativeBuySelections.TryGetValue(player.SteamID, out pendingSelection))
            {
                return false;
            }

            if (pendingSelection.expiresAt < Server.CurrentTime || pendingSelection.team != player.Team)
            {
                this._pendingNativeBuySelections.Remove(player.SteamID);
                return false;
            }

            return true;
        }

        private void TryResetNativeBuyCash(CCSPlayerController player)
        {
            if (player.InGameMoneyServices == null)
            {
                return;
            }

            player.InGameMoneyServices.Account = 16000;
        }

        private void TryRemovePickedWeapon(CCSPlayerController player, string normalizedWeapon)
        {
            if (player.PlayerPawn == null || !player.PlayerPawn.IsValid || player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid)
            {
                return;
            }

            var weaponService = player.PlayerPawn.Value.WeaponServices;

            if (weaponService == null)
            {
                return;
            }

            var playerWeaponService = new CCSPlayer_WeaponServices(weaponService.Handle);

            var matchingWeapons = playerWeaponService.MyWeapons
                .Where(weapon => weapon != null && weapon.IsValid && weapon.Value != null && weapon.Value.IsValid)
                .Where(weapon => this.NormalizeBuyToken(weapon.Value!.DesignerName).Equals(normalizedWeapon, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var weapon in matchingWeapons)
            {
                weapon.Value?.Remove();
            }
        }

        public void Dispose()
        {
            this._howToTimer?.Kill();
        }
    }
}
