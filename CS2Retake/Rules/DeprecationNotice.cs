namespace CS2Retake.Rules
{
    public static class DeprecationNotice
    {
        public const string RetakeV4Url = "https://github.com/NeuTroNBZh/CS2-RetakeV4";

        public static string Message =>
            $"CS2-RETAKE (V3) is deprecated and will be archived. Please migrate to RetakeV4: {RetakeV4Url} (V3 spawns and weapon preferences can be imported). Do not run both plugins together.";
    }
}
