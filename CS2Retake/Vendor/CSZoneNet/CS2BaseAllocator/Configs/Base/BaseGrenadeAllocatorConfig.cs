//Vendored from CSZoneNet.Plugin.CS2BaseAllocator 1.0.13 (see Vendor/CSZoneNet/README.md)
#nullable disable
using CounterStrikeSharp.API.Modules.Utils;
using CSZoneNet.Plugin.CS2BaseAllocator.Configs.Interfaces;
using CSZoneNet.Plugin.CS2BaseAllocator.Entities;
using CSZoneNet.Plugin.Utils.Enums;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CSZoneNet.Plugin.CS2BaseAllocator.Configs.Base
{
    public class BaseGrenadeAllocatorConfig : IBaseAllocatorConfig
    {
        public List<GrenadeKitEntity> GrenadeKits { get; set; } = new List<GrenadeKitEntity>()
        {
            new GrenadeKitEntity(new List<GrenadeEnum>(){}),
            new GrenadeKitEntity(new List<GrenadeEnum>(){GrenadeEnum.Smoke}),
            new GrenadeKitEntity(new List<GrenadeEnum>(){GrenadeEnum.Flashbang}),
            new GrenadeKitEntity(new List<GrenadeEnum>(){GrenadeEnum.HighExplosive}),
            new GrenadeKitEntity(new List<GrenadeEnum>(){GrenadeEnum.Smoke, GrenadeEnum.Flashbang}),
            new GrenadeKitEntity(new List<GrenadeEnum>(){GrenadeEnum.HighExplosive, GrenadeEnum.Flashbang}),

            new GrenadeKitEntity(new List<GrenadeEnum>(){GrenadeEnum.Flashbang, GrenadeEnum.Flashbang}, CsTeam.CounterTerrorist),
            new GrenadeKitEntity(new List<GrenadeEnum>(){GrenadeEnum.Smoke, GrenadeEnum.HighExplosive}, CsTeam.CounterTerrorist),
            new GrenadeKitEntity(new List<GrenadeEnum>(){GrenadeEnum.Incendiary}, CsTeam.CounterTerrorist),
            new GrenadeKitEntity(new List<GrenadeEnum>(){GrenadeEnum.HighExplosive}, CsTeam.CounterTerrorist),
            new GrenadeKitEntity(new List<GrenadeEnum>(){GrenadeEnum.Flashbang}, CsTeam.CounterTerrorist),
            new GrenadeKitEntity(new List<GrenadeEnum>(){GrenadeEnum.Incendiary, GrenadeEnum.Flashbang}, CsTeam.CounterTerrorist),
            new GrenadeKitEntity(new List<GrenadeEnum>(){GrenadeEnum.Smoke, GrenadeEnum.Incendiary, GrenadeEnum.Flashbang}, CsTeam.CounterTerrorist),

            new GrenadeKitEntity(new List<GrenadeEnum>(){GrenadeEnum.Molotov}, CsTeam.Terrorist),
            new GrenadeKitEntity(new List<GrenadeEnum>(){GrenadeEnum.Molotov, GrenadeEnum.Flashbang }, CsTeam.Terrorist),
            new GrenadeKitEntity(new List<GrenadeEnum>(){GrenadeEnum.Smoke, GrenadeEnum.Flashbang }, CsTeam.Terrorist),
        };

        public int Version { get; set; }

        [JsonIgnore]
        public string AllocatorConfigDirectoryPath { get; set; } = string.Empty;

        [JsonIgnore]
        public string AllocatorConfigPath { get; set; } = string.Empty;
    }
}
