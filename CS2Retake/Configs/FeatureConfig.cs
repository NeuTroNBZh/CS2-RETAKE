using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CS2Retake.Utils;

namespace CS2Retake.Configs
{
    public static class FeatureConfig
    {
        public static RetakeMessageLanguageEnum MessageLanguage { get; set; } = RetakeMessageLanguageEnum.English;
        public static bool EnableSpotAnnouncer { get; set; } = true;
        public static bool EnableQueue { get; set; } = true;
        public static bool EnableScramble { get; set; } = true;
        public static bool EnableSwitchOnRoundWin { get; set; } = true;

        public static bool EnableThankYouMessage { get; set; } = false;

        public static bool InstaDefuseEnabled { get; set; } = true;
        public static bool InstaDefuseRequireNoTAlive { get; set; } = true;
        public static bool InstaDefuseBlockOnHe { get; set; } = true;
        public static bool InstaDefuseBlockOnMolotov { get; set; } = true;
        public static bool InstaDefuseBlockOnInferno { get; set; } = true;
        public static float InstaDefuseInfernoDistance { get; set; } = 250.0f;
        public static bool InstaDefuseForceExplodeIfNoTime { get; set; } = true;
        public static bool InstaDefuseChatNotification { get; set; } = true;

        public static bool EnableDebug { get; set; } = false;

        public static void SetBaseConfig(CS2RetakeConfig baseConfig)
        {
            MessageLanguage = baseConfig.MessageLanguage;
            EnableSpotAnnouncer = baseConfig.SpotAnnouncerEnabled;
            EnableQueue = baseConfig.EnableQueue;
            EnableScramble = baseConfig.EnableScramble;
            EnableSwitchOnRoundWin = baseConfig.EnableSwitchOnRoundWin;

            EnableThankYouMessage = baseConfig.EnableThankYouMessage;

            InstaDefuseEnabled = baseConfig.InstaDefuseEnabled;
            InstaDefuseRequireNoTAlive = baseConfig.InstaDefuseRequireNoTAlive;
            InstaDefuseBlockOnHe = baseConfig.InstaDefuseBlockOnHe;
            InstaDefuseBlockOnMolotov = baseConfig.InstaDefuseBlockOnMolotov;
            InstaDefuseBlockOnInferno = baseConfig.InstaDefuseBlockOnInferno;
            InstaDefuseInfernoDistance = baseConfig.InstaDefuseInfernoDistance;
            InstaDefuseForceExplodeIfNoTime = baseConfig.InstaDefuseForceExplodeIfNoTime;
            InstaDefuseChatNotification = baseConfig.InstaDefuseChatNotification;

            EnableDebug = baseConfig.EnableDebug;
        }
    }
}
