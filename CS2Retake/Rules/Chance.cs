namespace CS2Retake.Rules
{
    public static class Chance
    {
        public static bool Roll(double chancePercent, Random random)
        {
            if (chancePercent <= 0.0d)
            {
                return false;
            }

            if (chancePercent >= 100.0d)
            {
                return true;
            }

            return random.NextDouble() * 100.0d <= chancePercent;
        }
    }
}
