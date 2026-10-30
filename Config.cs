using Exiled.API.Interfaces;
using PlayerRoles;
using System.Collections.Generic;

namespace ScpFreezer
{
    public class Config : IConfig
    {
        public const float MaximumFreezeDuration = 3600f;
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;

        public float FreezeDuration { get; set; } = 20f;
        public string FreezeMessage { get; set; } = "<i>Systemy startowe... Pozostaniesz w przechowalni jeszcze przez <color=red>%time%</color> sekund.</i>";

        public Dictionary<RoleTypeId, bool> ScpFreezeSettings { get; set; } = new Dictionary<RoleTypeId, bool>
        {
            { RoleTypeId.Scp049, true },
            { RoleTypeId.Scp0492, false },
            { RoleTypeId.Scp079, false },
            { RoleTypeId.Scp096, true },
            { RoleTypeId.Scp106, true },
            { RoleTypeId.Scp173, true },
            { RoleTypeId.Scp939, true },
            { RoleTypeId.Scp3114, false }
        };
    }
}
