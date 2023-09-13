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
        [PluginReference]
        Plugin BQuests;

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

            var netUser = NetUser.Find(inv.networkView.owner);
            if (netUser == null || netUser.playerClient == null) return;

            if (target.type == ResourceTarget.ResourceTargetType.Animal && target.gameObject.name.ToLower().Contains("stag"))
            {
                var random = Random.Range(1, 100 / 4 + 1);
                if (random == 17)
                {
                    Helper.GiveItem(netUser.playerClient, DatablockDictionary.GetByName("Armor Part 5"));
                    BQuests?.CallHook("OnDropHorns", netUser);
                    rust.InventoryNotice(netUser, $"1 x \"HORNS\"");
                }
            }

            if (BoostedUsers.ContainsKey(netUser.userID) && item.ResourceItemName != "Armor Part 5")
            {
                var bonusCount = (int)(3 / RustExtended.Core.ResourcesAmountMultiplierRock * collected - collected);
                Helper.GiveItem(netUser.playerClient, item.ResourceItemDataBlock, bonusCount);

                rust.InventoryNotice(netUser, $"[Бонус] {bonusCount} x {item.ResourceItemName}");
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

                rust.Notice(user, "Вы использовали \"Чай\". Бонус к добыче: х3 на \"20\" минут!");

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
        private void OnTeaUse(NetUser user)
        {
            if (BoostedUsers.ContainsKey(user.userID)) return;
            rust.Notice(user, "Вы использовали \"Чай\". Бонус к добыче: х3 на \"20\" минут!");

            BoostedUsers.Add(user.userID,
                timer.Once(20 * 60, () =>
                {
                    if (!BoostedUsers.ContainsKey(user.userID)) return;
                    BoostedUsers.Remove(user.userID);
                    rust.Notice(user, "Бонус от предмета \"Чай\" закончился.");
                }));
        }
    }
}
