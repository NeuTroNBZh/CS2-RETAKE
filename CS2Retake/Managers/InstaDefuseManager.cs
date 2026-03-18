using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using CS2Retake.Configs;
using CS2Retake.Managers.Base;
using CS2Retake.Utils;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace CS2Retake.Managers
{
    public class InstaDefuseManager : BaseManager
    {
        private static InstaDefuseManager? _instance;

        private float _bombPlantedTime = float.NaN;
        private bool _bombTicking;
        private int _molotovThreat;
        private int _heThreat;
        private HashSet<int> _infernoThreat = new();
        private bool _attributeHandlersRegistered;

        public static InstaDefuseManager Instance
        {
            get
            {
                _instance ??= new InstaDefuseManager();
                return _instance;
            }
        }

        private InstaDefuseManager()
        {
        }

        public void Initialize(CS2Retake plugin)
        {
            if (!_attributeHandlersRegistered)
            {
                plugin.RegisterAllAttributes(this);
                _attributeHandlersRegistered = true;
            }

            ResetForNextMap();
        }

        [GameEventHandler]
        public HookResult OnGrenadeThrown(EventGrenadeThrown @event, GameEventInfo info)
        {
            if (!IsActiveRetakeRound())
            {
                return HookResult.Continue;
            }

            var weapon = @event.Weapon;

            if (FeatureConfig.InstaDefuseBlockOnHe && weapon == "hegrenade")
            {
                _heThreat++;
            }
            else if (FeatureConfig.InstaDefuseBlockOnMolotov && (weapon == "incgrenade" || weapon == "molotov"))
            {
                _molotovThreat++;
            }
            else
            {
                return HookResult.Continue;
            }

            LogThreatLevel();

            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnInfernoStartBurn(EventInfernoStartburn @event, GameEventInfo info)
        {
            if (!IsActiveRetakeRound() || !FeatureConfig.InstaDefuseBlockOnInferno)
            {
                return HookResult.Continue;
            }

            var plantedBomb = FindPlantedBomb();
            if (plantedBomb == null)
            {
                return HookResult.Continue;
            }

            var plantedBombOrigin = plantedBomb.CBodyComponent?.SceneNode?.AbsOrigin;
            if (plantedBombOrigin == null)
            {
                return HookResult.Continue;
            }

            var infernoPosition = new Vector3(@event.X, @event.Y, @event.Z);
            var bombPosition = new Vector3(plantedBombOrigin.X, plantedBombOrigin.Y, plantedBombOrigin.Z);

            if (Vector3.Distance(infernoPosition, bombPosition) > FeatureConfig.InstaDefuseInfernoDistance)
            {
                return HookResult.Continue;
            }

            _infernoThreat.Add(@event.Entityid);
            LogThreatLevel();

            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnInfernoExtinguish(EventInfernoExtinguish @event, GameEventInfo info)
        {
            _infernoThreat.Remove(@event.Entityid);
            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnInfernoExpire(EventInfernoExpire @event, GameEventInfo info)
        {
            _infernoThreat.Remove(@event.Entityid);
            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnHeGrenadeDetonate(EventHegrenadeDetonate @event, GameEventInfo info)
        {
            if (_heThreat > 0)
            {
                _heThreat--;
            }

            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnMolotovDetonate(EventMolotovDetonate @event, GameEventInfo info)
        {
            if (_molotovThreat > 0)
            {
                _molotovThreat--;
            }

            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            ResetForNextRound();
            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnBombPlanted(EventBombPlanted @event, GameEventInfo info)
        {
            if (!IsActiveRetakeRound())
            {
                return HookResult.Continue;
            }

            _bombPlantedTime = Server.CurrentTime;
            _bombTicking = true;
            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnBombBeginDefuse(EventBombBegindefuse @event, GameEventInfo info)
        {
            var player = @event.Userid;

            if (player == null || !player.IsValid || !player.PawnIsAlive)
            {
                return HookResult.Continue;
            }

            AttemptInstantDefuse(player);

            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnBombDefused(EventBombDefused @event, GameEventInfo info)
        {
            _bombTicking = false;
            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnBombExploded(EventBombExploded @event, GameEventInfo info)
        {
            _bombTicking = false;
            return HookResult.Continue;
        }

        private void AttemptInstantDefuse(CCSPlayerController defuser)
        {
            if (!IsActiveRetakeRound() || !FeatureConfig.InstaDefuseEnabled)
            {
                return;
            }

            if (!_bombTicking)
            {
                MessageUtils.LogDebug("InstaDefuse skipped because no bomb is ticking.");
                return;
            }

            var plantedBomb = FindPlantedBomb();
            if (plantedBomb == null)
            {
                MessageUtils.LogDebug("InstaDefuse skipped because planted bomb entity was not found.");
                return;
            }

            if (plantedBomb.CannotBeDefused)
            {
                MessageUtils.LogDebug("InstaDefuse skipped because the bomb cannot be defused.");
                return;
            }

            if (FeatureConfig.InstaDefuseRequireNoTAlive && TeamHasAlivePlayers(CsTeam.Terrorist))
            {
                MessageUtils.LogDebug("InstaDefuse skipped because terrorists are still alive.");
                return;
            }

            if (HasActiveThreat())
            {
                NotifyAll(BuildThreatBlockedMessage());
                return;
            }

            var bombTimeUntilDetonation = plantedBomb.TimerLength - (Server.CurrentTime - _bombPlantedTime);
            var defuseLength = plantedBomb.DefuseLength;
            if (defuseLength != 5.0f && defuseLength != 10.0f)
            {
                defuseLength = defuser.PawnHasDefuser ? 5.0f : 10.0f;
            }

            var timeLeftAfterDefuse = bombTimeUntilDetonation - defuseLength;
            if (timeLeftAfterDefuse < 0.0f)
            {
                NotifyAll(BuildNotEnoughTimeMessage(defuser.PlayerName, Math.Abs(timeLeftAfterDefuse)));

                if (FeatureConfig.InstaDefuseForceExplodeIfNoTime)
                {
                    Server.NextFrame(() =>
                    {
                        var currentBomb = FindPlantedBomb();
                        if (currentBomb == null)
                        {
                            return;
                        }

                        currentBomb.C4Blow = 1.0f;
                    });
                }

                return;
            }

            Server.NextFrame(() =>
            {
                var currentBomb = FindPlantedBomb();
                if (currentBomb == null)
                {
                    return;
                }

                currentBomb.DefuseCountDown = 0;
                NotifyAll(BuildSuccessfulMessage(defuser.PlayerName, Math.Abs(bombTimeUntilDetonation)));
            });
        }

        private bool IsActiveRetakeRound()
        {
            return FeatureConfig.InstaDefuseEnabled && !GameRuleManager.Instance.IsWarmup;
        }

        private bool HasActiveThreat()
        {
            return (FeatureConfig.InstaDefuseBlockOnHe && _heThreat > 0)
                || (FeatureConfig.InstaDefuseBlockOnMolotov && _molotovThreat > 0)
                || (FeatureConfig.InstaDefuseBlockOnInferno && _infernoThreat.Count > 0);
        }

        private static bool TeamHasAlivePlayers(CsTeam team)
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (!player.IsValid)
                {
                    continue;
                }

                if (player.Team != team)
                {
                    continue;
                }

                if (!player.PawnIsAlive)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static CPlantedC4? FindPlantedBomb()
        {
            return Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
        }

        private void LogThreatLevel()
        {
            MessageUtils.LogDebug("InstaDefuse threats: HE [{heThreat}], Molotov [{molotovThreat}], Inferno [{infernoThreat}]", _heThreat, _molotovThreat, _infernoThreat.Count);
        }

        private void NotifyAll(string message)
        {
            if (!FeatureConfig.InstaDefuseChatNotification)
            {
                return;
            }

            MessageUtils.PrintToChatAll(message);
        }

        private static string BuildThreatBlockedMessage()
        {
            return FeatureConfig.MessageLanguage == RetakeMessageLanguageEnum.French
                ? "Instadefuse impossible : une menace HE/molotov/inferno est active."
                : "Instant defuse is blocked while an HE, molotov, or inferno threat is active.";
        }

        private static string BuildNotEnoughTimeMessage(string playerName, float missingSeconds)
        {
            return FeatureConfig.MessageLanguage == RetakeMessageLanguageEnum.French
                ? $"{ChatColors.DarkBlue}{playerName}{ChatColors.White} ne peut pas instadefuse, il manque {ChatColors.DarkRed}{missingSeconds:n3}s{ChatColors.White}."
                : $"{ChatColors.DarkBlue}{playerName}{ChatColors.White} cannot instant defuse, missing {ChatColors.DarkRed}{missingSeconds:n3}s{ChatColors.White}.";
        }

        private static string BuildSuccessfulMessage(string playerName, float remainingSeconds)
        {
            return FeatureConfig.MessageLanguage == RetakeMessageLanguageEnum.French
                ? $"{ChatColors.DarkBlue}{playerName}{ChatColors.White} a instadefuse avec {ChatColors.Green}{remainingSeconds:n3}s{ChatColors.White} restantes."
                : $"{ChatColors.DarkBlue}{playerName}{ChatColors.White} instant defused with {ChatColors.Green}{remainingSeconds:n3}s{ChatColors.White} remaining.";
        }

        public override void ResetForNextRound(bool completeReset = true)
        {
            _bombPlantedTime = float.NaN;
            _bombTicking = false;
            _heThreat = 0;
            _molotovThreat = 0;
            _infernoThreat = new HashSet<int>();
        }

        public override void ResetForNextMap(bool completeReset = true)
        {
            ResetForNextRound(completeReset);
        }
    }
}