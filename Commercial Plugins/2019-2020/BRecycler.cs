using RageMods;
using RustExtended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BRecycler", "systemXcrackedZ", "1.2.8")]
    internal class BRecycler : RustLegacyPlugin
    {
        private List<string> recyclerPositions;
        private List<LootableObject> recyclers;

        private class RecycleItem
        {
            public string ItemName { get; set; }
            public Dictionary<string, int> ResultItems;

            public RecycleItem(string itemName, Dictionary<string, int> resultItems)
            {
                ItemName = itemName;
                ResultItems = resultItems;
            }
        }
        private List<RecycleItem> recycleItems = new List<RecycleItem>
        {
            new RecycleItem("Bolt Action Rifle", new Dictionary<string, int>{{"Wood", 25},{"Low Quality Metal", 15},{"Primed 556 Casing", 60},}),
            new RecycleItem("Bolt Action Rifle Blueprint", new Dictionary<string, int>{{"Paper", 1},}),
            new RecycleItem("556 Ammo", new Dictionary<string, int>{{"Gunpowder", 1},{"Metal Fragments", 1},}),
            new RecycleItem("9mm Ammo", new Dictionary<string, int>{{"Gunpowder", 1},{"Metal Fragments", 1},}),
            new RecycleItem("Handmade Shell", new Dictionary<string, int>{{"Gunpowder", 1},{"Stones", 1},}),
            new RecycleItem("Shotgun Shells", new Dictionary<string, int>{{"Gunpowder", 1},{"Metal Fragments", 1},}),
            new RecycleItem("Cloth Helmet", new Dictionary<string, int>{{"Cloth", 2},}),
            new RecycleItem("Cloth Pants", new Dictionary<string, int>{{"Cloth", 4},}),
            new RecycleItem("Cloth Vest", new Dictionary<string, int>{{"Cloth", 5},}),
            new RecycleItem("Cloth Boots", new Dictionary<string, int>{{"Cloth", 3},}),
            new RecycleItem("Kevlar Helmet", new Dictionary<string, int>{{"Low Quality Metal", 2},{"Leather", 1},}),
            new RecycleItem("Kevlar Pants", new Dictionary<string, int>{{"Low Quality Metal", 2},{"Leather", 2},}),
            new RecycleItem("Kevlar Vest", new Dictionary<string, int>{{"Low Quality Metal", 3},{"Leather", 2},}),
            new RecycleItem("Kevlar Boots", new Dictionary<string, int>{{"Low Quality Metal", 2},{"Leather", 1},}),
            new RecycleItem("Leather Helmet", new Dictionary<string, int>{{"Leather", 3},}),
            new RecycleItem("Leather Pants", new Dictionary<string, int>{{"Leather", 6},}),
            new RecycleItem("Leather Vest", new Dictionary<string, int>{{"Leather", 7},}),
            new RecycleItem("Leather Boots", new Dictionary<string, int>{{"Leather", 2},}),
            new RecycleItem("Rad Suit Boots", new Dictionary<string, int>{{"Cloth", 5},{"Metal Fragments", 15},}),
            new RecycleItem("Rad Suit Helmet", new Dictionary<string, int>{{"Cloth", 5},{"Metal Fragments", 15},}),
            new RecycleItem("Rad Suit Pants", new Dictionary<string, int>{{"Cloth", 7},{"Metal Fragments", 20},}),
            new RecycleItem("Rad Suit Vest", new Dictionary<string, int>{{"Cloth", 7},{"Metal Fragments", 25},}),
            new RecycleItem("Metal Ceiling BP", new Dictionary<string, int>{{"Paper", 1},}),
            new RecycleItem("Metal Doorway BP", new Dictionary<string, int>{{"Paper", 1},}),
            new RecycleItem("Metal Foundation BP", new Dictionary<string, int>{{"Paper", 1},}),
            new RecycleItem("Metal Pillar BP", new Dictionary<string, int>{{"Paper", 1},}),
            new RecycleItem("Metal Ramp BP", new Dictionary<string, int>{{"Paper", 1},}),
            new RecycleItem("Metal Stairs BP", new Dictionary<string, int>{{"Paper", 1},}),
            new RecycleItem("Metal Wall BP", new Dictionary<string, int>{{"Paper", 1},}),
            new RecycleItem("Metal Window BP", new Dictionary<string, int>{{"Paper", 1},}),
            new RecycleItem("Metal Ceiling", new Dictionary<string, int>{{"Low Quality Metal", 3},}),
            new RecycleItem("Metal Doorway", new Dictionary<string, int>{{"Low Quality Metal", 2},}),
            new RecycleItem("Metal Foundation", new Dictionary<string, int>{{"Low Quality Metal", 4},}),
            new RecycleItem("Metal Pillar", new Dictionary<string, int>{{"Low Quality Metal", 1},}),
            new RecycleItem("Metal Ramp", new Dictionary<string, int>{{"Low Quality Metal", 2},}),
            new RecycleItem("Metal Stairs", new Dictionary<string, int>{{"Low Quality Metal", 2},}),
            new RecycleItem("Metal Wall", new Dictionary<string, int>{{"Low Quality Metal", 2},}),
            new RecycleItem("Metal Window", new Dictionary<string, int>{{"Low Quality Metal", 2},}),
            new RecycleItem("Wood Ceiling", new Dictionary<string, int>{{"Wood Planks", 3},}),
            new RecycleItem("Wood Doorway", new Dictionary<string, int>{{"Wood Planks", 2},}),
            new RecycleItem("Wood Foundation", new Dictionary<string, int>{{"Wood Planks", 4},}),
            new RecycleItem("Wood Pillar", new Dictionary<string, int>{{"Wood Planks", 1},}),
            new RecycleItem("Wood Ramp", new Dictionary<string, int>{{"Wood Planks", 2},}),
            new RecycleItem("Wood Stairs", new Dictionary<string, int>{{"Wood Planks", 2},}),
            new RecycleItem("Wood Wall", new Dictionary<string, int>{{"Wood Planks", 2},}),
            new RecycleItem("Wood Window", new Dictionary<string, int>{{"Wood Planks", 2},}),
            new RecycleItem("Camp Fire", new Dictionary<string, int>{{"Wood", 2},}),
            new RecycleItem("Explosive Charge", new Dictionary<string, int>{{"Explosives", 5},{"Leather", 2},}),
            new RecycleItem("Furnace", new Dictionary<string, int>{{"Stones", 7},{"Wood", 10},{"Low Grade Fuel", 5},}),
            new RecycleItem("Large Spike Wall", new Dictionary<string, int>{{"Wood", 100},}),
            new RecycleItem("Large Wood Storage", new Dictionary<string, int>{{"Wood", 30},}),
            new RecycleItem("Metal Door", new Dictionary<string, int>{{"Metal Fragments", 100},}),
            new RecycleItem("Metal Window Bars", new Dictionary<string, int>{{"Metal Fragments", 50},}),
            new RecycleItem("Repair Bench", new Dictionary<string, int>{{"Stones", 6},{"Wood", 30},{"Metal Fragments", 25},{"Low Grade Fuel", 3},}),
            new RecycleItem("Spike Wall", new Dictionary<string, int>{{"Wood", 50},}),
            new RecycleItem("Wood Barricade", new Dictionary<string, int>{{"Wood", 15},}),
            new RecycleItem("Wood Gate", new Dictionary<string, int>{{"Wood", 60},}),
            new RecycleItem("Wood GateWay", new Dictionary<string, int>{{"Wood", 200},}),
            new RecycleItem("Wood Storage Box", new Dictionary<string, int>{{"Wood", 15},}),
            new RecycleItem("Wooden Door", new Dictionary<string, int>{{"Wood", 15},}),
            new RecycleItem("Workbench", new Dictionary<string, int>{{"Stones", 4},{"Wood", 25},}),
            new RecycleItem("Explosives", new Dictionary<string, int>{{"Gunpowder", 10},{"Low Grade Fuel", 1},{"Sulfur", 2},{"Metal Fragments", 5},}),
            new RecycleItem("Bandage", new Dictionary<string, int>{{"Cloth", 1},}),
            new RecycleItem("Large Medkit", new Dictionary<string, int>{{"Cloth", 2},{"Blood", 2},}),
            new RecycleItem("Small Medkit", new Dictionary<string, int>{{"Cloth", 1},{"Blood", 1},}),
            new RecycleItem("Flare", new Dictionary<string, int>{{"Gunpowder", 4},{"Metal Fragments", 4},}),
            new RecycleItem("Blood Draw Kit", new Dictionary<string, int>{{"Metal Fragments", 50},{"Cloth", 5},}),
            new RecycleItem("9mm Pistol", new Dictionary<string, int>{{"Low Quality Metal", 5},{"Primed 556 Casing", 15},}),
            new RecycleItem("9mm Pistol Blueprint", new Dictionary<string, int>{{"Paper", 1},}),
            new RecycleItem("F1 Grenade Blueprint", new Dictionary<string, int>{{"Paper", 1},}),
            new RecycleItem("M4 Blueprint", new Dictionary<string, int>{{"Paper", 1},}),
            new RecycleItem("MP5A4 Blueprint", new Dictionary<string, int>{{"Paper", 1},}),
            new RecycleItem("P250 Blueprint", new Dictionary<string, int>{{"Paper", 1},}),
            new RecycleItem("Shotgun Blueprint", new Dictionary<string, int>{{"Paper", 1},}),
            new RecycleItem("F1 Grenade", new Dictionary<string, int>{{"Gunpowder", 40},{"Metal Fragments", 20},}),
            new RecycleItem("Hatchet", new Dictionary<string, int>{{"Metal Fragments", 5},{"Wood", 10},}),
            new RecycleItem("Hunting Bow", new Dictionary<string, int>{{"Wood", 17},{"Cloth", 2},}),
            new RecycleItem("M4", new Dictionary<string, int>{{"Low Quality Metal", 15},{"Primed 556 Casing", 50},}),
            new RecycleItem("Flashlight Mod BP", new Dictionary<string, int>{{"Paper", 1},}),
            new RecycleItem("Holo sight BP", new Dictionary<string, int>{{"Paper", 1},}),
            new RecycleItem("Laser Sight BP", new Dictionary<string, int>{{"Paper", 1},}),
            new RecycleItem("Silencer BP", new Dictionary<string, int>{{"Paper", 1},}),
            new RecycleItem("Flashlight Mod", new Dictionary<string, int>{{"Low Quality Metal", 2},{"Primed 556 Casing", 5},}),
            new RecycleItem("Holo sight", new Dictionary<string, int>{{"Low Quality Metal", 2},{"Primed 556 Casing", 5},}),
            new RecycleItem("Laser Sight", new Dictionary<string, int>{{"Low Quality Metal", 2},{"Primed 556 Casing", 5},}),
            new RecycleItem("Silencer", new Dictionary<string, int>{{"Low Quality Metal", 4},{"Primed 556 Casing", 8},}),
            new RecycleItem("MP5A4", new Dictionary<string, int>{{"Low Quality Metal", 10},{"Primed 556 Casing", 30},}),
            new RecycleItem("P250", new Dictionary<string, int>{{"Low Quality Metal", 6},{"Primed 556 Casing", 20},}),
            new RecycleItem("Pick Axe", new Dictionary<string, int>{{"Metal Fragments", 7},{"Wood", 20},}),
            new RecycleItem("Shotgun", new Dictionary<string, int>{{"Low Quality Metal", 6},{"Primed 556 Casing", 20},}),
            new RecycleItem("Stone Hatchet", new Dictionary<string, int>{{"Wood", 10},{"Stones", 5},}),
            new RecycleItem("HandCannon", new Dictionary<string, int>{{"Wood", 10},{"Metal Fragments", 5},{"Primed 556 Casing", 5},}),
            new RecycleItem("Pipe Shotgun", new Dictionary<string, int>{{"Wood", 25},{"Metal Fragments", 20},{"Primed 556 Casing", 10},}),
            new RecycleItem("Revolver", new Dictionary<string, int>{{"Wood", 30},{"Metal Fragments", 40},{"Primed 556 Casing", 15},{"Cloth", 5},}),
            new RecycleItem("Large Wood Storage Blueprint", new Dictionary<string, int>{{"Paper", 1},}),
            new RecycleItem("Armor Part 5", new Dictionary<string, int>{{"Small Water Bottle", 1}})
        };

        public FieldInfo recyclePlayer;
        private const float recycleTime = 5.0f;

        private Dictionary<LootableObject, Timer> timers = new Dictionary<LootableObject, Timer>();
        private Dictionary<LootableObject, List<IInventoryItem>> recycleQueue = new Dictionary<LootableObject, List<IInventoryItem>>();

        private void Loaded()
        {
            recyclePlayer = typeof(LootableObject).GetField("_currentlyUsingPlayer", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
            recyclerPositions = Core.Interface.GetMod().DataFileSystem.ReadObject<List<string>>("RecyclersData");
            recyclers = new List<LootableObject>();

            if (!World.Initialized || !Spawns.Initialized || !RustExtended.Bootstrap.Initialized) timer.Once(60.0f, () =>
            {
                InitRecyclers();
            });
            else
            {
                InitRecyclers();
            }
        }
        private void Unload() => SaveData();
        private void OnServerSave() => SaveData();

        private void InitRecyclers()
        {
            List<LootableObject> lootables = UnityEngine.Object.FindObjectsOfType<LootableObject>().Where(f => IsLootable(f)).ToList();
            if (lootables == null || lootables.Count == 0) return;

            foreach (string recyclerPosition in new List<string>(recyclerPositions))
            {
                Vector3 position = ToVector3(recyclerPosition);
                LootableObject lootable = lootables.Find(f => Vector3.Distance(f.transform.position, position) < 5.0f)?.GetComponent<Inventory>()?.GetLocal<LootableObject>();
                if (lootable == null)
                {
                    recyclerPositions.Remove(recyclerPosition);
                    continue;
                }
                recyclers.Add(lootable);
                recycleQueue.Add(lootable, new List<IInventoryItem>());
            }
        }

        private void OnPlayerInitialized(NetUser user, PlayerMods mods)
        {
            mods.ChangeBlueprint("Research Kit 1", new Dictionary<string, int> { { "Low Quality Metal", 20 }, { "Leather", 20 }, { "Paper", 3 } });
	    mods.ChangeBlueprint("Camp Fire", new Dictionary<string, int> { { "Low Quality Metal", 78979799 } });
        }

        private void OnItemRemoved(Inventory fromInv, int slot, IInventoryItem item)
        {
            LootableObject lootable = fromInv.GetLocal<LootableObject>();
            if (lootable == null) return;
            if (!IsRecycler(lootable)) return;
            if (!timers.ContainsKey(lootable)) return;

            string itemName = item.datablock.name;

            if (!IsRecycle(itemName)) return;

            if (recycleQueue[lootable].Contains(item))
            {
                recycleQueue[lootable].Remove(item);
                return;
            }
            timers[lootable].Destroy();
            timers.Remove(lootable);

            if (recycleQueue[lootable].Count > 0)
            {
                DoRecycle(fromInv, recycleQueue[lootable][0], lootable);
                recycleQueue[lootable].Remove(item);
            }
        }
        private void DoRecycle(Inventory inv, IInventoryItem item, LootableObject lootable)
        {
            int itemCount = Helper.InventoryItemCount(inv, item.datablock);
           
            string itemName = item.datablock.name;
            RecycleItem recycleItem = recycleItems.Find(f => f.ItemName == item.datablock.name);
            NetUser user = null;
            if ((uLink.NetworkPlayer)recyclePlayer.GetValue(lootable) != uLink.NetworkPlayer.unassigned) user = (NetUser)((uLink.NetworkPlayer)recyclePlayer.GetValue(lootable)).GetLocalData();
            if (user != null) rust.Notice(user, $"Началась переработка предмета \"{itemName}\"! Ждать: {(int)recycleTime} секунд.");
            timers.Add(lootable, timer.Once(recycleTime, () =>
            {
                if (!Helper.InventoryGetItems(inv).Contains(item))
                {
                    timers.Remove(lootable);
                    if (recycleQueue[lootable].Count > 0)
                    {
                        DoRecycle(inv, recycleQueue[lootable][0], lootable);
                        recycleQueue[lootable].Remove(item);
                    }
                    return;
                }
                itemCount = Helper.InventoryItemCount(inv, item.datablock);
                Helper.InventoryItemRemove(inv, item.datablock, itemCount);
                foreach (KeyValuePair<string, int> result in recycleItem.ResultItems) inv.AddItemAmount(DatablockDictionary.GetByName(result.Key), result.Value * itemCount);
                if (itemName == "Armor Part 5") itemName = "Рога";
                if (user != null) rust.Notice(user, $"Предмет \"{itemName}\" был успешно переработан!");

                timers.Remove(lootable);
                if (recycleQueue[lootable].Count > 0)
                {
                    DoRecycle(inv, recycleQueue[lootable][0], lootable);
                    recycleQueue[lootable].Remove(item);
                }
            }));
        }
        private void OnItemAdded(Inventory inv, int slot, IInventoryItem item)
        {
            LootableObject lootable = inv.GetLocal<LootableObject>();
            if (lootable == null) return;
            if (!IsRecycler(lootable)) return;

            string itemName = item.datablock.name;
            uLink.NetworkPlayer value = (uLink.NetworkPlayer)recyclePlayer.GetValue(lootable);

            if (value != uLink.NetworkPlayer.unassigned)
            {
                NetUser user = value.GetLocalData<NetUser>();
                if (user == null) return;

                int itemCount = Helper.InventoryItemCount(inv, item.datablock);
                if (itemName == "Armor Part 5") itemName = "Рога";
                RecycleItem recycleItem = recycleItems.Find(f => f.ItemName == item.datablock.name);
                if (recycleItem == null)
                {
                    if (itemName == "Research Kit 1") return;
                    Helper.InventoryItemRemove(inv, item.datablock, itemCount);
                    Helper.GiveItem(user.playerClient, itemName, itemCount);
                    return;
                }

                if (timers.ContainsKey(lootable))
                {
                    recycleQueue[lootable].Add(item);
                    rust.Notice(user, $"Предмет \"{itemName}\" был добавлен в очередь переработки!");
                    return;
                }
                DoRecycle(inv, item, lootable);
            }
        }

        private bool IsLootable(LootableObject deploy) =>
            deploy != null && deploy.GetComponent<Inventory>() != null && deploy.GetComponent<Inventory>().GetLocal<LootableObject>() != null;
        private bool IsRecycler(LootableObject lootable) =>
            recyclers.Contains(lootable);
        private bool IsRecycle(string item) =>
            recycleItems.Find(f => f.ItemName == item) != null;

        private Vector3 ToVector3(string vector)
        {
            if (vector.StartsWith("(") && vector.EndsWith(")"))
                vector = vector.Substring(1, vector.Length - 2);

            var sArray = vector.Split(',');
            return new Vector3(float.Parse(sArray[0]), float.Parse(sArray[1]), float.Parse(sArray[2]));
        }

        [ChatCommand("setrecycler")]
        private void CMD_SetRecycler(NetUser user, string cmd, string[] args)
        {
            if (user.admin)
            {
                GameObject obj = Helper.GetLookObject(Helper.GetLookRay(user), 10.0f);
                if (obj == null)
                {
                    rust.Notice(user, "Look object is null.");
                    return;
                }

                LootableObject deployable = obj.GetComponent<LootableObject>();
                if (deployable == null)
                {
                    rust.Notice(user, "Look object is not deploy.");
                    return;
                }
                
                if (!IsLootable(deployable))
                {
                    rust.Notice(user, "Look object is not lootable.");
                    return;
                }

                LootableObject lootable = deployable.GetComponent<Inventory>().GetLocal<LootableObject>();
                recyclerPositions.Add(lootable.transform.position.ToString());
                recyclers.Add(lootable);
                recycleQueue.Add(lootable, new List<IInventoryItem>());
                SaveData();

                rust.Notice(user, "You saved a new recycler.");
            }
        }

        private void SaveData() =>
            Core.Interface.GetMod().DataFileSystem.WriteObject("RecyclersData", recyclerPositions);
    }
}
