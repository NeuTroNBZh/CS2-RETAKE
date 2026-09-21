using CounterStrikeSharp.API.Modules.Cvars;
using CS2Retake.Configs;
using CS2Retake.Managers.Base;
using CS2Retake.Managers.Interfaces;
using CS2Retake.Rules;
using CS2Retake.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSZoneNet.Plugin.Utils.Enums;

namespace CS2Retake.Managers
{
    public class RoundTypeManager : BaseManager, IRoundTypeManager
    {
        private static RoundTypeManager? _instance = null;

        public RoundTypeEnum RoundType { get; private set; } = RoundTypeEnum.Undefined;
        private IReadOnlyList<RoundTypeEnum> _roundTypeList = Array.Empty<RoundTypeEnum>();

        public static RoundTypeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RoundTypeManager();
                }
                return _instance;
            }
        }
        private RoundTypeManager() { }


        public void HandleRoundType()
        {
            switch (RuntimeConfig.RoundTypeMode)
            {
                case RoundTypeModeEnum.Random:
                    this.HandleRandomRoundType();
                    break;
                case RoundTypeModeEnum.Specific:
                    this.HandleSpecificRoundType();
                    break;
                case RoundTypeModeEnum.Sequence:
                    this.HandleSequenceRoundType();
                    break;
                default:
                    this.HandleRandomRoundType();
                    break;
            }
        }

        public void ResetSequenceRoundType()
        {
            this.CreateRoundTypeList();
        }

        private void HandleRandomRoundType()
        {
            this.RoundType = (RoundTypeEnum)new Random().Next(0, Enum.GetNames(typeof(RoundTypeEnum)).Length - 1);
        }

        private void HandleSpecificRoundType()
        {
            this.RoundType = RuntimeConfig.RoundTypeSpecific;
        }

        private void HandleSequenceRoundType()
        {
            if (!this._roundTypeList.Any())
            {
                this.CreateRoundTypeList();
            }

            var totalRoundsPlayed = GameRuleManager.Instance.TotalRoundsPlayed;
            var maxRounds = ConVar.Find("mp_maxrounds")!.GetPrimitiveValue<int>();

            MessageUtils.LogDebug($"TotalRoundsPlayed: {totalRoundsPlayed} - MaxRounds: {maxRounds}");

            var roundType = RoundTypeSequence.TryGetAt(this._roundTypeList, totalRoundsPlayed, maxRounds);

            if (roundType.HasValue)
            {
                this.RoundType = roundType.Value;
            }
        }

        private void CreateRoundTypeList()
        {
            var maxRounds = ConVar.Find("mp_maxrounds")!.GetPrimitiveValue<int>();

            MessageUtils.LogDebug($"MaxRounds: {maxRounds}");

            this._roundTypeList = RoundTypeSequence.Build(RuntimeConfig.RoundTypeSequence, maxRounds);
        }

        public override void ResetForNextRound(bool completeReset = true)
        {
            if (completeReset)
            {

            }

        }

        public override void ResetForNextMap(bool completeReset = true)
        {
            if (completeReset)
            {
                this.RoundType = RuntimeConfig.RoundTypeSpecific;
                this._roundTypeList = Array.Empty<RoundTypeEnum>();
            }
        }
    }
}
