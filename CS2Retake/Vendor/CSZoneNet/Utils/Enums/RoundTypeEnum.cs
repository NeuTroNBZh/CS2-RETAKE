//Vendored from CSZoneNet.Plugin.Utils 1.0.1 (see Vendor/CSZoneNet/README.md)
#nullable disable
using System.Text.Json.Serialization;

namespace CSZoneNet.Plugin.Utils.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RoundTypeEnum
    {
        Undefined = -1,
        Pistol = 0,
        Mid = 1,
        FullBuy = 2,
    }
}
