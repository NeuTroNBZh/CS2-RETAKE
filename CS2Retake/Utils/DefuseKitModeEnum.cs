using System.Text.Json.Serialization;

namespace CS2Retake.Utils
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DefuseKitModeEnum
    {
        All = 0,
        Quota = 1,
        Chance = 2,
    }
}