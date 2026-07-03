//Vendored from CSZoneNet.Plugin.Utils 1.0.1 (see Vendor/CSZoneNet/README.md)
#nullable disable
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace CSZoneNet.Plugin.Utils.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum GrenadeEnum
    {
        [EnumMember(Value = "weapon_smokegrenade")]
        Smoke = 0,

        [EnumMember(Value = "weapon_flashbang")]
        Flashbang = 1,

        [EnumMember(Value = "weapon_hegrenade")]
        HighExplosive = 2,

        [EnumMember(Value = "weapon_molotov")]
        Molotov = 3,

        [EnumMember(Value = "weapon_incgrenade")]
        Incendiary = 4,

        [EnumMember(Value = "weapon_decoy")]
        Decoy = 5,

        //Frag Grenade (Danger Zone)
        [EnumMember(Value = "weapon_frag")]
        Frag = 6,

        //Tactical Awareness Grenade (Operation Wildfire)
        [EnumMember(Value = "weapon_tagrenade")]
        TacticalAwareness = 7,
    }
}
