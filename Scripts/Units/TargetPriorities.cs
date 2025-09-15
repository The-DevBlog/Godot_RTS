using System.Collections.Generic;
using MyEnums;

public static class TargetPriorities
{
    public static readonly Dictionary<WeaponType, Dictionary<UnitClass, int>> Priorities = new()
    {
        {
            WeaponType.Cannon, new Dictionary<UnitClass, int>
            {
                { UnitClass.HeavyVehicle, 1 },
                { UnitClass.LightVehicle, 1 },
                { UnitClass.Infantry, 2 },
            }
        },
        {
            WeaponType.SmallArms, new Dictionary<UnitClass, int>
            {
                { UnitClass.Infantry, 1 },
                { UnitClass.LightVehicle, 2 },
                { UnitClass.HeavyVehicle, 3 },
            }
        }
    };

    public static int GetPriority(WeaponType wt, UnitClass uc, int @default = 1000)
    {
        if (Priorities.TryGetValue(wt, out var map) &&
            map.TryGetValue(uc, out var p))
            return p;
        return @default; // unknown class/weapon => worst
    }
}
