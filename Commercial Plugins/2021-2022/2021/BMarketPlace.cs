using System.Collections.Generic;
using System.Globalization;
using RustExtended;
//using RageMods;

namespace Oxide.Plugins
{
    [Info("BMarketPlace", "systemXcrackedZ", "1.0.0")]
  
    public class BMarketPlace : RustLegacyPlugin
    {
        private class MarketItem
        {
            public string Product { get; set; }
            public string Price { get; set; }
            public string Seller { get; set; }
            public int Count { get; set; }
        }
        private static List<MarketItem> _marketItems = new List<MarketItem>();

        private class UndiscoveredPlayer
        {
            public string PlayerName { get; set; }
            public string Item { get; set; }
            public int Count { get; set; }
        }
        private static List<UndiscoveredPlayer> _undiscoveredPlayers = new List<UndiscoveredPlayer>();

        private static Dictionary<int, List<MarketItem>> _marketPages = new Dictionary<int, List<MarketItem>>();

        private void Loaded()
        {
            _marketItems = Core.Interface.GetMod().DataFileSystem.ReadObject<List<MarketItem>>("MarketPlace");
            _undiscoveredPlayers = Core.Interface.GetMod().DataFileSystem.ReadObject<List<UndiscoveredPlayer>>("MarketPlaceUndiscoveredPlayers");
        }
        private void OnServerSave()
        {
            SaveData();
        }
        private static void SaveData()
        {
            Core.Interface.GetMod().DataFileSystem.WriteObject("MarketPlace", _marketItems);
            Core.Interface.GetMod().DataFileSystem.WriteObject("MarketPlaceUndiscoveredPlayers", _undiscoveredPlayers);
        }

        /*private void OnPlayerInitialized(NetUser user, PlayerMods playerMods)
        {
            if (user == null) return;
            
            var undiscoveredPlayer = _undiscoveredPlayers.Find(f => f.PlayerName == user.playerClient.userName);
            if (undiscoveredPlayer != null)
            {
                if (TheStringIsANumber(undiscoveredPlayer.Item))
                {
                    var userBalance = (int)Economy.Database[user.userID].Balance;
                    Economy.Database[user.userID].Balance = (ulong) (userBalance + undiscoveredPlayer.Count);
                    return;
                }
                Helper.GiveItem(user.playerClient, undiscoveredPlayer.Item, undiscoveredPlayer.Count);
            }
        }*/

        private static void AddPage(int page, List<MarketItem> items) => _marketPages.Add(page, items);
        
        [ChatCommand("mp")]
        private void OnCMD_MP(NetUser user, string cmd, string[] args)
        {
            if (_marketItems.Count == 0)
            {
                SendChatMessage(user, "В текущий момент на торговой площадке нет лотов.");
                return;
            }
        
            _marketPages.Clear();
            
            var b = 0;
            var itemList = new List<MarketItem>();

            for (var z = 0; z < _marketItems.Count; z++)
            {
                itemList.Add(_marketItems[z]);

                if (_marketItems.Count <= 5 && z == _marketItems.Count)
                {
                    AddPage(b, itemList);
                }

                if (!IsDivisible(z, 6)) continue;
                
                b++;
                AddPage(b, itemList);
                itemList.Clear();
            }

            var pageNumber = 1;
            if (args.Length > 0) 
                if (IsStringIsANumber(args[0])) 
                    pageNumber = int.Parse(args[0]);

            var i = 1;
            foreach (var item in _marketPages[pageNumber])
            {
                var itemMessage = $"{i}) Товар: \"{item.Product}\". Цена: \"{item.Price}({item.Count})\"";

                if (IsStringIsANumber(item.Price))
                {
                    itemMessage = $"{i}) Товар: \"{item.Product}\". Цена: {item.Price}$";
                }
                
                SendChatMessage(user, itemMessage);
                SendChatMessage(user, $"Продавец: \"{item.Seller}\"");
                SendChatMessage(user, "---------------------------------------------------------------");
                i++;
            }
            
            SendChatMessage(user, $"Переход по страницам: /mp \"номер страницы ({pageNumber}/{_marketPages.Count})\"");
            SendChatMessage(user, "Для покупки товара введите: /mpbuy \"номер лота\"");
        }
        [ChatCommand("mpsell")]
        private void OnCMD_MPSell(NetUser user, string cmd, string[] args)
        {
            if (args.Length < 2)
            {
                SendChatMessage(user, "Использование команды /mpsell: \"Товар\" \"Цена\" \"Количество\"");
                SendChatMessage(user, "Ценой может являться как деньги, так и предметы из вашего инвентаря!");
                return;
            }

            var inventory = user.playerClient.rootControllable.idMain.GetComponent<Inventory>();
            
            var product = args[0].ToUpper(new CultureInfo("en-US", false));
            var price = args[1];
            var count = 1;

            if (args.Length >= 3)
            {
                count = int.Parse(args[2]);
            }
            if (Helper.InventoryItemCount(inventory, DatablockDictionary.GetByName(args[0])) == 0)
            {
                if (DatablockDictionary.GetByName(product) == null)
                {
                    SendChatMessage(user, $"Предмет \"{product}\" не существует в игре!");
                    return;
                }
                SendChatMessage(user, $"У вас нет предмета \"{args[0]}\"!");
                return;
            }
            
            var sellMessage = $"Вы выставили товар \"{args[0]}\" за \"{args[1]}\"({count})!";
            
            int z;
            if (int.TryParse(args[1], out z))
            {
                price = z.ToString();
                count = z;
                sellMessage = $"Вы выставили товар \"{args[0]}\" за {z}$!";
            }
            
            _marketItems.Add(new MarketItem
            {
                Product = product,
                Price = price,
                Seller = Helper.GetPlayerClient(user.userID).userName,
                Count = count
            });
            
            Helper.InventoryItemRemove(inventory, DatablockDictionary.GetByName(args[0]), 1);
            
            SendChatMessage(user, sellMessage);
            SaveData();
        }
        [ChatCommand("mpbuy")]
        private void OnCMD_MPBuy(NetUser user, string cmd, string[] args)
        {
            if (args.Length != 1)
            {
                SendChatMessage(user, "Использование команды: /mpbuy \"Номер лота\"");
                SendChatMessage(user, "Чтобы посмотреть список доступных лотов, введите \"/mp\"");
                return;
            }
            if (!IsStringIsANumber(args[0]))
            {
                SendChatMessage(user, "Использование команды: /mpbuy \"Номер лота\"");
                SendChatMessage(user, "Чтобы посмотреть список доступных лотов, введите \"/mp\"");
                return;
            }
            
            var iLot = int.Parse(args[0]) - 1;
            var item = _marketItems[iLot];
                
            if (item == null)
            {
                SendChatMessage(user, $"Лот с номером {iLot} не существует!");
                SendChatMessage(user, "Чтобы посмотреть список доступных лотов, введите \"/mp\"");
                return;
            }

            var inventory = user.playerClient.rootControllable.idMain.GetComponent<Inventory>();
            var buyMessage = $"Вы купили товар \"{item.Product}\" за \"{item.Price}({item.Count})\"!";
            var buyedMessage = $"Ваш товар \"{item.Product}\" купили! Вы получили: \"{item.Price}({item.Count})\"!";
            
            if (IsStringIsANumber(item.Price))
            {
                var price = int.Parse(item.Price);
                var userBalance = (int)Economy.Database[user.userID].Balance;

                if (price > userBalance)
                {
                    SendChatMessage(user, "У вас недостаточно средств для покупки лота!");
                    return;
                }
                Economy.Database[user.userID].Balance = (ulong)(userBalance - price);
                buyMessage = $"Вы купили товар \"{item.Product}\" за {item.Price}$!";
                buyedMessage = $"Ваш товар \"{item.Product}\" купили! Вы получили: {item.Price}$!";
            }
            if (Helper.InventoryItemCount(inventory, DatablockDictionary.GetByName(item.Price)) < item.Count)
            { 
                SendChatMessage(user, $"У вас нет предмета \"{item.Price}({item.Count})\"");
                return;   
            }

            SendChatMessage(user, buyMessage);
            if (!IsStringIsANumber(item.Price)) Helper.InventoryItemRemove(inventory, DatablockDictionary.GetByName(item.Price), item.Count);
            
            Helper.GiveItem(user.playerClient, DatablockDictionary.GetByName(item.Product));

            var seller = Helper.GetPlayerClient(item.Seller);
            if (seller == null)
            {
                _undiscoveredPlayers.Add(new UndiscoveredPlayer
                {
                    PlayerName = item.Seller,
                    Item = item.Price,
                    Count = item.Count
                });
                return;
            }
            
            SendChatMessage(seller.netUser, buyedMessage);
            if (IsStringIsANumber(item.Price))
            {
                var sellerBalance = (int)Economy.Database[seller.userID].Balance;
                Economy.Database[seller.userID].Balance = (ulong)(sellerBalance + item.Count);
                return;
            }
            
            Helper.GiveItem(seller, DatablockDictionary.GetByName(item.Price));

            _marketItems.Remove(item);
            SaveData();
        }

        private static bool IsStringIsANumber(string estimatedNumber)
        {
            int n;
            return int.TryParse(estimatedNumber, out n);
        }
        private static bool IsDivisible(int x, int n)
        {
            return (x % n) == 0;
        }
        
        private void SendChatMessage(NetUser user, string message) => rust.SendChatMessage(user, "MarketPlace", $"[COLOR #DC143C]{message}");
    }
}