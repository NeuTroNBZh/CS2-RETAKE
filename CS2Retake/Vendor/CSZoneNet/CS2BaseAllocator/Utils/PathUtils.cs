//Vendored from CSZoneNet.Plugin.CS2BaseAllocator 1.0.13 (see Vendor/CSZoneNet/README.md)
#nullable disable

namespace CSZoneNet.Plugin.CS2BaseAllocator.Utils
{
    public static class PathUtils
    {
        public static DirectoryInfo CounterStrikeSharpRootDirectoryInfo => new FileInfo(typeof(CounterStrikeSharp.API.Bootstrap).Assembly.Location).Directory?.Parent;
        public static string CounterStrikeSharpRootDirectoryPath => CounterStrikeSharpRootDirectoryInfo?.FullName ?? string.Empty;
        public static string AllocatorConfigDirectory => Path.Combine(CounterStrikeSharpRootDirectoryPath, "configs", "allocators");


    }
}
