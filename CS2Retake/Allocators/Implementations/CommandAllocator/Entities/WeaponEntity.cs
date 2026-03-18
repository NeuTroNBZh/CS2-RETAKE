using CounterStrikeSharp.API.Modules.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS2Retake.Allocators.Implementations.CommandAllocator.Entities
{   
    public class WeaponEntity
    {
        public string WeaponName { get; set; } = string.Empty;
        public string WeaponString { get; set; } = string.Empty;
        public CsTeam Team { get; set; } = CsTeam.None;

        public WeaponEntity() { }

        public WeaponEntity(string weaponName, string weaponString, CsTeam team = CsTeam.None)
        {
            this.WeaponName = weaponName;
            this.WeaponString = weaponString;
            this.Team = team;
        }

    }
}
