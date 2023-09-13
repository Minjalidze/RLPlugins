using System;
using System.Collections.Generic;
using RustExtended;

namespace Oxide.Plugins
{
    using Random = Oxide.Core.Random;
    [Info("BlessOre", "systemXcrackedZ (vk.com/sysxcrackz)", "1.0.0")]
    public class BlessOre : RustLegacyPlugin
    {
        private static readonly List<string> AvailableOres = new List<string>
        {
            "/p 1 - обмен Uran. В него входит: ",
            " - Uber Hatchet, Leather Helmet, Leather Vest, Leather Pants, Leather Boots.",
            "-",
            "/p 2 - обмен Californi. В него входит: ",
            " - 45 Low Quality Metal, 2 F1 Grenade, 450 Gunpowder."
        };
        
        [ChatCommand("p")]
        private void OnCMD_P(NetUser user, string cmd, string[] args)
        {
            if (args.Length == 0)
            {
                SCM(user, "Список доступных команд: ");
                foreach (var oreMessage in AvailableOres)
                {
                    SCM(user, oreMessage);
                }
                return;
            }

            var inv = user.playerClient.controllable.GetComponent<Inventory>();

            var weaponPart3DataBlock = DatablockDictionary.GetByName("Weapon Part 3");
            var weaponPart4DataBlock = DatablockDictionary.GetByName("Weapon Part 4");
            
            switch (args[0])
            {
                case "1":
                    if (IsExist(inv, weaponPart3DataBlock))
                    {
                        ItemRemove(inv, weaponPart3DataBlock);
                        
                        ItemGive(user.playerClient, GetItemDataBlock("Uber Hatchet"));
                        ItemGive(user.playerClient, GetItemDataBlock("Leather Helmet"));
                        ItemGive(user.playerClient, GetItemDataBlock("Leather Vest"));
                        ItemGive(user.playerClient, GetItemDataBlock("Leather Pants"));
                        ItemGive(user.playerClient, GetItemDataBlock("Leather Boots"));
                        
                        SCM(user, "Вы успешно обменяли \"Uran\"!");
                        return;
                    }
                    SCM(user, "В вашем инвентаре нет \"Uran\"!");
                    return;
                case "2":
                    if (IsExist(inv, weaponPart4DataBlock))
                    {
                        ItemRemove(inv, weaponPart4DataBlock);
                        
                        ItemGive(user.playerClient, GetItemDataBlock("Low Quality Metal"), 45);
                        ItemGive(user.playerClient, GetItemDataBlock("F1 Grenade"), 2);
                        ItemGive(user.playerClient, GetItemDataBlock("Gunpowder"), 450);
                        
                        SCM(user, "Вы успешно обменяли \"Californi\"!");
                        return;
                    }
                    SCM(user, "В вашем инвентаре нет \"Californi\"!");
                    break;
            }
        }
        
        #region [SERVER ACTIONS] -> [Действия Сервера]

        private void OnGather(Inventory inv, ResourceTarget resourceTarget, ResourceGivePair resourceGivePair, int amount)
        {
            try
            {
                var user = NetUser.Find(inv.networkView.owner);
                
                switch (resourceTarget.type)
                {
                    case ResourceTarget.ResourceTargetType.Rock1:
                    case ResourceTarget.ResourceTargetType.Rock2:
                    case ResourceTarget.ResourceTargetType.Rock3:
                    {
                        var resultUran = Random.Range(1, 6);
                        if (resultUran == 3)
                        {
                            Helper.GiveItem(user.playerClient, "Weapon Part 3");
                            rust.InventoryNotice(user, "Uran x1");
                            return;
                        }
                        
                        var resultCaliforni = Random.Range(1, 6);
                        if (resultCaliforni == 3)
                        {
                            Helper.GiveItem(user.playerClient, "Weapon Part 4");
                            rust.InventoryNotice(user, "Californi x1");
                        }
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                //stfu fucking unity
            }
        }

        #endregion
        
        #region [SIMPLIFICATION] -> [Упрощения]

        private void SCM(NetUser user, string message, string color = "[COLOR #FFD700]") => rust.SendChatMessage(user, "INVR", $"{color}{message}");

        private static void ItemRemove(Inventory inventory, ItemDataBlock itemDataBlock, int count = 1) => 
            Helper.InventoryItemRemove(inventory, itemDataBlock, count);
        private static void ItemGive(PlayerClient player, ItemDataBlock itemDataBlock, int count = 1) => 
            Helper.GiveItem(player, itemDataBlock, count);
        private static bool IsExist(Inventory inventory, ItemDataBlock itemDataBlock) =>
            Helper.InventoryItemCount(inventory, itemDataBlock) > 0;

        private static ItemDataBlock GetItemDataBlock(string item) => DatablockDictionary.GetByName(item);

        #endregion
    }
}