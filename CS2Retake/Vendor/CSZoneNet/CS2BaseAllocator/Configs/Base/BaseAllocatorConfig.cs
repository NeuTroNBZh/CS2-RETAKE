//Vendored from CSZoneNet.Plugin.CS2BaseAllocator 1.0.13 (see Vendor/CSZoneNet/README.md)
#nullable disable
using CSZoneNet.Plugin.CS2BaseAllocator.Configs.Interfaces;
using System.Text.Json.Serialization;

namespace CSZoneNet.Plugin.CS2BaseAllocator.Configs.Base
{
    public class BaseAllocatorConfig : IBaseAllocatorConfig
    {
        public int Version { get; set; }

        [JsonIgnore]
        public string AllocatorConfigDirectoryPath { get; set; } = string.Empty;

        [JsonIgnore]
        public string AllocatorConfigPath { get; set; } = string.Empty;
    }
}
