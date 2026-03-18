using System.Text.Json.Serialization;

namespace CS2Retake.Utils
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RetakeMessageLanguageEnum
    {
        English = 0,
        French = 1,
    }
}