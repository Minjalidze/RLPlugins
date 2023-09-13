using System.Collections.Generic;
using RustExtended;
using UnityEngine;
using System;

namespace Oxide.Plugins
{
    [Info("SXCZCeilingRaid", "systemXcrackedZ", "1.0.0")]
    internal class SXCZCeilingRaid : RustLegacyPlugin
    {
        object ModifyDamage(TakeDamage take, DamageEvent damage)
        {
            try
            {
                if (damage.victim.idMain is StructureComponent && damage.victim.idMain.name.ToLower().Contains("ceiling") && damage.damageTypes == DamageTypeFlags.damage_explosion)
                {
                    damage.amount = 600f;
                    return damage;
                }
                return null;
            }
            catch { }
            return null;
        }
    }
}