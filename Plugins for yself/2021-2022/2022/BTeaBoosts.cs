using Oxide.Core;
using RustExtended;
using UnityEngine;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using Random = Oxide.Core.Random;

namespace Oxide.Plugins
{
    [Info("BTeaBoosts", "systemXcrackedZ", "2.2.4")]
    internal class BTeaBoosts : RustLegacyPlugin
    {
        private readonly Dictionary<ulong, Timer> BoostedUsers = new Dictionary<ulong, Timer>();
        private const string Booster = "Small Water Bottle";

        private void OnPlayerDisconnected(uLink.NetworkPlayer networkPlayer)
        {
            NetUser user = NetUser.Find(networkPlayer);
            if (user != null && BoostedUsers.ContainsKey(user.userID))
            {
                BoostedUsers[user.userID].Destroy();
                BoostedUsers.Remove(user.userID);
            }
        }

        private void OnGather(Inventory inv, ResourceTarget target, ResourceGivePair item, int collected)
        {
            if (item == null || inv == null || target == null || collected < 1 || inv.networkView.owner == null) return;

            if (target.type != ResourceTarget.ResourceTargetType.Rock1
                && target.type != ResourceTarget.ResourceTargetType.Rock2
                && target.type != ResourceTarget.ResourceTargetType.Rock3
                && target.type != ResourceTarget.ResourceTargetType.WoodPile
                && target.type != ResourceTarget.ResourceTargetType.Animal)
                return;

            NetUser netuser = NetUser.Find(inv.networkView.owner);
            if (netuser == null || netuser.playerClient == null) return;

            if (target.type == ResourceTarget.ResourceTargetType.Animal && target.gameObject.name.ToLower().Contains("stag"))
            {
                int random = Random.Range(1, (100 / 5) + 1);
                if (random == 10)
                {
                    Helper.GiveItem(netuser.playerClient, DatablockDictionary.GetByName("Armor Part 5"), 1);
                    rust.InventoryNotice(netuser, $"1 x \"TEA\"");
                }
            }

            if (BoostedUsers.ContainsKey(netuser.userID) && item.ResourceItemName != "Armor Part 5")
            {
                int bonusCount = (int)((2 / RustExtended.Core.ResourcesAmountMultiplierRock * collected) - collected);
                Helper.GiveItem(netuser.playerClient, item.ResourceItemDataBlock, bonusCount);

                rust.InventoryNotice(netuser, $"[Бонус] {bonusCount} x {item.ResourceItemName}");
            }
        }

        private void OnKilled(TakeDamage takeDamage, DamageEvent evt)
        {
            try
            {
                NetUser victim = evt.victim.client?.netUser;
                if (victim == null || !BoostedUsers.ContainsKey(victim.userID)) return;

                BoostedUsers[victim.userID].Destroy();
                BoostedUsers.Remove(victim.userID);
            }
            catch { }
        }

        [HookMethod("OnBeltUse")]
        public object OnBeltUse(PlayerInventory playerInv, IInventoryItem inventoryItem)
        {
            if (inventoryItem != null && Booster == inventoryItem.datablock.name && !BoostedUsers.ContainsKey(playerInv.inventoryHolder.netUser.userID))
            {
                NetUser user = playerInv.inventoryHolder.netUser;

                Inventory inv = rust.GetInventory(user);
                Helper.InventoryItemRemove(inv, DatablockDictionary.GetByName(Booster), 1);

                rust.Notice(user, "Вы использовали \"Чай\". Бонус к добыче: х2 на \"20\" минут!");

                BoostedUsers.Add(user.userID,
                    timer.Once(20 * 60, () =>
                      {
                          if (!BoostedUsers.ContainsKey(user.userID)) return;
                          BoostedUsers.Remove(user.userID);
                          rust.Notice(user, "Бонус от предмета \"Чай\" закончился.");
                      }));

                return true;
            }
            return null;
        }

    }
}
