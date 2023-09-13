using Oxide.Core;
using UnityEngine;
using RustExtended;

using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using RageMods;

namespace Oxide.Plugins
{
    [Info("BBattlePass", "systemXcrackedZ", "4.4.1")]
    [Description("Battle Pass System for Rust Legacy server: Dark Rust.")]
    internal class BBattlePass : RustLegacyPlugin
    {
        [Flags]
        private enum ActionType
        {
            Null = -1,
            KillPlayer = 0,

            Craft = 1,

            LootSupply = 2,
            LootWeapon = 3,

            GatherWood = 4,
            GatherOres = 5,

            Research = 6,

            KillAnimals = 7,
            KillMutants = 8,

            LootRT = 9,
            RollPaper = 10,
            UseRecycler = 11
        }

        private PlayerMods playerMods = new PlayerMods();
        private List<ulong> loadedPlayers = new List<ulong>();

        private class Quest
        {
            public Quest(ActionType actionType, int actionsCount, string rewardItems, string rewardItemsGold, string result = "null")
            {
                ActionType = actionType; ActionsCount = actionsCount;
                RewardItems = rewardItems; RewardItemsGold = rewardItemsGold;
                Result = result;
            }

            [JsonProperty("Тип действия")] public ActionType ActionType { get; set; }
            [JsonProperty("Количество действий")] public int ActionsCount { get; set; }

            [JsonProperty("Награда (предметы)")] public string RewardItems { get; set; }
            [JsonProperty("Награда GOLD (предметы)")] public string RewardItemsGold { get; set; }

            [JsonProperty("Результат")] public string Result { get; set; }
        }
        private class QuestUser
        {
            public QuestUser(bool isSubscriber, int quest, int progress, List<string> inventory)
            {
                IsSubscriber = isSubscriber;
                Quest = quest;
                Progress = progress;
                Inventory = inventory;
            }

            [JsonProperty("Статус подписки")] public bool IsSubscriber { get; set; }
            [JsonProperty("Номер задания")] public int Quest { get; set; }
            [JsonProperty("Прогресс выполнения")] public int Progress { get; set; }
            [JsonProperty("Инвентарь")] public List<string> Inventory { get; set; }
        }

        private QuestUser GetUserData(ulong userID)
        {
            QuestUser questUser;
            users.TryGetValue(userID, out questUser);

            if (questUser == null)
            {
                questUser = new QuestUser(false, 1, 0, new List<string>());
                users.Add(userID, questUser);
                SavePluginData();
            }

            return questUser;
        }

        private Dictionary<int, Quest> quests;
        private Dictionary<ulong, QuestUser> users;

        private object OnPlayerAction(NetUser user, QuestUser QuestUser, int count = 1)
        {
            int questID = QuestUser.Quest;
            if (questID == -1) return null;

            IncreaseCompletedActions(QuestUser, count);

            Quest quest = quests[questID];
            if (quest.ActionsCount == 1)
            {
                CompleteQuest(user, questID);
                return null;
            }

            if (quest.ActionsCount > 1) if (QuestUser.Progress >= quest.ActionsCount) CompleteQuest(user, questID);

            return null;
        }
        private void IncreaseCompletedActions(QuestUser user, int count = 1)
        {
            user.Progress += count;
            SavePluginData();
        }
        private void CompleteQuest(NetUser user, int questID)
        {
            QuestUser QuestUser = GetUserData(user.userID);
            if (QuestUser.Quest == questID)
            {
                QuestUser.Quest++;
                Quest quest = quests[questID];

                QuestUser.Inventory.Add(quest.RewardItems);
                if (QuestUser.IsSubscriber) QuestUser.Inventory.Add(quest.RewardItemsGold);

                QuestUser.Progress = 0;

                PlayCompleteSound(user.playerClient);
            }
        }

        private void PlayCompleteSound(PlayerClient playerClient)
        {
            playerMods.playerClient = playerClient;
            playerMods.PlaySound("QuestCompleted");
        }

        #region [HOOK] [OnKilled] (ActionType 0 - 9)
        private void OnKilled(TakeDamage damage, DamageEvent evt)
        {
            try
            {
                if (evt.amount < damage.health) return;

                NetUser netUser = evt.attacker.client?.netUser ?? null;
                if (netUser == null) return;

                QuestUser user = GetUserData(netUser.userID);
                if (user.Quest == -1) return;

                Quest quest = quests[user.Quest];

                if (quest.ActionType == ActionType.KillPlayer && damage is HumanBodyTakeDamage)
                {
                    NetUser victim = evt.victim.client?.netUser ?? null;
                    if (victim == null || netUser == victim) return;

                    OnPlayerAction(netUser, user, Economy.Get(victim.userID).PlayersKilled);
                    return;
                }

                if (quest.ActionType == ActionType.KillAnimals)
                {
                    Character victim = evt.victim.character ?? null;
                    if (victim != null)
                    {
                        string niceName = Helper.NiceName(victim.name);
                        if (niceName == "Wolf" || niceName == "Bear" || niceName == "Boar" || niceName == "Rabbit" || niceName == "Chicken") OnPlayerAction(netUser, user);
                    }
                }
                if (quest.ActionType == ActionType.KillMutants)
                {
                    Character victim = evt.victim.character ?? null;
                    if (victim != null)
                    {
                        string niceName = Helper.NiceName(victim.name);
                        if (niceName == "Mutant Wolf" || niceName == "Mutant Bear") OnPlayerAction(netUser, user);
                    }
                }
            }
            catch { }
        }
        #endregion
        #region [HOOK] [OnItemCraft] (ActionType 10)
        private void OnItemCraft(CraftingInventory inventory, BlueprintDataBlock blueprint, int amount, ulong startTime)
        {
            NetUser netUser = NetUser.Find(inventory.networkView.owner);
            PlayerInventory playerInv = inventory as PlayerInventory;
            if (playerInv == null || netUser == null) return;

            QuestUser user = GetUserData(netUser.userID);
            Quest quest = quests[user.Quest];

            if (quest.ActionType == ActionType.Craft && blueprint.resultItem.name == quest.Result) OnPlayerAction(netUser, user, amount);
        }
        #endregion
        #region [HOOK] [OnLoot] (ActionType 11 - 15)
        private Dictionary<int, Vector3> lootableList = new Dictionary<int, Vector3>();
        private void OnItemRemoved(Inventory lootableInventory, int slot, IInventoryItem item)
        {
            if (lootableInventory == null) return;

            LootableObject lootableObject = lootableInventory.GetComponent<LootableObject>();
            if (lootableObject != null && lootableObject.destroyOnEmpty && lootableObject.NumberOfSlots == 12)
            {
                int lootableID = lootableObject.networkViewID.id;
                if (lootableID == 0) return;

                if (lootableList.ContainsKey(lootableID) && lootableList[lootableID] != lootableObject.transform.position) lootableList.Remove(lootableID);
                if (!lootableList.ContainsKey(lootableID))
                {
                    foreach (PlayerClient playerClient in PlayerClient.All)
                    {
                        if (playerClient != null && lootableInventory.IsAnAuthorizedLooter(playerClient.netPlayer))
                        {
                            string lootableName = lootableObject.name.Replace("(Clone)", "");

                            QuestUser user = GetUserData(playerClient.userID);

                            Quest quest = quests[user.Quest];
                            if (quest.ActionType == ActionType.LootRT && (lootableName == "WeaponLootBox" || lootableName == "BoxLoot" || lootableName == "AmmoLootBox" || lootableName == "MedicalLootBox"))
                            {
                                OnPlayerAction(playerClient.netUser, user);
                                lootableList.Add(lootableID, lootableObject.transform.position);
                            }
                            if (quest.ActionType == ActionType.LootSupply && lootableName == "SupplyCrate")
                            {
                                OnPlayerAction(playerClient.netUser, user);
                                lootableList.Add(lootableID, lootableObject.transform.position);
                            }
                            if (quest.ActionType == ActionType.LootWeapon && lootableName == "WeaponLootBox")
                            {
                                OnPlayerAction(playerClient.netUser, user);
                                lootableList.Add(lootableID, lootableObject.transform.position);
                            }
                        }
                    }
                }
            }
        }
        #endregion
        #region [HOOK] [OnGather] (ActionType 16 - 19)
        private void OnGather(Inventory inv, ResourceTarget obj, ResourceGivePair resource, int count)
        {
            if (resource == null || inv == null || obj == null || count < 1 || inv.networkView.owner == null) return;
            NetUser netUser = NetUser.Find(inv.networkView.owner);
            if (netUser == null) return;

            QuestUser user = GetUserData(netUser.userID);
            ClanData clan = Clans.Find(netUser.userID);

            int bonus = 0;
            if (clan != null) bonus = (int)(count*clan.Level.BonusGatheringWood / 100);

            Quest quest = quests[user.Quest];
            if (quest.ActionType == ActionType.GatherWood && resource.ResourceItemName == "Wood") OnPlayerAction(netUser, user, bonus + count);
            if (quest.ActionType == ActionType.GatherOres && (resource.ResourceItemName == "Metal Ore" || resource.ResourceItemName == "Sulfur Ore")) OnPlayerAction(netUser, user, bonus + count);
        }
        #endregion
        #region [HOOK] [OnResearchItem] (ActionType 20)
        private void OnResearchItem(ResearchToolItem<ResearchToolDataBlock> tool, IInventoryItem item)
        {
            try
            {
                NetUser netUser = item.inventory.GetComponent<Character>()?.netUser;
                if (netUser == null) return;

                QuestUser user = GetUserData(netUser.userID);
                if (user.Quest == -1) return;

                Quest quest = quests[user.Quest];
                if (quest.ActionType == ActionType.Research) OnPlayerAction(netUser, user);
            }
            catch { }
        }
        #endregion
        #region [HOOK] [OnPaperRoll] (ActionType 25)
        private void OnPaperRoll(NetUser netUser)
        {
            if (netUser == null) return;

            QuestUser user = GetUserData(netUser.userID);
            if (user.Quest == -1) return;

            Quest quest = quests[user.Quest];
            if (quest.ActionType == ActionType.RollPaper) OnPlayerAction(netUser, user);
        }
        #endregion
        #region [HOOK] [OnUseRecycler] (ActionType 26)
        private void OnUseRecycler(NetUser netUser)
        {
            if (netUser == null) return;

            QuestUser user = GetUserData(netUser.userID);
            if (user.Quest == -1) return;

            Quest quest = quests[user.Quest];
            if (quest.ActionType == ActionType.UseRecycler) OnPlayerAction(netUser, user);
        }
        #endregion

        [ConsoleCommand("oxide.givebattlepass")]
        private void OnConsoleCMD_GiveBattlePass(ConsoleSystem.Arg arg)
        {
            if (arg.Args.Length == 0)
            {
                Debug.Log("[Battle Pass]: Используйте - oxide.givebattlepass <ник>");
                return;
            }
            UserData uData = Users.All.Find(f => f.Username.ToLower().Contains(arg.Args[0]) || f.Username.ToLower() == arg.Args[0]);
            if (uData == null)
            {
                Debug.Log($"[Battle Pass]: Игрок {arg.Args[0]} не найден.");
                return;
            }

            GetUserData(uData.SteamID).IsSubscriber = true;

            Debug.Log($"[Battle Pass]: Игроку {uData.Username} был выдан Gold BattlePass!");
            SavePluginData();
        }

        [ChatCommand("bpclear")]
        private void OnCMD_BPClear(NetUser user, string cmd, string[] args)
        {
            if (user.admin)
            {
                foreach (ulong questUserID in new List<ulong>(users.Keys))
                {
                    QuestUser questUser = GetUserData(questUserID);
                    questUser.Quest = 1;
                    questUser.Progress = 0;
                    if (questUser.Inventory.Count == 0 && !questUser.IsSubscriber) users.Remove(questUserID);    
                }
                SavePluginData();
                rust.SendChatMessage(user, "BattlePass", "Обнулено!");
            }
        }
        [ChatCommand("bpsetlevel")]
        private void OnCMD_BPSetLevel(NetUser netUser, string cmd, string[] args)
        {
            if (netUser.admin)
            {
                if (args.Length == 0)
                {
                    rust.SendChatMessage(netUser, "BattlePass", "Использование команды: /bpsetlevel <квест>");
                    return;
                }
                QuestUser user = GetUserData(netUser.userID);

                int questID;
                try { questID = int.Parse(args[0]); }
                catch
                {
                    rust.SendChatMessage(netUser, "BattlePass", "Использование команды: /bpsetlevel <квест>");
                    return;
                }

                user.Quest = questID;
                rust.SendChatMessage(netUser, "BattlePass", $"Успешно установлен квест {questID}!");
                SavePluginData();
            }
        }
        [ChatCommand("bpsetprogress")]
        private void OnCMD_BPSetScore(NetUser netUser, string cmd, string[] args)
        {
            if (netUser.admin)
            {
                if (args.Length == 0)
                {
                    rust.SendChatMessage(netUser, "BattlePass", "Использование команды: /bpsetscore <прогресс>");
                    return;
                }
                QuestUser user = GetUserData(netUser.userID);

                int questID = user.Quest;
                if (questID == -1) return;

                int progress;
                try { progress = int.Parse(args[0]); }
                catch 
                {
                    rust.SendChatMessage(netUser, "BattlePass", "Использование команды: /bpsetscore <прогресс>");
                    return; 
                }

                OnPlayerAction(netUser, user, progress);
                rust.SendChatMessage(netUser, "BattlePass", $"Успешно установлен прогресс {progress}!");
                SavePluginData();
            }
        }

        private void LoadPluginToPlayer(PlayerClient pClient)
        {
            if (pClient.gameObject.GetComponent<BattlePassVM>() != null) return;
            if (loadedPlayers.Contains(pClient.userID)) loadedPlayers.Remove(pClient.userID);

            BattlePassVM vm = pClient.gameObject.AddComponent<BattlePassVM>();
            vm.PlayerClient = pClient;
            vm.BattlePass = this;

            loadedPlayers.Add(pClient.userID);
        }
        private void UnloadPluginFromPlayer(GameObject gameObject, Type plugin)
        {
            if (gameObject.GetComponent(plugin) != null) UnityEngine.Object.Destroy(gameObject.GetComponent(plugin));
        }

        private void Loaded()
        {
            quests = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<int, Quest>>("BattlePassConfig");
            if (quests == null || quests.Count == 0)
            {
                quests = new Dictionary<int, Quest>
                {
                    { 1, new Quest(ActionType.GatherWood, 1500, "250WOOD", "1KWOOD") },
                    { 2, new Quest(ActionType.GatherOres, 500, "FireHatchet", "FireHatchet") },
                    { 3, new Quest(ActionType.KillAnimals, 20, "LCLGF", "LCLGF2") },
                    { 4, new Quest(ActionType.KillPlayer, 10, "Pipes", "Pipe9mmPistol") },
                    { 5, new Quest(ActionType.KillMutants, 30,  "1KMONEY", "5KMoney") },
                    { 6, new Quest(ActionType.KillPlayer, 20, "R9mm", "LSetP250") },
                    { 7, new Quest(ActionType.LootRT, 30, "MedRad", "SmallRad") },
                    { 8, new Quest(ActionType.UseRecycler, 1, "24LQ", "50LQ") },
                    { 9, new Quest(ActionType.KillPlayer, 50, "LSetP250", "M4KSet") },
                    { 10, new Quest(ActionType.Research, 1, "1-5KMONEY", "8KMONEY") },
                    { 11, new Quest(ActionType.LootWeapon, 15, "M4", "allammo") },
                    { 12, new Quest(ActionType.RollPaper, 1, "Paper", "5Paper") },
                    { 13, new Quest(ActionType.KillPlayer, 65, "KSet", "SOLDAT") },
                    { 14, new Quest(ActionType.LootSupply, 1, "5KMoney", "15KMONEY") },
                    { 15, new Quest(ActionType.Craft, 2, "FREE", "KING", "Explosive Charge") }
                };
            }
            users = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, QuestUser>>("BattlePassUserData");

            SavePluginData();
        }
        private void Unload()
        {
            foreach (ulong loadedPlayer in loadedPlayers)
            {
                PlayerClient playerClient; PlayerClient.FindByUserID(loadedPlayer, out playerClient);
                if (playerClient != null)
                    UnloadPluginFromPlayer(playerClient.gameObject, typeof(BattlePassVM));
            }

            SavePluginData();
        }
        private void OnServerSave()
        {
            SavePluginData();
        }

        private void OnPlayerConnected(NetUser user)
        {
            if (user.playerClient != null)
                LoadPluginToPlayer(user.playerClient);
        }
        private void OnPlayerDisconnected(uLink.NetworkPlayer networkPlayer)
        {
            NetUser user = NetUser.Find(networkPlayer);
            if (user != null) loadedPlayers.Remove(user.userID);
        }
        private void OnPlayerInitialized(NetUser user, PlayerMods playerMods)
        {
            playerMods.UploadSound("QuestCompleted", "http://198.244.249.28/project/darkrust/BP/QuestCompleted.ogg");
        }

        private void SavePluginData()
        {
            Interface.GetMod().DataFileSystem.WriteObject("BattlePassConfig", quests);
            Interface.GetMod().DataFileSystem.WriteObject("BattlePassUserData", users);
        }

        public void GiveItemFromBasket(ulong userID, string item)
        {
            QuestUser holder = GetUserData(userID);
            if (!holder.Inventory.Contains(item)) return;
            holder.Inventory.Remove(item);

            SavePluginData();

            PlayerClient playerClient;
            if (PlayerClient.FindByUserID(userID, out playerClient))
            {
                switch (item)
                {
                    case "R9mm": 
                        {
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Revolver"));
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("9mm Ammo"), 50);
                            break;
                        }
                    case "LSetP250":
                        {
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Leather Helmet"));
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Leather Vest"));
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Leather Pants"));
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Leather Boots"));
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("P250"));
                            break;
                        }
                    case "M4":
                        {
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("M4"));
                            break;
                        }
                    case "MedRad":
                        {
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Rad Suit Helmet"));
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Rad Suit Vest"));
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Rad Suit Pants"));
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Rad Suit Boots"));
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Large Medkit"), 10);
                            break;
                        }
                    case "Paper":
                        {
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Paper"), 5);
                            break;
                        }
                    case "Pipes":
                        {
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Pipe Shotgun"), 2);
                            break;
                        }
                    case "24LQ":
                        {
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Low Quality Metal"), 24);
                            break;
                        }
                    case "250WOOD":
                        {
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Wood"), 250);
                            break;
                        }
                    case "1KMONEY":
                        {
                            Economy.BalanceAdd(playerClient.userID, 1000);
                            break;
                        }
                    case "1-5KMONEY":
                        {
                            Economy.BalanceAdd(playerClient.userID, 1500);
                            break;
                        }
                    case "FireHatchet":
                        {
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Uber Hatchet"));
                            break;
                        }
                    case "FREE":
                        {
                            ConsoleSystem.Run($"serv.users \"{Users.GetBySteamID(userID).Username}\" rank 24");
                            break;
                        }
                    case "KSet":
                        {
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Kevlar Helmet"));
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Kevlar Vest"));
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Kevlar Pants"));
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Kevlar Boots"));
                            break;
                        }
                    case "LCLGF":
                        {
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Leather"), 20);
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Cloth"), 50);
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Low Grade Fuel"), 50);
                            break;
                        }
                    case "5KMoney":
                        {
                            Economy.BalanceAdd(playerClient.userID, 5000);
                            break;
                        }


                    case "8KMoney":
                        {
                            Economy.BalanceAdd(playerClient.userID, 8000);
                            break;
                        }
                    case "15KMoney":
                        {
                            Economy.BalanceAdd(playerClient.userID, 15000);
                            break;
                        }
                    case "allammo":
                        {
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("556 Ammo"), 500);
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("9mm Ammo"), 250);
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Shotgun Shells"), 125);
                            break;
                        }
                    case "50LQ":
                        {
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Low Quality Metal"), 50);
                            break;
                        }
                    case "5Paper":
                        {
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Paper"), 5);
                            break;
                        }
                    case "1KWOOD":
                        {
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Wood"), 1000);
                            break;
                        }
                    case "KING":
                        {
                            ConsoleSystem.Run($"serv.users \"{Users.GetBySteamID(userID).Username}\" rank 24");
                            break;
                        }
                    case "LCLGF2":
                        {
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Leather"), 100);
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Cloth"), 150);
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Low Grade Fuel"), 150);
                            break;
                        }
                    case "M4KSet":
                        {
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("M4"));
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Kevlar Helmet"));
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Kevlar Vest"));
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Kevlar Pants"));
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Kevlar Boots"));
                            break;
                        }
                    case "SmallRad":
                        {
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Rad Suit Helmet"), 2);
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Rad Suit Vest"), 2);
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Rad Suit Pants"), 2);
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Rad Suit Boots"), 2);
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Small Rations"), 50);
                            break;
                        }
                    case "SOLDAT":
                        {
                            ConsoleSystem.Run($"serv.kit \"{Users.GetBySteamID(userID).Username}\" soldat");
                            break;
                        }
                    case "Pipe9mmPistol":
                        {
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Pipe Shotgun"), 3);
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("9mm Pistol"));
                            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("9mm Ammo"), 50);
                            break;
                        }
                }
            }
        }

        internal class BattlePassVM : MonoBehaviour
        {
            public PlayerClient PlayerClient;
            public BBattlePass BattlePass;
            private new Facepunch.NetworkView networkView;

            private void Start() => networkView = GetComponent<Facepunch.NetworkView>();

            [RPC]
            public void SendClickedItem(string item)
            {
                BattlePass.GiveItemFromBasket(PlayerClient.userID, item);
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

            [RPC]
            public void GetProgress()
            {
                SetProgress();
            }

            private void SetProgress()
            {
                QuestUser holder = BattlePass.GetUserData(PlayerClient.userID);

                int current = holder.Progress;
                int needed = BattlePass.quests[holder.Quest].ActionsCount;

                SendRPC("SetProgress", PlayerClient, current, needed);
            }

            public void SetSubscriberStatus()
            {
                SendRPC("SetSubscriberStatus", PlayerClient, BattlePass.GetUserData(PlayerClient.userID).IsSubscriber);
            }
            public void SetBasketItems()
            {
                QuestUser holder = BattlePass.GetUserData(PlayerClient.userID);
                foreach (string item in holder.Inventory) SendRPC("SetBasketItems", PlayerClient, item);
            }
            public void SetCurrentQuest()
            {
                QuestUser holder = BattlePass.GetUserData(PlayerClient.userID);
                SendRPC("SetCurrentQuest", PlayerClient, holder.Quest - 1);
            }

            public void SendRPC(string rpcName, PlayerClient player, params object[] param) => networkView.RPC(rpcName, player.netPlayer, param);
        }
    }
}