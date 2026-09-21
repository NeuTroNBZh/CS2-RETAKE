using CS2Retake.Entities;
using CSZoneNet.Plugin.Utils.Enums;

namespace CS2Retake.Rules
{
    public static class RoundTypeSequence
    {
        public static IReadOnlyList<RoundTypeEnum> Build(IEnumerable<RoundTypeSequenceEntity> sequence, int maxRounds)
        {
            var list = new List<RoundTypeEnum>();

            foreach (var entry in sequence)
            {
                var amount = entry.AmountOfRounds < 0
                    ? maxRounds - list.Count + 1
                    : entry.AmountOfRounds;

                list.AddRange(Enumerable.Repeat(entry.RoundType, Math.Max(0, amount)));
            }

            return list.AsReadOnly();
        }

        public static RoundTypeEnum? TryGetAt(IReadOnlyList<RoundTypeEnum> list, int totalRoundsPlayed, int maxRounds)
        {
            if (totalRoundsPlayed > maxRounds || totalRoundsPlayed >= list.Count)
            {
                return null;
            }

            return list[totalRoundsPlayed];
        }
    }
}
