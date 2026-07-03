using CounterStrikeSharp.API.Modules.Utils;
using CS2Retake.Allocators.Implementations.CommandAllocator.Entities;
using CSZoneNet.Plugin.CS2BaseAllocator.Configs.Base;


namespace CS2Retake.Allocators.Implementations.CommandAllocator.Configs
{
    public class FullBuyConfig : BaseAllocatorConfig
    {
        public List<WeaponEntity> AvailablePrimaries { get; set; } = new List<WeaponEntity>()
        {
            new WeaponEntity("M4A4", "weapon_m4a1", CsTeam.CounterTerrorist),
            new WeaponEntity("M4A1-s", "weapon_m4a1_silencer", CsTeam.CounterTerrorist),
            new WeaponEntity("Famas", "weapon_famas", CsTeam.CounterTerrorist),
            new WeaponEntity("AUG", "weapon_aug", CsTeam.CounterTerrorist),
            new WeaponEntity("MP-9", "weapon_mp9", CsTeam.CounterTerrorist),
            new WeaponEntity("SCAR-20", "weapon_scar20", CsTeam.CounterTerrorist),
            new WeaponEntity("MAG-7", "weapon_mag7", CsTeam.CounterTerrorist),

            new WeaponEntity("AK-47", "weapon_ak47", CsTeam.Terrorist),
            new WeaponEntity("Galil", "weapon_galilar", CsTeam.Terrorist),
            new WeaponEntity("SG-553", "weapon_sg556", CsTeam.Terrorist),
            new WeaponEntity("Mac-10", "weapon_mac10", CsTeam.Terrorist),
            new WeaponEntity("G3SG1", "weapon_g3sg1", CsTeam.Terrorist),
            new WeaponEntity("Sawed-Off", "weapon_sawedoff", CsTeam.Terrorist),

            new WeaponEntity("MP7", "weapon_mp7"),
            new WeaponEntity("MP5-SD", "weapon_mp5sd"),
            new WeaponEntity("UMP-45", "weapon_ump45"),
            new WeaponEntity("P90", "weapon_p90"),
            new WeaponEntity("PP-Bizon", "weapon_bizon"),
            new WeaponEntity("SSG 08", "weapon_ssg08"),
            new WeaponEntity("Nova", "weapon_nova"),
            new WeaponEntity("XM1014", "weapon_xm1014"),
            new WeaponEntity("M249", "weapon_m249"),
            new WeaponEntity("Negev", "weapon_negev"),

        };

        public List<WeaponEntity> AvailableSecondaries { get; set; } = new List<WeaponEntity>()
        {
            new WeaponEntity("Deagle", "weapon_deagle"),
            new WeaponEntity("P250", "weapon_p250"),
            new WeaponEntity("CZ75", "weapon_cz75a"),
            new WeaponEntity("Dual Berettas", "weapon_elite"),

            new WeaponEntity("USP-s", "weapon_usp_silencer", CsTeam.CounterTerrorist),           
            new WeaponEntity("P2000", "weapon_hkp2000", CsTeam.CounterTerrorist),
            new WeaponEntity("FiveSeven", "weapon_fiveseven", CsTeam.CounterTerrorist),

            new WeaponEntity("Glock", "weapon_glock", CsTeam.Terrorist),
            new WeaponEntity("Tec-9", "weapon_tec9", CsTeam.Terrorist),
            new WeaponEntity("R8 Revolver", "weapon_revolver"),
        };

        public ChanceEntity AWPChanceCT { get; set; } = new ChanceEntity()
        {
            Team = CsTeam.CounterTerrorist,
            WeaponName = "AWP",
            WeaponString = "weapon_awp",
            Limit = 1,
            Chances = new List<int>() { 30 },
        };

        public ChanceEntity AWPChanceT { get; set; } = new ChanceEntity()
        {
            Team = CsTeam.Terrorist,
            WeaponName = "AWP",
            WeaponString = "weapon_awp",
            Limit = 1,
            Chances = new List<int>() { 30 },
        };

        public bool EnableAWPChance { get; set; } = true;

        public FullBuyConfig()
        {
            this.Version = 3;
        }
    }
}
