//Vendored from CSZoneNet.Plugin.CS2BaseAllocator 1.0.13 (see Vendor/CSZoneNet/README.md)
#nullable disable
using CounterStrikeSharp.API.Core;
using CSZoneNet.Plugin.Utils.Enums;
using System;
using System.Collections.Generic;

namespace CSZoneNet.Plugin.CS2BaseAllocator.Interfaces
{
    public interface IBaseAllocator
    {
        (string primaryWeapon, string secondaryWeapon, KevlarEnum kevlar, bool kit, bool zeus, List<GrenadeEnum> grenades) Allocate(CCSPlayerController player, RoundTypeEnum roundType = RoundTypeEnum.Undefined);

        void InitializeConfig(object instance, Type allocatorType);
        void InjectBasePluginInstance(IPlugin basePluginInstance);
        void OnGunsCommand(CCSPlayerController player);
        void OnPlayerConnected(CCSPlayerController player);
        void OnPlayerDisconnected(CCSPlayerController player);

        void ResetForNextRound(bool completeReset = true);
    }
}
