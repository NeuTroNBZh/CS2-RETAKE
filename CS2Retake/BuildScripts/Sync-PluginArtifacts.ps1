param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectDir,

    [Parameter(Mandatory = $true)]
    [string]$TargetDir,

    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$WorkspaceRoot
)

$ErrorActionPreference = 'Stop'

$pluginRoot = Join-Path $WorkspaceRoot 'plugin'
$releaseRoot = Join-Path $ProjectDir (Join-Path '..' (Join-Path 'release' ("v{0}" -f $Version)))

$pkgWithConfigsPlugins = Join-Path $pluginRoot 'pkg-with-configs/addons/counterstrikesharp/plugins/CS2Retake'
$pkgNoConfigsPlugins = Join-Path $pluginRoot 'pkg-no-configs/addons/counterstrikesharp/plugins/CS2Retake'
$pkgWithConfigsConfigRoot = Join-Path $pluginRoot 'pkg-with-configs/addons/counterstrikesharp/configs/plugins/CS2Retake'
$configBaseRoot = Join-Path $pluginRoot 'config-base/addons/counterstrikesharp/configs/plugins/CS2Retake'
$configAllocatorRoot = Join-Path $pluginRoot 'config-allocator/addons/counterstrikesharp/configs/plugins/CS2Retake'
$pkgWithConfigsRoot = Join-Path $pluginRoot 'pkg-with-configs'
$pkgNoConfigsRoot = Join-Path $pluginRoot 'pkg-no-configs'

$templatePkgWithConfigsConfigRoot = Join-Path $releaseRoot 'pkg-with-configs/addons/counterstrikesharp/configs/plugins/CS2Retake'
$templateConfigBaseRoot = Join-Path $releaseRoot 'config-base/addons/counterstrikesharp/configs/plugins/CS2Retake'
$templateConfigAllocatorRoot = Join-Path $releaseRoot 'config-allocator/addons/counterstrikesharp/configs/plugins/CS2Retake'
$projectSpawnsRoot = Join-Path $ProjectDir 'spawns'
$projectCfgRoot = Join-Path $ProjectDir 'cfg'
$templateSpawnsRoot = Join-Path $releaseRoot 'pkg-with-configs/addons/counterstrikesharp/plugins/CS2Retake/spawns'

$targetsToReset = @(
    $pkgWithConfigsPlugins,
    $pkgNoConfigsPlugins,
    $pkgWithConfigsConfigRoot,
    $configBaseRoot,
    $configAllocatorRoot
)

foreach ($path in $targetsToReset) {
    if (Test-Path $path) {
        Remove-Item -Path $path -Recurse -Force
    }
    New-Item -Path $path -ItemType Directory -Force | Out-Null
}

New-Item -Path $pkgWithConfigsRoot -ItemType Directory -Force | Out-Null
New-Item -Path $pkgNoConfigsRoot -ItemType Directory -Force | Out-Null

Copy-Item -Path (Join-Path $TargetDir '*') -Destination $pkgWithConfigsPlugins -Recurse -Force
Copy-Item -Path (Join-Path $TargetDir '*') -Destination $pkgNoConfigsPlugins -Recurse -Force

if (Test-Path $projectSpawnsRoot) {
    Copy-Item -Path $projectSpawnsRoot -Destination $pkgWithConfigsPlugins -Recurse -Force
    Copy-Item -Path $projectSpawnsRoot -Destination $pkgNoConfigsPlugins -Recurse -Force
}
elseif (Test-Path $templateSpawnsRoot) {
    Copy-Item -Path $templateSpawnsRoot -Destination $pkgWithConfigsPlugins -Recurse -Force
    Copy-Item -Path $templateSpawnsRoot -Destination $pkgNoConfigsPlugins -Recurse -Force
}

if (Test-Path $projectCfgRoot) {
    Copy-Item -Path $projectCfgRoot -Destination $pkgWithConfigsRoot -Recurse -Force
    Copy-Item -Path $projectCfgRoot -Destination $pkgNoConfigsRoot -Recurse -Force
}

if (-not (Test-Path $templatePkgWithConfigsConfigRoot)) {
    throw "Missing template configs for pkg-with-configs: $templatePkgWithConfigsConfigRoot"
}
if (-not (Test-Path $templateConfigBaseRoot)) {
    throw "Missing template configs for config-base: $templateConfigBaseRoot"
}
if (-not (Test-Path $templateConfigAllocatorRoot)) {
    throw "Missing template configs for config-allocator: $templateConfigAllocatorRoot"
}

Copy-Item -Path (Join-Path $templatePkgWithConfigsConfigRoot '*') -Destination $pkgWithConfigsConfigRoot -Recurse -Force
Copy-Item -Path (Join-Path $templateConfigBaseRoot '*') -Destination $configBaseRoot -Recurse -Force
Copy-Item -Path (Join-Path $templateConfigAllocatorRoot '*') -Destination $configAllocatorRoot -Recurse -Force

$zipSpecs = @(
    @{ Name = "CS2-RETAKE-$Version.zip"; Source = (Join-Path $pluginRoot 'pkg-with-configs/*') },
    @{ Name = "CS2-RETAKE-$Version-linux.zip"; Source = (Join-Path $pluginRoot 'pkg-with-configs/*') },
    @{ Name = "CS2-RETAKE-$Version-no_configs.zip"; Source = (Join-Path $pluginRoot 'pkg-no-configs/*') },
    @{ Name = "CS2-RETAKE-$Version-linux-no_configs.zip"; Source = (Join-Path $pluginRoot 'pkg-no-configs/*') },
    @{ Name = "CS2-RETAKE-$Version-base_config.zip"; Source = (Join-Path $pluginRoot 'config-base/*') },
    @{ Name = "CS2-RETAKE-$Version-allocator_config.zip"; Source = (Join-Path $pluginRoot 'config-allocator/*') }
)

foreach ($zip in $zipSpecs) {
    $zipPath = Join-Path $pluginRoot $zip.Name
    if (Test-Path $zipPath) {
        Remove-Item -Path $zipPath -Force
    }

    Compress-Archive -Path $zip.Source -DestinationPath $zipPath -CompressionLevel Optimal -Force
}

Write-Host "Plugin artifacts synchronized to: $pluginRoot"
