//Vendored from CSZoneNet.Plugin.Utils 1.0.1 (see Vendor/CSZoneNet/README.md)
#nullable disable
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace CSZoneNet.Plugin.Utils.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum KevlarEnum
    {
        [EnumMember(Value = "")]
        None = 0,

        [EnumMember(Value = "item_kevlar")]
        Kevlar = 1,

        [EnumMember(Value = "item_assaultsuit")]
        KevlarHelmet = 2,
    }
}
