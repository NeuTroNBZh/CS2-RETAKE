//Vendored from CSZoneNet.Plugin.CS2BaseAllocator 1.0.13 (see Vendor/CSZoneNet/README.md)
#nullable disable

namespace CSZoneNet.Plugin.CS2BaseAllocator.Configs.Interfaces
{
    public interface IAllocatorConfig<T> where T: IBaseAllocatorConfig, new()
    {
        T Config { get; set; }

        public void OnAllocatorConfigParsed(T config);
    }
}
