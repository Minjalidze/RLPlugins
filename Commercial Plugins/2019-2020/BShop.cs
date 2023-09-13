using Oxide.Core;
using RustExtended;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("BShop", "systemXcrackedZ", "1.2.4")]
    internal class BShop : RustLegacyPlugin
    {
        private const string ChatName = "Магазин";
        private const string Scrap = "Primed 556 Casing";

        private class BuyItem
        {
            public BuyItem(int scrapValue, string itemName, int itemValue)
            {
                ScrapValue = scrapValue;
                ItemName = itemName;
                ItemValue = itemValue;
            }
            public int ScrapValue { get; set; }
            public string ItemName { get; set; }
            public int ItemValue { get; set; }
        }
        private class SellItem
        {
            public SellItem(string itemName, int itemValue, int scrapValue)
            {
                ItemName = itemName;
                ItemValue = itemValue;
                ScrapValue = scrapValue;
            }
            public string ItemName { get; set; }
            public int ItemValue { get; set; }
            public int ScrapValue { get; set; }
        }

        private List<BuyItem> buyItems = new List<BuyItem>
        {
            new BuyItem(500, "Gunpowder", 250),
            new BuyItem(200, "RandomCase", 1),
            new BuyItem(100, "Wood", 250),
            new BuyItem(250, "9mm Ammo", 250),
            new BuyItem(750, "Explosives", 1),
            new BuyItem(125, "Metal Fragments", 250),
            new BuyItem(100, "Cloth", 50),
            new BuyItem(1000, "MAP", 1),
            new BuyItem(500, "Research Kit 1", 1),
            new BuyItem(2000, "Supply Signal", 1)
        };
        private List<SellItem> sellItems = new List<SellItem>
        {
            new SellItem("Can of Tuna", 5, 100),
            new SellItem("Granola Bar", 5, 60),
            new SellItem("Small Rations", 1, 50),
            new SellItem("Chocolate Bar", 5, 50),
            new SellItem("Can of Beans", 5, 100),
            new SellItem("Stones", 250, 100),
            new SellItem("Low Quality Metal", 25, 150),
            new SellItem("Research Kit 1", 1, 300),
            new SellItem("Wood", 250, 75),
            new SellItem("Anti-Radiation Pills", 10, 150)
        };

        private List<Dictionary<string, int>> randomBoxItems = new List<Dictionary<string, int>>()
        {
            new Dictionary<string, int> { { "Wood", 250 } },
            new Dictionary<string, int>() { { "Research Kit 1", 1 } },
            new Dictionary<string, int>() { { "Revolver", 1 } },
            new Dictionary<string, int>() { { "Pipe Shotgun", 1 } },
            new Dictionary<string, int>() { { "Stones", 250 } },
            new Dictionary<string, int>() { { "MP5A4 Blueprint", 1 } },
            new Dictionary<string, int>() { { "Metal Ore", 250 } },
            new Dictionary<string, int>() { { "Cloth", 125 } },
            new Dictionary<string, int>() { { "Low Grade Fuel", 100 } }
        };

        [ChatCommand("sbuy")]
        private void CMD_ShopBuy(NetUser user, string cmd, string[] args)
        {
            var index = int.Parse(args[0]);
            var buyItem = buyItems[index];

            Inventory inv = user.playerClient.rootControllable.character.idMain.GetComponent<Inventory>();
            if (inv == null) return;

            if (Helper.InventoryItemCount(inv, DatablockDictionary.GetByName(Scrap)) < buyItem.ScrapValue)
            {
                rust.SendChatMessage(user, ChatName, $"У вас нет предмета \"SCRAP\" в количестве {buyItem.ScrapValue} шт");
                return;
            }

            Helper.InventoryItemRemove(inv, DatablockDictionary.GetByName(Scrap), buyItem.ScrapValue);
            if (buyItem.ItemName == "RandomCase")
            {
                var randomItems = randomBoxItems.ElementAt(UnityEngine.Random.Range(0, randomBoxItems.Count));
                foreach (var randomItem in randomItems)
                {
                    Helper.GiveItem(user.playerClient, DatablockDictionary.GetByName(randomItem.Key), randomItem.Value);
                    rust.SendChatMessage(user, ChatName, $"Вы успешно обменяли \"SCRAP\" (x{buyItem.ScrapValue}) на \"{randomItem.Key}\" (x{randomItem.Value})");
                }
            }
            else if (buyItem.ItemName == "MAP")
            {
                var random = Random.Range(0, 5);
                Helper.GiveItem(user.playerClient, DatablockDictionary.GetByName($"Armor Part {random}"), buyItem.ItemValue);
                rust.SendChatMessage(user, ChatName, $"Вы успешно обменяли \"SCRAP\" (x{buyItem.ScrapValue}) на \"{buyItem.ItemName} {random}\" (x{buyItem.ItemValue})");
            }
            else
            {
                Helper.GiveItem(user.playerClient, DatablockDictionary.GetByName(buyItem.ItemName), buyItem.ItemValue);
                rust.SendChatMessage(user, ChatName, $"Вы успешно обменяли \"SCRAP\" (x{buyItem.ScrapValue}) на \"{buyItem.ItemName}\" (x{buyItem.ItemValue})");
            }
        }
        [ChatCommand("ssell")]
        private void CMD_ShopSell(NetUser user, string cmd, string[] args)
        {
            var index = int.Parse(args[0]);
            var sellItem = sellItems[index];

            Inventory inv = user.playerClient.rootControllable.character.idMain.GetComponent<Inventory>();
            if (inv == null) return;

            if (Helper.InventoryItemCount(inv, DatablockDictionary.GetByName(sellItem.ItemName)) < sellItem.ItemValue)
            {
                rust.SendChatMessage(user, ChatName, $"У вас нет предмета \"{sellItem.ItemName}\" в количестве {sellItem.ItemValue} шт");
                return;
            }

            Helper.InventoryItemRemove(inv, DatablockDictionary.GetByName(sellItem.ItemName), sellItem.ItemValue); 
            Helper.GiveItem(user.playerClient, DatablockDictionary.GetByName(Scrap), sellItem.ScrapValue);
            rust.SendChatMessage(user, ChatName, $"Вы успешно обменяли \"{sellItem.ItemName}\" (x{sellItem.ItemValue}) на \"Scrap\" (x{sellItem.ScrapValue})");
        }
    }
}
