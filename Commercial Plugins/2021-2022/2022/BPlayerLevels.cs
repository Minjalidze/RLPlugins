using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Oxide.Plugins
{
    internal class BRustMMO : RustLegacyPlugin
    {
        [Flags]
        private enum Skill
        {
            Null = -1,

            Mining, WoodCutting,

            Archery, Axes = 3,

            Repair, Smelting = 5/*,
            Acrobatics = 6,*/
        }
        [Flags]
        private enum ActionType
        {
            Null = -1,
            Kill = 0,

            Loot = 1, Gather = 2, Repair = 3, 
            Smelting = 4/*,
                         * Falling = 5*/
        }
        [Flags]
        private enum ChanceType
        {
            DoubleDrop = 0, DoubleLoot = 1,

            IncreaseDamage = 2, IncreaseArmor = 3, IncreaseSmeltingSpeed = 4,

            PerfectRepair = 5
        }
        private class PlayerStats
        {
            public Dictionary<Skill, int> SkillPoints { get; set; }
        }
        private class SkillStats
        {
            public List<ActionType> ActionTypes { get; set; }
            public Dictionary<ChanceType, int> Chances { get; set; }
            public int ChancePower { get; set; }
        }

        private class Chance
        {
            public Chance(int chance) => iChance = chance;

            private int iChance { get; set; }

            public bool IsChance()
            {
                Random r = new Random();
                double res = iChance / 100.0;

                if (r.NextDouble() < res) return true;
                return false;
            }
        }
    }
}
