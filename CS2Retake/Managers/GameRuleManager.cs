using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CS2Retake.Managers.Base;
using CS2Retake.Managers.Interfaces;
using CS2Retake.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS2Retake.Managers
{
    public class GameRuleManager : BaseManager, IGameRuleManager
    {
        private static GameRuleManager? _instance = null;

        public CCSGameRules? GameRules { get; set; } = null;

        public static GameRuleManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameRuleManager();
                }
                return _instance;
            }
        }

        private GameRuleManager() { }

        public bool IsWarmup
        {
            get
            {
                if (this.GameRules == null)
                {
                    this.GetGameRules();
                }

                return this.GameRules?.WarmupPeriod ?? false;
            }
        }

        public float WarmupEnd
        {
            get
            {
                if (this.GameRules == null)
                {
                    this.GetGameRules();
                }

                return this.GameRules?.WarmupPeriodEnd ?? 0f;
            }
        }

        public bool BombPlanted
        {
            get
            {
                if (this.GameRules == null)
                {
                    this.GetGameRules();
                }

                return this.GameRules?.BombPlanted ?? false;
            }

            set
            {
                if (this.GameRules == null)
                {
                    this.GetGameRules();
                }

                if (this.GameRules != null)
                {
                    this.GameRules.BombPlanted = value;
                }
            }
        }

        public bool BombDefused
        {
            get
            {
                if (this.GameRules == null)
                {
                    this.GetGameRules();
                }

                return this.GameRules?.BombDefused ?? false;
            }

            set
            {
                if (this.GameRules == null)
                {
                    this.GetGameRules();
                }

                if (this.GameRules != null)
                {
                    this.GameRules.BombDefused = value;
                }
            }
        }

        public int TotalRoundsPlayed
        {
            get
            {
                if (this.GameRules == null)
                {
                    this.GetGameRules();
                }

                return this.GameRules?.TotalRoundsPlayed ?? 0;
            }
        }

        public void TerminateRound(RoundEndReason reason = RoundEndReason.RoundDraw)
        {
            this.GameRules?.TerminateRound(0, reason);
        }

        private void GetGameRules()
        {
            //No ConfigureForRetake() here: this getter runs from OnTick while the proxy is missing,
            //and executing retake.cfg every tick restarted warmup settings in a loop. OnMapStart handles the cfg.
            MessageUtils.LogDebug($"GameRules is null. Fetching gamerule...");

            var gameRuleProxyList = this.GetGameRulesProxies();

            if (gameRuleProxyList.Count > 1)
            {
                MessageUtils.Log(LogLevel.Error, $"Multiple GameRuleProxies found. Using firstOrDefault");
            }

            var gameRuleProxy = gameRuleProxyList.FirstOrDefault();

            if (gameRuleProxy == null)
            {
                MessageUtils.Log(LogLevel.Error, $"GameRuleProxy is null");
                return;
            }

            if (gameRuleProxy.GameRules == null)
            {
                MessageUtils.Log(LogLevel.Error, $"GameRules is null");
                return;
            }

            this.GameRules = gameRuleProxy.GameRules;
        }

        private List<CCSGameRulesProxy> GetGameRulesProxies()
        {
            var gameRulesProxyList = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").ToList();

            if (!gameRulesProxyList.Any())
            {
                MessageUtils.Log(LogLevel.Error, $"No gameRuleProxy found!");
            }

            return gameRulesProxyList;
        }

        public override void ResetForNextRound(bool completeReset = true)
        {
            
        }

        public override void ResetForNextMap(bool completeReset = true)
        {

        }
    }
}
