namespace CS2Retake.Rules
{
    public static class AwpRules
    {
        public const int MinActivePlayers = 5;

        public static bool IsLobbyEligible(int activePlayers) => activePlayers >= MinActivePlayers;

        public static T? PickRecipient<T>(IReadOnlyList<T> volunteers, Func<T, int> chanceOf, Random random)
            where T : class
        {
            if (volunteers.Count == 0)
            {
                return null;
            }

            var selected = volunteers[random.Next(volunteers.Count)];

            return Chance.Roll(chanceOf(selected), random) ? selected : null;
        }
    }
}
