using System;
using System.Linq;
using System.Collections.Generic;
//
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
//
using RageMods;
using UnityEngine;
using RustExtended;
//
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    using Random = Core.Random;
    [Info("BBattlePass", "systemXcrackedZ", "4.1.8")]
    internal class BSFBattlePass : RustLegacyPlugin
    {
        #region [CLASSES] -> Классы. Информация об игроках, наградах, заданиях.

        private class Holder
        {
            [JsonProperty("Идентификатор")] public ulong UserID { get; set; }
            [JsonProperty("Текущий квест")] public int CurrentQuest { get; set; }
            [JsonProperty("Статус подписки")] public bool IsSubscriber { get; set; }
            [JsonProperty("Корзина")] public List<string> Basket { get; set; }
        }
        private class QuestAward
        {
            public class DefaultAward
            {
                public DefaultAward(int questNumber)
                {
                    QuestNumber = questNumber;
                }

                public int QuestNumber { get; }
                public string Item { get; set; }
                public int Amount { get; set; }
            }
            public class GoldAward
            {
                public GoldAward(int questNumber)
                {
                    QuestNumber = questNumber;
                }

                public int QuestNumber { get; }
                public string Item { get; set; }
                public int Amount { get; set; }
            }

            public static readonly List<DefaultAward> DefaultAwards = new List<DefaultAward>
            {
                new DefaultAward(1) { Item =  "Stones", Amount =         1 },
                new DefaultAward(2) { Item =  "MetalOre", Amount =       1 },
                new DefaultAward(3) { Item =  "SulfurOre", Amount =      1 },
                new DefaultAward(4) { Item =  "Cloth", Amount =          1 },
                new DefaultAward(5) { Item =  "UberHatchet", Amount =    1 },
                new DefaultAward(6) { Item =  "Leather", Amount =        1 },
                new DefaultAward(7) { Item =  "WoodPlanks50", Amount =   1 },
                new DefaultAward(8) { Item =  "ResearchKit", Amount =    1 },
                new DefaultAward(9) { Item =  "P250", Amount =           1 },
                new DefaultAward(10) { Item = "LeatherSet", Amount =     1 },
                new DefaultAward(11) { Item = "Money5K", Amount =        1 },
                new DefaultAward(12) { Item = "M4", Amount =             1 },
                new DefaultAward(13) { Item = "Money8K", Amount =        1 },
                new DefaultAward(14) { Item = "KevlarSet", Amount =      1 },
                new DefaultAward(15) { Item = "Free", Amount =           1 },
            };
            public static readonly List<GoldAward> GoldAwards = new List<GoldAward>
            {
                new GoldAward(1) { Item =  "P250", Amount =               1 },
                new GoldAward(2) { Item =  "Paper", Amount =              1 },
                new GoldAward(3) { Item =  "Money5K", Amount =            1 },
                new GoldAward(4) { Item =  "MP5A4", Amount =              1 },
                new GoldAward(5) { Item =  "UberHatchet", Amount =        1 },
                new GoldAward(6) { Item =  "LeatherSetP250", Amount =     1 },
                new GoldAward(7) { Item =  "M4", Amount =                 1 },
                new GoldAward(8) { Item =  "WoodPlanks500", Amount =      1 },
                new GoldAward(9) { Item =  "KevlarSetM4",  Amount =       1 },
                new GoldAward(10) { Item = "SupplySignal", Amount =       1 },
                new GoldAward(11) { Item = "LowQualityMetal", Amount =    1 },
                new GoldAward(12) { Item = "Money50K", Amount =           1 },
                new GoldAward(13) { Item = "KITR3", Amount =              1 },
                new GoldAward(14) { Item = "Clan", Amount =               1 },
                new GoldAward(15) { Item = "DonateCase", Amount =         1 }
            };
        }
        private class QuestStatistic
        {
            [JsonProperty("Идентификатор")] public ulong UserID { get; set; }
            [JsonProperty("Номер квеста")] public int QuestNumber { get; set; }
            [JsonProperty("Необходимые условия")] public Dictionary<string, int> QuestRequirements { get; set; }
        }

        private class BattlePassData
        {
            [JsonProperty("Пользователи")] public List<Holder> Holders = new List<Holder>();
            [JsonProperty("Статистика")] public List<QuestStatistic> Statistics = new List<QuestStatistic>();
        }
        private BattlePassData _battlePassData;

        #endregion

        #region [FUNCTIONS] -> Функции. Получение информации о данных игрока из конфига.

        private Holder FindHolder(ulong userID) =>
            _battlePassData.Holders.Find(f => f.UserID == userID);

        private QuestStatistic FindUserStatistic(ulong userID) =>
            _battlePassData.Statistics.Find(f => f.UserID == userID);

        private static QuestAward.DefaultAward FindDefaultAward(int questNumber) =>
            QuestAward.DefaultAwards.Find(f => f.QuestNumber == questNumber);
        private static QuestAward.GoldAward FindGoldAward(int questNumber) =>
            QuestAward.GoldAwards.Find(f => f.QuestNumber == questNumber);

        #endregion

        #region [CMD] -> Battle Pass.

        [ConsoleCommand("oxide.givebattlepass")]
        private void OnConsoleCMD_GiveBattlePass(ConsoleSystem.Arg arg)
        {
            if (arg.Args.Length == 0)
            {
                Debug.Log("[Battle Pass]: Используйте - oxide.givebattlepass <ник>");
                return;
            }
            var uData = Users.Find(arg.Args[0]);
            if (uData == null)
            {
                Debug.Log($"[Battle Pass]: Игрок {arg.Args[0]} не найден.");
                return;
            }

            FindHolder(uData.SteamID).IsSubscriber = true;

            Debug.Log($"[Battle Pass]: Игроку {uData.Username} был выдан Gold BattlePass!");
            SavePluginData();
        }

        [ChatCommand("battlepassclear")]
        private void OnChatCMD_BattlePassClear(NetUser user, string cmd, string[] args)
        {
            if (user.CanAdmin())
            {
                foreach (var holder in _battlePassData.Holders)
                {
                    foreach (var statistic in _battlePassData.Statistics)
                    {
                        if (holder == null || statistic == null)
                            return;
                        holder.CurrentQuest = 1;
                        statistic.QuestNumber = 1;
                        statistic.QuestRequirements = new Dictionary<string, int> { { "playerKill", 0 } };
                    }
                }
                SavePluginData();
                rust.SendChatMessage(user, "BattlePass", "Обнулено!");
            }
        }

        #endregion

        #region [VOIDS] -> Действия плагина с игроком и конфигом.
        private void LoadPluginToPlayer(PlayerClient pClient)
        {
            if (pClient.gameObject.GetComponent<RPCHandler>() != null) 
                return;
            if (LoadedPlayers.Contains(pClient.userID)) 
                LoadedPlayers.Remove(pClient.userID);

            var rpcHandler = pClient.gameObject.AddComponent<RPCHandler>();
            rpcHandler.playerClient = pClient;
            rpcHandler.battlePass = this;

            LoadedPlayers.Add(pClient.userID);
        }
        private static void UnloadPluginFromPlayer(GameObject gameObject, Type plugin)
        {
            if (gameObject.GetComponent(plugin) != null)
                UnityEngine.Object.Destroy(gameObject.GetComponent(plugin));
        }

        private void Loaded()
        {
            foreach (var playerClient in PlayerClient.All.Where(playerClient => playerClient != null))
                LoadPluginToPlayer(playerClient);

            _battlePassData = Interface.GetMod().DataFileSystem.ReadObject<BattlePassData>("BattlePassData");

            foreach (var playerClient in PlayerClient.All.Where(playerClient => FindHolder(playerClient.userID) == null))
            {
                _battlePassData.Holders.Add(new Holder
                {
                    UserID = playerClient.userID,
                    CurrentQuest = 1,
                    IsSubscriber = false,
                    Basket = new List<string>()
                });
                _battlePassData.Statistics.Add(new QuestStatistic
                {
                    UserID = playerClient.userID,
                    QuestRequirements = new Dictionary<string, int> { { "playerKill", 0 } },
                    QuestNumber = 1
                });
                SavePluginData();
                Debug.Log($"[BattlePass]: Игрок \"{playerClient.userName}\" не был найден в конфиге и был успешно добавлен!");
            }

            timer.Repeat(300f, 0, () => SavePluginData());
        }
        private void Unload()
        {
            foreach (var loadedPlayer in LoadedPlayers)
            {
                PlayerClient playerClient; PlayerClient.FindByUserID(loadedPlayer, out playerClient);
                if (playerClient != null)
                    UnloadPluginFromPlayer(playerClient.gameObject, typeof(RPCHandler));
            }
        }

        private void OnPlayerConnected(NetUser user)
        {
            if (FindHolder(user.userID) == null)
            {
                _battlePassData.Holders.Add(new Holder
                {
                    UserID = user.userID,
                    CurrentQuest = 1,
                    IsSubscriber = false,
                    Basket = new List<string>()
                });
                _battlePassData.Statistics.Add(new QuestStatistic
                {
                    UserID = user.userID,
                    QuestRequirements = new Dictionary<string, int> { { "playerKill", 0 } },
                    QuestNumber = 1
                });

                SavePluginData();
            }

            if (user.playerClient != null) 
                LoadPluginToPlayer(user.playerClient);
        }
        private void OnPlayerDisconnected(uLink.NetworkPlayer networkPlayer)
        {
            var user = NetUser.Find(networkPlayer);
            if (user != null)
                LoadedPlayers.Remove(user.userID);
        }

        private void OnServerSave() => SavePluginData();
        private void SavePluginData() => Interface.GetMod().DataFileSystem.WriteObject("BattlePassData", _battlePassData);

        private void OnPlayerInitialized(NetUser user, PlayerMods playerMods)
        {
            playerMods.UploadSound("QuestCompleted", "http://rage.hostfun.top/project/darkrust/QuestCompleted.ogg");
        }
        #endregion

        #region [HOOKS] -> Хуки. Засчитывание действий игрока.
        private void OnItemRemoved(Inventory inventory, int slot, IInventoryItem item)
        {
            try
            {
                if (inventory == null) return;

                var lootObject = inventory.GetComponent<LootableObject>();
                if (lootObject == null || lootObject.NumberOfSlots != 12) return;

                var lootID = lootObject.networkViewID.id;
                if (LootBoxes.ContainsKey(lootID) && LootBoxes[lootID] != lootObject.transform.position)
                    LootBoxes.Remove(lootID);

                if (LootBoxes.ContainsKey(lootID)) return;
                foreach (var holder in from playerClient in PlayerClient.All
                                       where playerClient != null && inventory.IsAnAuthorizedLooter(playerClient.netPlayer)
                                       let holder = FindHolder(playerClient.userID)
                                       select holder)
                {
                    var statistic = FindUserStatistic(holder.UserID);
                    var lootObjectName = lootObject.name.Replace("(Clone)", "");

                    if (holder.CurrentQuest == 2 && lootObjectName == "BoxLoot")
                    {
                        statistic.QuestRequirements["boxes"] += 1;
                        if (statistic.QuestRequirements["boxes"] < 20)
                            return;

                        statistic.QuestRequirements.Remove("boxes"); statistic.QuestRequirements.Add("processor", 0);

                        var defaultAward = FindDefaultAward(2);
                        holder.Basket.Add(defaultAward.Item);

                        if (IsGoldSubscriber(holder.UserID)) { var goldAward = FindGoldAward(2); holder.Basket.Add(goldAward.Item); }

                        holder.CurrentQuest += 1; statistic.QuestNumber += 1;

                        var playerClient = Helper.GetPlayerClient(holder.UserID); PlayCompleteSound(playerClient);
                        return;
                    }
                    if (holder.CurrentQuest == 3 && Vector3.Distance(lootObject.transform.position, ProcessorPosition) < 4f)
                    {
                        statistic.QuestRequirements.Remove("processor"); statistic.QuestRequirements.Add("bearMutant", 0);

                        var defaultAward = FindDefaultAward(3);
                        holder.Basket.Add(defaultAward.Item);

                        if (IsGoldSubscriber(holder.UserID)) { var goldAward = FindGoldAward(3); holder.Basket.Add(goldAward.Item); }

                        holder.CurrentQuest += 1; statistic.QuestNumber += 1;

                        var playerClient = Helper.GetPlayerClient(holder.UserID); PlayCompleteSound(playerClient);
                        return;
                    }
                    if (holder.CurrentQuest == 12 && lootObjectName == "WeaponLootBox")
                    {
                        statistic.QuestRequirements["weapons"] += 1;
                        if (statistic.QuestRequirements["weapons"] < 20)
                            return;

                        holder.CurrentQuest += 1; statistic.QuestNumber += 1;

                        var defaultAward = FindDefaultAward(12);
                        holder.Basket.Add(defaultAward.Item);

                        if (IsGoldSubscriber(holder.UserID)) { var goldAward = FindGoldAward(12); holder.Basket.Add(goldAward.Item); }

                        statistic.QuestRequirements.Remove("weapons"); statistic.QuestRequirements.Add("airdrop", 0);

                        var playerClient = Helper.GetPlayerClient(holder.UserID); PlayCompleteSound(playerClient);
                        return;
                    }
                    if (holder.CurrentQuest == 13 && lootObjectName == "SupplyCrate")
                    {
                        statistic.QuestRequirements.Remove("airdrop"); statistic.QuestRequirements.Add("roll", 0);

                        var defaultAward = FindDefaultAward(13);
                        holder.Basket.Add(defaultAward.Item);

                        if (IsGoldSubscriber(holder.UserID)) { var goldAward = FindGoldAward(13); holder.Basket.Add(goldAward.Item); }

                        holder.CurrentQuest += 1; statistic.QuestNumber += 1;

                        var playerClient = Helper.GetPlayerClient(holder.UserID); PlayCompleteSound(playerClient);
                        return;
                    }
                }
            }
            catch
            {
                // ignored
            }
        }
        private void OnKilled(TakeDamage takedamage, DamageEvent damage)
        {
            try
            {
                if (damage.amount < takedamage.health) return;

                var netUser = damage.attacker.client.netUser;
                if (netUser == null) return;

                var victim = damage.victim.character ? damage.victim.character : null;
                var holder = FindHolder(damage.attacker.userID); var statistic = FindUserStatistic(holder.UserID);

                switch (holder.CurrentQuest)
                {
                    case 1:
                        {
                            var netVictim = damage.victim.client?.netUser ?? null;

                            if (victim == null || netUser == netVictim) return;
                            if (!(takedamage is HumanBodyTakeDamage)) return;

                            statistic.QuestRequirements["playerKill"] += 1;
                            if (statistic.QuestRequirements["playerKill"] < 20)
                                return;

                            statistic.QuestRequirements.Remove("playerKill"); statistic.QuestRequirements.Add("boxes", 0);

                            var defaultAward = FindDefaultAward(1);
                            holder.Basket.Add(defaultAward.Item);

                            if (IsGoldSubscriber(holder.UserID)) { var goldAward = FindGoldAward(1); holder.Basket.Add(goldAward.Item); }

                            holder.CurrentQuest += 1; statistic.QuestNumber += 1;

                            var playerClient = Helper.GetPlayerClient(holder.UserID); PlayCompleteSound(playerClient);
                        } return;
                    case 4:
                        {
                            if (victim == null || Helper.NiceName(victim.name) != "Mutant Bear")
                                return;

                            statistic.QuestRequirements["bearMutant"] += 1;
                            if (statistic.QuestRequirements["bearMutant"] < 30)
                                return;

                            statistic.QuestRequirements.Remove("bearMutant"); statistic.QuestRequirements.Add("playerHeadKill", 0);

                            var defaultAward = FindDefaultAward(4);
                            holder.Basket.Add(defaultAward.Item);

                            if (IsGoldSubscriber(holder.UserID)) { var goldAward = FindGoldAward(4); holder.Basket.Add(goldAward.Item); }

                            holder.CurrentQuest += 1; statistic.QuestNumber += 1;

                            var playerClient = Helper.GetPlayerClient(holder.UserID); PlayCompleteSound(playerClient);
                        } return;
                    case 5:
                        {
                            var netVictim = damage.victim.client?.netUser ?? null;
                            if (victim == null || netUser == netVictim) return;
                            if ((!(takedamage is HumanBodyTakeDamage)) || (damage.bodyPart != BodyPart.Head)) return;

                            statistic.QuestRequirements["playerHeadKill"] += 1;
                            if (statistic.QuestRequirements["playerHeadKill"] < 35)
                                return;

                            statistic.QuestRequirements.Remove("playerHeadKill"); statistic.QuestRequirements.Add("animals", 0);

                            var defaultAward = FindDefaultAward(5);
                            holder.Basket.Add(defaultAward.Item);

                            if (IsGoldSubscriber(holder.UserID)) { var goldAward = FindGoldAward(5); holder.Basket.Add(goldAward.Item); }

                            holder.CurrentQuest += 1;
                            statistic.QuestNumber += 1;

                            var playerClient = Helper.GetPlayerClient(holder.UserID); PlayCompleteSound(playerClient);
                        } return;
                    case 6:
                        {
                            if (victim == null || (Helper.NiceName(victim.name) != "Boar" &&
                                                   Helper.NiceName(victim.name) != "Rabbit" &&
                                                   Helper.NiceName(victim.name) != "Stag" &&
                                                   Helper.NiceName(victim.name) != "Chicken" &&
                                                   Helper.NiceName(victim.name) != "Bear" &&
                                                   Helper.NiceName(victim.name) != "Wolf")) return;

                            statistic.QuestRequirements["animals"] += 1;
                            if (statistic.QuestRequirements["animals"] < 40)
                                return;

                            statistic.QuestRequirements.Remove("animals"); statistic.QuestRequirements.Add("sulfur", 0);

                            var defaultAward = FindDefaultAward(6);
                            holder.Basket.Add(defaultAward.Item);

                            if (IsGoldSubscriber(holder.UserID)) { var goldAward = FindGoldAward(6); holder.Basket.Add(goldAward.Item); }

                            holder.CurrentQuest += 1; statistic.QuestNumber += 1;

                            var playerClient = Helper.GetPlayerClient(holder.UserID); PlayCompleteSound(playerClient);
                        } return;
                    case 8:
                        {
                            var netVictim = damage.victim.client?.netUser ?? null;
                            if (victim == null || netUser == netVictim) return;
                            if (!(takedamage is HumanBodyTakeDamage)) return;

                            statistic.QuestRequirements["playerKill"] += 1;
                            if (statistic.QuestRequirements["playerKill"] < 40)
                                return;

                            statistic.QuestRequirements.Remove("playerKill"); statistic.QuestRequirements.Add("wolf", 0);

                            var defaultAward = FindDefaultAward(8);
                            holder.Basket.Add(defaultAward.Item);

                            if (IsGoldSubscriber(holder.UserID)) { var goldAward = FindGoldAward(8); holder.Basket.Add(goldAward.Item); }

                            holder.CurrentQuest += 1;
                            statistic.QuestNumber += 1;

                            var playerClient = Helper.GetPlayerClient(holder.UserID); PlayCompleteSound(playerClient);
                        } return;
                    case 9:
                        if (victim != null && Helper.NiceName(victim.name) == "Wolf")
                        {
                            statistic.QuestRequirements["wolf"] += 1;
                            if (statistic.QuestRequirements["wolf"] < 40)
                                return;

                            statistic.QuestRequirements.Remove("wolf"); statistic.QuestRequirements.Add("grenadeResearch", 0);

                            var defaultAward = FindDefaultAward(9);
                            holder.Basket.Add(defaultAward.Item);

                            if (IsGoldSubscriber(holder.UserID)) { var goldAward = FindGoldAward(9); holder.Basket.Add(goldAward.Item); }

                            holder.CurrentQuest += 1; statistic.QuestNumber += 1;

                            var playerClient = Helper.GetPlayerClient(holder.UserID);
                            var inventory = playerClient.controllable.GetComponent<PlayerInventory>();

                            BlueprintDataBlock bp;
                            BlueprintDataBlock.FindBlueprintForItem(DatablockDictionary.GetByName("F1 Grenade"), out bp);

                            if (inventory.KnowsBP(bp))
                            {
                                statistic.QuestRequirements.Remove("grenadeResearch"); statistic.QuestRequirements.Add("weaponPart", 0);

                                var defaultAwardBp = FindDefaultAward(10);
                                holder.Basket.Add(defaultAwardBp.Item);

                                if (IsGoldSubscriber(holder.UserID)) { var goldAward = FindGoldAward(10); holder.Basket.Add(goldAward.Item); }

                                holder.CurrentQuest += 1; statistic.QuestNumber += 1;
                            }
                            PlayCompleteSound(playerClient);
                        }
                        break;
                }
            }
            catch
            {
                // ignored
            }
        }
        private void OnResearchItem(ResearchToolItem<ResearchToolDataBlock> tool, IInventoryItem item)
        {
            try
            {
                var netUser = item.inventory.GetComponent<Character>()?.netUser;
                if (netUser == null)
                    return;

                var holder = FindHolder(netUser.userID); var statistic = FindUserStatistic(holder.UserID);

                if (item.datablock.name == "F1 Grenade" && holder.CurrentQuest == 10)
                {
                    statistic.QuestRequirements.Remove("grenadeResearch"); statistic.QuestRequirements.Add("weaponPart", 0);

                    var defaultAward = FindDefaultAward(10);
                    holder.Basket.Add(defaultAward.Item);

                    if (IsGoldSubscriber(holder.UserID)) { var goldAward = FindGoldAward(10); holder.Basket.Add(goldAward.Item); }

                    holder.CurrentQuest += 1; statistic.QuestNumber += 1;

                    var playerClient = Helper.GetPlayerClient(holder.UserID); PlayCompleteSound(playerClient); SavePluginData();
                }
            }
            catch
            {
                // ignored
            }
        }
        private void OnBlueprintUse(BlueprintDataBlock bp, IBlueprintItem item)
        {
            try
            {
                var netUser = item.inventory.GetComponent<Character>()?.netUser;
                if (netUser == null)
                    return;

                var holder = FindHolder(netUser.userID); var statistic = FindUserStatistic(holder.UserID);

                if (bp.resultItem.name == "F1 Grenade" && holder.CurrentQuest == 10)
                {
                    statistic.QuestRequirements.Remove("grenadeResearch"); statistic.QuestRequirements.Add("weaponPart", 0);

                    var defaultAward = FindDefaultAward(10);
                    holder.Basket.Add(defaultAward.Item);

                    if (IsGoldSubscriber(holder.UserID)) { var goldAward = FindGoldAward(10); holder.Basket.Add(goldAward.Item); }

                    holder.CurrentQuest += 1; statistic.QuestNumber += 1;

                    var playerClient = Helper.GetPlayerClient(holder.UserID); PlayCompleteSound(playerClient); SavePluginData();
                }
            }
            catch
            {
                // ignored
            }
        }
        private void OnItemCraft(CraftingInventory inventory, BlueprintDataBlock blueprint, int amount, ulong startTime)
        {
            try
            {
                var netUser = inventory.GetComponent<Character>().netUser;

                var holder = FindHolder(netUser.userID);
                var statistic = FindUserStatistic(holder.UserID);

                if (holder.CurrentQuest == 15 && blueprint.resultItem.name == "Explosive Charge")
                {
                    statistic.QuestRequirements["C4"] += amount;
                    if (statistic.QuestRequirements["C4"] < 2)
                        return;

                    statistic.QuestRequirements.Remove("С4"); statistic.QuestRequirements.Add("FULL", 99999);

                    var defaultAward = FindDefaultAward(15);
                    holder.Basket.Add(defaultAward.Item);

                    if (IsGoldSubscriber(holder.UserID)) { var goldAward = FindGoldAward(15); holder.Basket.Add(goldAward.Item); }

                    holder.CurrentQuest += 1; statistic.QuestNumber += 1;

                    var playerClient = Helper.GetPlayerClient(holder.UserID); PlayCompleteSound(playerClient); SavePluginData();
                }
            }
            catch
            {
                // ignored
            }
        }
        private void OnGather(Inventory reciever, ResourceTarget obj, ResourceGivePair item, int collected)
        {
            if (item == null || reciever == null || obj == null || collected < 1 || reciever.networkView.owner == null)
                return;

            var netUser = NetUser.Find(reciever.networkView.owner);
            if (netUser == null)
                return;

            var holder = FindHolder(netUser.userID); var statistic = FindUserStatistic(holder.UserID);

            if (holder.CurrentQuest == 7 && item.ResourceItemName == "Sulfur Ore")
            {
                statistic.QuestRequirements["sulfur"] += 1;
                if (statistic.QuestRequirements["sulfur"] < 20)
                    return;

                statistic.QuestRequirements.Remove("sulfur"); statistic.QuestRequirements.Add("playerKill", 0);

                var defaultAward = FindDefaultAward(7);
                holder.Basket.Add(defaultAward.Item);

                if (IsGoldSubscriber(holder.UserID)) { var goldAward = FindGoldAward(7); holder.Basket.Add(goldAward.Item); }

                holder.CurrentQuest += 1; statistic.QuestNumber += 1;

                var playerClient = Helper.GetPlayerClient(holder.UserID); PlayCompleteSound(playerClient);
            }
        }
        private object OnUserCommand(IPlayer player, string command, string[] args)
        {
            try
            {
                var userID = ulong.Parse(player.Id);
                var holder = FindHolder(userID); var statistic = FindUserStatistic(userID);
                var inventory = Helper.GetPlayerClient(userID).rootControllable.GetComponent<Inventory>();

                if (holder.CurrentQuest == 14 && command == "roll")
                {
                    var paper = DatablockDictionary.GetByName("Paper");
                    var paperCount = Helper.InventoryItemCount(inventory, paper);

                    if (paperCount > 0)
                    {
                        statistic.QuestRequirements.Remove("roll"); statistic.QuestRequirements.Add("C4", 0);

                        var defaultAward = FindDefaultAward(14);
                        holder.Basket.Add(defaultAward.Item);

                        if (IsGoldSubscriber(holder.UserID))
                        { var goldAward = FindGoldAward(14); holder.Basket.Add(goldAward.Item); }

                        holder.CurrentQuest += 1; statistic.QuestNumber += 1;

                        var playerClient = Helper.GetPlayerClient(holder.UserID); PlayCompleteSound(playerClient); SavePluginData();
                    }
                }
                if (holder.CurrentQuest == 11 && command == "weapon" && args.Length > 0)
                {
                    var weaponPart1 = DatablockDictionary.GetByName("Weapon Part 1");
                    var weaponPart2 = DatablockDictionary.GetByName("Weapon Part 2");
                    var weaponPart3 = DatablockDictionary.GetByName("Weapon Part 3");
                    var weaponPart4 = DatablockDictionary.GetByName("Weapon Part 4");
                    var weaponPart5 = DatablockDictionary.GetByName("Weapon Part 5");
                    var weaponPart6 = DatablockDictionary.GetByName("Weapon Part 6");
                    var weaponPart7 = DatablockDictionary.GetByName("Weapon Part 7");

                    var weaponPart1Count = Helper.InventoryItemCount(inventory, weaponPart1);
                    var weaponPart2Count = Helper.InventoryItemCount(inventory, weaponPart2);
                    var weaponPart3Count = Helper.InventoryItemCount(inventory, weaponPart3);
                    var weaponPart4Count = Helper.InventoryItemCount(inventory, weaponPart4);
                    var weaponPart5Count = Helper.InventoryItemCount(inventory, weaponPart5);
                    var weaponPart6Count = Helper.InventoryItemCount(inventory, weaponPart6);
                    var weaponPart7Count = Helper.InventoryItemCount(inventory, weaponPart7);

                    if (weaponPart1Count + weaponPart2Count + weaponPart3Count + weaponPart4Count + weaponPart5Count + weaponPart6Count + weaponPart7Count > 0)
                    {
                        statistic.QuestRequirements.Remove("weaponPart"); statistic.QuestRequirements.Add("weapons", 0);

                        var defaultAward = FindDefaultAward(11);
                        holder.Basket.Add(defaultAward.Item);

                        if (IsGoldSubscriber(holder.UserID)) { var goldAward = FindGoldAward(11); holder.Basket.Add(goldAward.Item); }

                        holder.CurrentQuest += 1; statistic.QuestNumber += 1;

                        var playerClient = Helper.GetPlayerClient(holder.UserID); PlayCompleteSound(playerClient); SavePluginData();
                    }
                }
            }
            catch
            {
                //fuck legacy
            }
            return null;
        }

        #endregion

        #region [VOIDS] -> Действия с игроком из мода.

        private void GiveItemFromBasket(ulong userID, string item)
        {
            var holder = FindHolder(userID);
            holder.Basket.Remove(item);

            SavePluginData();

            PlayerClient playerClient;
            PlayerClient.FindByUserID(userID, out playerClient);

            if (item == "Clan")
            {
                rust.SendChatMessage(NetUser.FindByUserID(userID), "Получение награды",
                    "[COLOR # FF0000]Для получения клана 10-го уровня, обратитесь к Главному Администратору!");
            }
            if (item == "Cloth")
            {
                Helper.GiveItem(playerClient, "Cloth", 100);
            }
            if (item == "DonateCase")
            {
                var ranks = new[] { 2, 5, 10, 6, 12, 11, 3, 4 };
                var random = Random.Range(0, 9);

                if (random == 6 || random == 12)
                    random = Random.Range(0, 9);

                ConsoleSystem.Run($"serv.users \"{Users.GetBySteamID(userID).Username}\" rank {ranks[random]}");
            }
            if (item == "Free")
            {
                ConsoleSystem.Run($"serv.users \"{Users.GetBySteamID(userID).Username}\" rank 24");
            }
            if (item == "KevlarSet")
            {
                Helper.GiveItem(playerClient, "Kevlar Helmet");
                Helper.GiveItem(playerClient, "Kevlar Vest");
                Helper.GiveItem(playerClient, "Kevlar Pants");
                Helper.GiveItem(playerClient, "Kevlar Boots");
            }
            if (item == "KevlarSetM4")
            {
                Helper.GiveItem(playerClient, "Kevlar Helmet");
                Helper.GiveItem(playerClient, "Kevlar Vest");
                Helper.GiveItem(playerClient, "Kevlar Pants");
                Helper.GiveItem(playerClient, "Kevlar Boots");
                Helper.GiveItem(playerClient, "M4");
            }
            if (item == "KitR3")
            {
                Helper.GiveItem(playerClient, "Gunpowder", 750);
                Helper.GiveItem(playerClient, "Wood Planks", 500);
                Helper.GiveItem(playerClient, "Sulfur Ore", 750);
                Helper.GiveItem(playerClient, "Metal Ore", 750);
                Helper.GiveItem(playerClient, "Low Grade Fuel", 500);
                Helper.GiveItem(playerClient, "Low Quality Metal", 500);
                Helper.GiveItem(playerClient, "Wood", 1500);
            }
            if (item == "Leather")
            {
                Helper.GiveItem(playerClient, "Leather", 50);
            }
            if (item == "LeatherSet")
            {
                Helper.GiveItem(playerClient, "Leather Helmet");
                Helper.GiveItem(playerClient, "Leather Vest");
                Helper.GiveItem(playerClient, "Leather Pants");
                Helper.GiveItem(playerClient, "Leather Boots");
            }
            if (item == "LeatherSetP250")
            {
                Helper.GiveItem(playerClient, "Leather Helmet");
                Helper.GiveItem(playerClient, "Leather Vest");
                Helper.GiveItem(playerClient, "Leather Pants");
                Helper.GiveItem(playerClient, "Leather Boots");
                Helper.GiveItem(playerClient, "P250");
            }
            if (item == "LowQualityMetal")
            {
                Helper.GiveItem(playerClient, "Low Quality Metal", 300);
            }
            if (item == "M4")
            {
                Helper.GiveItem(playerClient, "M4");
            }
            if (item == "MetalOre")
            {
                Helper.GiveItem(playerClient, "Metal Ore", 100);
            }
            if (item == "Money5K")
            {
                var balance = Economy.Database[userID].Balance;
                Economy.Database[userID].Balance = balance + 5000;
            }
            if (item == "Money8K")
            {
                var balance = Economy.Database[userID].Balance;
                Economy.Database[userID].Balance = balance + 8000;
            }
            if (item == "Money50K")
            {
                var balance = Economy.Database[userID].Balance;
                Economy.Database[userID].Balance = balance + 50000;
            }
            if (item == "MP5A4")
            {
                Helper.GiveItem(playerClient, "MP5A4");
            }
            if (item == "P250")
            {
                Helper.GiveItem(playerClient, "P250");
            }
            if (item == "Paper")
            {
                Helper.GiveItem(playerClient, "Paper");
            }
            if (item == "ResearchKit")
            {
                Helper.GiveItem(playerClient, "Research Kit 1");
            }
            if (item == "Stones")
            {
                Helper.GiveItem(playerClient, "Stones", 150);
            }
            if (item == "SulfurOre")
            {
                Helper.GiveItem(playerClient, "Sulfur Ore", 100);
            }
            if (item == "SupplySignal")
            {
                Helper.GiveItem(playerClient, "Supply Signal");
            }
            if (item == "UberHatchet")
            {
                Helper.GiveItem(playerClient, "Uber Hatchet");
            }
            if (item == "WoodPlanks50")
            {
                Helper.GiveItem(playerClient, "Wood Planks", 50);
            }
            if (item == "WoodPlanks500")
            {
                Helper.GiveItem(playerClient, "Wood Planks", 500);
            }
        }
        private void PlayCompleteSound(PlayerClient playerClient)
        {
            PlayerMods.playerClient = playerClient;
            PlayerMods.PlaySound("QuestCompleted");
        }

        #endregion

        #region [VARIABLES] -> Переменные.

        private static readonly Vector3 ProcessorPosition = new Vector3(6731.9f, 344.4f, -4114.5f);
        private static readonly Dictionary<int, Vector3> LootBoxes = new Dictionary<int, Vector3>();
        private static readonly List<ulong> LoadedPlayers = new List<ulong>();
        private bool IsGoldSubscriber(ulong userID) => FindHolder(userID).IsSubscriber;

        private static readonly PlayerMods PlayerMods = new PlayerMods();

        #endregion

        #region [CLASS] -> Слушатель RPC-Пакетов. Взаимодействие с модом.

        internal class RPCHandler : MonoBehaviour
        {
            public PlayerClient playerClient;
            public BSFBattlePass battlePass;
            private Facepunch.NetworkView _networkView;

            [RPC]
            public void SendClickedItem(string item)
            {
                battlePass.GiveItemFromBasket(playerClient.userID, item);
            }

            [RPC]
            public void GetBasketItems()
            {
                SetBasketItems();
            }

            [RPC]
            public void GetCurrentQuest()
            {
                SetCurrentQuest();
            }

            [RPC]
            public void GetSubscriberStatus()
            {
                SetSubscriberStatus();
            }

            public void SetSubscriberStatus()
            {
                SendRPC("SetSubscriberStatus", playerClient, battlePass.IsGoldSubscriber(playerClient.userID));
            }
            public void SetBasketItems()
            {
                var holder = battlePass.FindHolder(playerClient.userID);
                foreach (var basket in holder.Basket)
                {
                    SendRPC("SetBasketItems", playerClient, basket);
                }
            }
            public void SetCurrentQuest()
            {
                var holder = battlePass.FindHolder(playerClient.userID);
                SendRPC("SetCurrentQuest", playerClient, holder.CurrentQuest - 1);
            }

            private void Start() => _networkView = GetComponent<Facepunch.NetworkView>();
            public void SendRPC(string rpcName, PlayerClient player, params object[] param) => _networkView.RPC(rpcName, player.netPlayer, param);
        }

        #endregion
    }
}