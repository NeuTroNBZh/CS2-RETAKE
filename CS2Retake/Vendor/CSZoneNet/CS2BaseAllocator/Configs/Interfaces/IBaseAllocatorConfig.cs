//Vendored from CSZoneNet.Plugin.CS2BaseAllocator 1.0.13 (see Vendor/CSZoneNet/README.md)
#nullable disable

namespace CSZoneNet.Plugin.CS2BaseAllocator.Configs.Interfaces
{
    public interface IBaseAllocatorConfig
    {
        int Version { get; set; }


        string AllocatorConfigDirectoryPath { get; set; }

        string AllocatorConfigPath { get; set; }
    }
}
