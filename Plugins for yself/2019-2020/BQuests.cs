using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using RustExtended;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BQuests", "systemXcrackedZ", "1.0.0")]
    internal class BQuests : RustLegacyPlugin
    {
        [PluginReference] private Plugin BTeaBoosts;

        private readonly List<ulong> _loadedPlayers = new List<ulong>();

        public class Quest
        {
            public Quest(int iD, string branch, string description, int neededActions, Dictionary<string, int> rewards, Dictionary<string, int> arguments)
            {
                ID = iD;
                Branch = branch;
                Description = description;
                NeededActions = neededActions;
                Rewards = rewards;
                Arguments = arguments;
            }
            public int ID { get; set; }
            public string Branch { get; set; }
            public string Description { get; set; }
            public int NeededActions { get; set; }
            public Dictionary<string, int> Rewards { get; set; }
            public Dictionary<string, int> Arguments { get; set; }
        }
        public static List<Quest> Quests = new List<Quest>
        {
            /*  +  */new Quest(1, "PVE", "Убить 15 мутантов", 15, 
                new Dictionary<string, int>{{"Primed 556 Casing",125}}, 
                new Dictionary<string, int>{{"Mutant", 15}}),
            /*  +  */new Quest(2, "PVE", "Добыть 500 дерева", 500, 
                new Dictionary<string, int>{{"Money",1000}}, 
                new Dictionary<string, int>{{"Wood", 500}}),
            /*  +  */new Quest(3, "PVE", "Убить оленя и получить рога", 2, 
                new Dictionary<string, int> {{"Primed 556 Casing",150}}, 
                new Dictionary<string, int> {{"Stag", 1}, {"Horns", 1}}),
            /*  +  */new Quest(4, "PVE", "Нафармить по 150 руды каждого вида", 300, 
                new Dictionary<string, int>{{"Rad Suit Boots",1},{"Rad Suit Pants",1},{"Rad Suit Vest",1},{"Rad Suit Helmet",1},{"Anti-Radiation Pills", 20},{"Primed 556 Casing",200}}, 
                new Dictionary<string, int>{{"Sulfur", 150}, {"Metal", 150}}),
            /*  +  */new Quest(5, "PVE", "Залутать 30 любых ящиков", 30, 
                new Dictionary<string, int>{{"Low Quality Metal",40},{"Primed 556 Casing",200}}, 
                new Dictionary<string, int>{{"Box", 30}}),
            /*  +  */new Quest(6, "PVE", "Убить 15 кроликов", 15, 
                new Dictionary<string, int>{{"Uber Hatchet",1}}, 
                new Dictionary < string, int > { { "Rabbit", 15 } }),
            /*  +  */new Quest(7, "PVE", "Добыть 1000 дерева и 400 руды", 1400, 
                new Dictionary<string, int>{{"Primed 556 Casing",400},{"Small Water Bottle",1}}, 
                new Dictionary < string, int > { { "Wood", 1000 }, {"Ores", 400} }),
            /*  +  */new Quest(8, "PVE", "Залутать Аир Дроп", 1, 
                new Dictionary<string, int>{{"Primed 556 Casing",750}}, 
                new Dictionary < string, int > { { "AirDrop", 1 } }),
            /*  +  */new Quest(9, "PVE", "Скрафтить 10 гранат и 2 с4", 12, 
                new Dictionary<string, int>{{"Kevlar Boots",1},{"Kevlar Pants",1},{"Kevlar Vest",1},{"Kevlar Helmet",1},{"M4",1},{"556 Ammo",100}}, 
                new Dictionary < string, int > { { "C4", 2 }, {"F1 Grenade", 10} }),
            /*  +  */new Quest(10, "PVE", "Взорвать одну стену", 1, 
                new Dictionary<string, int>{{"Primed 556 Casing",1000}}, 
                new Dictionary<string, int>{{"Wall", 1}}),

            /*  +  */new Quest(1, "PVP", "Убить 5 волков и 5 медведей", 10, 
                new Dictionary<string, int>{{"Metal Fragments",250},{"Primed 556 Casing",50}}, 
                new Dictionary<string, int>{{"Wolf", 5}, {"Bear", 5}}),
            /*  +  */new Quest(2, "PVP", "Скрафтить 2 револьвера", 2, 
                new Dictionary<string, int>{{"Pipe Shotgun",2},{"Primed 556 Casing",70}}, 
                new Dictionary<string, int>{{"Revolver", 2}}),
            /*  +  */new Quest(3, "PVP", "Убить 5 игроков с Pipe Shotgun", 5, 
                new Dictionary<string, int>{{"Money",1000},}, 
                new Dictionary<string, int>{{"PipeKill", 5}}),
            /*  +  */new Quest(4, "PVP", "Залутать 5 военных ящиков", 5, 
                new Dictionary<string, int>{{"P250",1},{"Primed 556 Casing",100}}, 
                new Dictionary<string, int>{{"WeaponBox", 5}}),
            /*  +  */new Quest(5, "PVP", "Убить 30 мутантов", 30, 
                new Dictionary<string, int>{{"Research Kit 1",1},{"Primed 556 Casing",250}}, 
                new Dictionary<string, int>{{"Mutants", 30}}),
            /*  +  */new Quest(6, "PVP", "Убить 20 игроков", 20, 
                new Dictionary<string, int>{{"Leather Boots",1},{"Leather Pants",1},{"Leather Vest",1},{"Leather Helmet",1},{"9mm Ammo",100},}, 
                new Dictionary < string, int > { { "Players", 20 } }),
            /*  +  */new Quest(7, "PVP", "Убить КАЖДОГО вида животных(в том числе и мутантов)", 8, 
                new Dictionary<string, int>{{"M4",1},{"556 Ammo",250}}, 
                new Dictionary < string, int > { { "Wolf", 1 }, {"Bear", 1}, {"Mutant Wolf", 1}, {"Mutant Bear", 1}, {"Stag", 1}, {"Boar", 1}, {"Rabbit", 1}, {"Chicken", 1} }),
            /*  +  */new Quest(8, "PVP", "Скрафтить 2 M4 2 P250 2 Shotgun", 6, 
                new Dictionary<string, int>{{"Primed 556 Casing",500},{"Low Quality Metal",30}}, 
                new Dictionary < string, int > { { "M4", 2 }, {"P250", 2}, {"Shotgun", 2} }),
            /*  +  */new Quest(9, "PVP", "Залутать Аир-Дроп", 1, 
                new Dictionary<string, int>{{"Gunpowder",500},{"Primed 556 Casing",300}}, 
                new Dictionary < string, int > { { "AirDrop", 1 } }),
            /*  +  */new Quest(10, "PVP", "Убить человека в зоне рейдблока", 1, 
                new Dictionary<string, int>{{"Primed 556 Casing",1000},}, 
                new Dictionary < string, int > { { "KillInRaid", 1 } }),

            /*  +  */new Quest(11, "PVP&PVE", "Сдать 5000 скрапа", 5000,
            new Dictionary<string, int>{{"Rank: BestPlayer",1},},
            new Dictionary < string, int > { { "PassScrap", 5000 } })
        };
        public static Dictionary<string, string> Translates = new Dictionary<string, string>
        {
            { "Убить 15 мутантов", "Kill 15 mutants"},
            { "Добыть 500 дерева", "Get 500 wood"},
            { "Убить оленя и получить рога", "Kill deer and get Horns"},
            { "Нафармить по 150 руды каждого вида", "Farm 150 ores of each type"},
            { "Залутать 30 любых ящиков", "Loot any 30 crates"},
            { "Убить 15 кроликов", "Kill 15 rabbits"},
            { "Добыть 1000 дерева и 400 руды", "Get 1000 wood and 400 ore"},
            { "Залутать Аир Дроп", "Loot Air Drop"},
            { "Скрафтить 10 гранат и 2 с4", "Craft 10 F1 Grenades and 2 C4"},
            { "Взорвать одну стену", "Blow up one wall"},

            { "Убить 5 волков и 5 медведей", "Kill 5 wolves and 5 bears"},
            { "Скрафтить 2 револьвера", "Craft 2 revolvers"},
            { "Убить 5 игроков с Pipe Shotgun", "Kill 5 players with the Pipe Shotgun"},
            { "Залутать 5 военных ящиков", "Loot 5 Military Crates"},
            { "Убить 30 мутантов", "Kill 30 mutants"},
            { "Убить 20 игроков", "Kill 20 players"},
            { "Убить КАЖДОГО вида животных(в том числе и мутантов)", "Kill EVERY type of animal (including mutants)"},
            { "Скрафтить 2 M4 2 P250 2 Shotgun", "Craft 2 M4, 2 P250, 2 Shotgun"},
            { "Залутать Аир-Дроп", "Loot Air Drop"},
            { "Убить человека в зоне рейдблока", "Kill a person in the raid block zone"},

            { "Сдать 5000 скрапа", "Turn in 5000 Scrap" }
        };
        public class QuestUser
        {
            public QuestUser(string playerName, ulong userId, Dictionary<string, int> completedActions, Dictionary<string, int> completedArguments,Dictionary<string, List<int>> completedQuests, Dictionary<string, List<int>> earnedQuests)
            {
                PlayerName = playerName;
                UserID = userId;
                CompletedActions = completedActions;
                CompletedArguments = completedArguments;
                CompletedQuests = completedQuests;
                EarnedQuests = earnedQuests;
            }
            public string PlayerName { get; set; }
            public ulong UserID { get; set; }
            public Dictionary<string, int> CompletedActions { get; set; }
            public Dictionary<string, int> CompletedArguments { get; set; }
            public Dictionary<string, List<int>> CompletedQuests { get; set; }
            public Dictionary<string, List<int>> EarnedQuests { get; set; }
        }
        public static List<QuestUser> QuestUsers;

        public static QuestUser GetQuestUser(ulong userID)
        {
            var user = QuestUsers.Find(f => f.UserID == userID);
            return user;
        }
        private static Quest GetQuest(int id, string branch) => 
            Quests.Find(f => f.ID == id && f.Branch == branch);

        public void OnQuestCompleted(NetUser user, Quest quest)
        {
            foreach (var reward in quest.Rewards)
            {
                switch (reward.Key)
                {
                    case "Money":
                        Economy.Database[user.userID].Balance += (ulong)reward.Value;
                        continue;
                    case "Rank: BestPlayer":
                        Users.GetBySteamID(user.userID).Rank = reward.Value;
                        continue;
                    default:
                        Helper.GiveItem(user.playerClient, reward.Key, reward.Value);
                        break;
                }
            }
            GetQuestUser(user.userID).EarnedQuests[quest.Branch].Add(quest.ID);
            rust.Notice(user, $"Вы успешно получили награду за квест \"{quest.Branch} №{quest.ID}\"!");
        }

        private void LoadPluginToPlayer(PlayerClient pClient)
        {
            if (pClient.gameObject.GetComponent<QuestVm>() != null) return;
            if (_loadedPlayers.Contains(pClient.userID)) _loadedPlayers.Remove(pClient.userID);

            var vm = pClient.gameObject.AddComponent<QuestVm>();
            vm.PlayerClient = pClient;
            vm.BQuests = this;

            _loadedPlayers.Add(pClient.userID);
        }
        private static void UnloadPluginFromPlayer(GameObject gameObject, Type plugin)
        {
            if (gameObject.GetComponent(plugin) != null) UnityEngine.Object.Destroy(gameObject.GetComponent(plugin));
        }
            
        private void Loaded()
        {
            QuestUsers = Interface.Oxide.DataFileSystem.ReadObject<List<QuestUser>>("BQuestsUserData");
            foreach (var pc in PlayerClient.All.Where(pc => pc != null))
            {
                LoadPluginToPlayer(pc);
                if (GetQuestUser(pc.userID) == null)
                {
                    QuestUsers.Add(new QuestUser(Helper.NiceName(pc.userName), pc.userID, new Dictionary<string, int>(),
                        new Dictionary<string, int>(),
                        new Dictionary<string, List<int>> { { "PVP", new List<int>() }, { "PVE", new List<int>() }, { "PVP&PVE", new List<int>() } },
                        new Dictionary<string, List<int>> { { "PVP", new List<int>() }, { "PVE", new List<int>() }, { "PVP&PVE", new List<int>() } }));
                    foreach (var quest in Quests)
                    {
                        foreach (var argument in quest.Arguments)
                        {
                            GetQuestUser(pc.userID).CompletedArguments.Add($"{quest.ID} - {argument.Key}", 0);
                            GetQuestUser(pc.userID).CompletedActions.Add(quest.Description, 0);
                        }
                    }
                }
            }

            foreach (var questUser in QuestUsers)
            {
                if (!questUser.CompletedQuests.ContainsKey("PVP&PVE")) questUser.CompletedQuests.Add("PVP&PVE", new List<int>());
                if (!questUser.EarnedQuests.ContainsKey("PVP&PVE")) questUser.EarnedQuests.Add("PVP&PVE", new List<int>());

                if (questUser.CompletedArguments == null) questUser.CompletedArguments = new Dictionary<string, int>();
                foreach (var quest in Quests)
                {
                    foreach (var argument in quest.Arguments.Where(argument => !questUser.CompletedArguments.ContainsKey($"{quest.ID} - {argument.Key}")))
                        questUser.CompletedArguments.Add($"{quest.ID} - {argument.Key}", 0);

                    if (!questUser.CompletedActions.ContainsKey(quest.Description))
                        questUser.CompletedActions.Add(quest.Description, 0);
                }
            }
        }
        private void Unload()
        {
            SaveData();
            foreach (var loadedPlayer in _loadedPlayers)
            {
                PlayerClient playerClient;
                PlayerClient.FindByUserID(loadedPlayer, out playerClient);
                if (playerClient != null) UnloadPluginFromPlayer(playerClient.gameObject, typeof(QuestVm));
            }
        }
        private void OnServerSave() =>
            SaveData();

        private static void SaveData() =>
            Interface.Oxide.DataFileSystem.WriteObject("BQuestsUserData", QuestUsers);

        private void OnPlayerConnected(NetUser user)
        {
            if (GetQuestUser(user.userID) == null)
            {
                QuestUsers.Add(new QuestUser(Helper.NiceName(user.displayName), user.userID, new Dictionary<string, int>(), new Dictionary<string, int>(), new Dictionary<string, List<int>> { { "PVP", new List<int>() }, { "PVE", new List<int>() }, { "PVP&PVE", new List<int>() } }, new Dictionary<string, List<int>> { { "PVP", new List<int>() }, { "PVE", new List<int>() }, { "PVP&PVE", new List<int>() } }));
                foreach (var quest in Quests)
                {
                    foreach (var argument in quest.Arguments) GetQuestUser(user.userID).CompletedArguments.Add($"{quest.ID} - {argument.Key}", 0);
                    GetQuestUser(user.userID).CompletedActions.Add(quest.Description, 0);
                }
            }
            if (user.playerClient != null) LoadPluginToPlayer(user.playerClient);
        }
        private void OnPlayerDisconnected(uLink.NetworkPlayer networkPlayer)
        {
            var user = NetUser.Find(networkPlayer);
            if (user != null) _loadedPlayers.Remove(user.userID);
        }

        private void OnDropHorns(NetUser netUser)
        {
            var user = GetQuestUser(netUser.userID);
            if (!user.CompletedQuests["PVE"].Contains(3) && user.CompletedArguments["3 - Horns"] < GetQuest(3, "PVE").Arguments["Horns"])
            {
                CompleteAction(netUser, GetQuest(3, "PVE"));
                user.CompletedArguments["3 - Horns"]++;
            }
        }
        private void OnKilled(TakeDamage damage, DamageEvent evt)
        {
            try
            {
                if (evt.victim.idMain != null && evt.attacker.idMain != null)
                {
                    if (evt.amount < damage.health) return;

                    var netUser = evt.attacker.client.netUser;
                    if (netUser == null) return;

                    var user = GetQuestUser(netUser.userID);

                    if (damage is HumanBodyTakeDamage)
                    {
                        var victimPlayer = evt.victim.client.netUser;
                        if (victimPlayer == null || netUser == victimPlayer) return;

                        if (!user.CompletedQuests["PVP"].Contains(6)) CompleteAction(netUser, GetQuest(6, "PVP"));

                        var weapon = evt.extraData as WeaponImpact;
                        if (weapon != null)
                        {
                            var weaponName = weapon.dataBlock.name;
                            if (weaponName.ToLower().Contains("pipe") && !user.CompletedQuests["PVP"].Contains(3)) CompleteAction(netUser, GetQuest(3, "PVP"));
                        }
                    }

                    var victim = evt.victim.character;
                    if (victim != null)
                    {
                        var vName = Helper.NiceName(victim.name);
                        if (vName.ToLower().Contains("mutant"))
                        {
                            if (!user.CompletedQuests["PVE"].Contains(1)) CompleteAction(netUser, GetQuest(1, "PVE"));
                            if (!user.CompletedQuests["PVP"].Contains(5)) CompleteAction(netUser, GetQuest(5, "PVP"));
                        }
                        if (!user.CompletedQuests["PVP"].Contains(7) && user.CompletedArguments["7 - " + vName] < GetQuest(7, "PVP").Arguments[vName])
                        {
                            CompleteAction(netUser, GetQuest(7, "PVP"));
                            user.CompletedArguments["7 - " + vName]++;
                        }
                        switch (vName)
                        {
                            case "Wolf":
                                {
                                    if (!user.CompletedQuests["PVP"].Contains(1) && user.CompletedArguments["1 - Wolf"] < GetQuest(1, "PVP").Arguments["Wolf"])
                                    {
                                        CompleteAction(netUser, GetQuest(1, "PVP"));
                                        user.CompletedArguments["1 - Wolf"]++;
                                    }
                                    break;
                                }
                            case "Bear":
                                {
                                    if (!user.CompletedQuests["PVP"].Contains(1) && user.CompletedArguments["1 - Bear"] < GetQuest(1, "PVP").Arguments["Bear"])
                                    {
                                        CompleteAction(netUser, GetQuest(1, "PVP"));
                                        user.CompletedArguments["1 - Bear"]++;
                                    }
                                    break;
                                }
                            case "Stag":
                                {
                                    if (!user.CompletedQuests["PVE"].Contains(3) && user.CompletedArguments["3 - Stag"] < GetQuest(3, "PVE").Arguments["Stag"])
                                    {
                                        CompleteAction(netUser, GetQuest(3, "PVE"));
                                        user.CompletedArguments["3 - Stag"]++;
                                    }
                                    break;
                                }
                            case "Rabbit":
                                {
                                    if (!user.CompletedQuests["PVE"].Contains(6)) CompleteAction(netUser, GetQuest(6, "PVE"));
                                    break;
                                }
                        }
                    }

                    if (evt.victim.idMain is StructureComponent && evt.damageTypes == DamageTypeFlags.damage_explosion && !user.CompletedQuests["PVE"].Contains(10)) CompleteAction(netUser, GetQuest(10, "PVE"));
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
                // ignored
            }
        }
        private void OnKillInBlock(NetUser netUser)
        {
            var user = GetQuestUser(netUser.userID);
            if (!user.CompletedQuests["PVP"].Contains(10)) CompleteAction(netUser, GetQuest(10, "PVP"));
        }
        private void OnItemCraft(CraftingInventory inventory, BlueprintDataBlock blueprint, int amount, ulong startTime)
        {
            var netUser = NetUser.Find(inventory.networkView.owner);
            var playerInv = inventory as PlayerInventory;
            if (playerInv == null || netUser == null) return;

            var user = GetQuestUser(netUser.userID);
            var resultName = blueprint.resultItem.name;
            if (resultName.ToLower().Contains("armor part")) Helper.InventoryItemRemove(playerInv, DatablockDictionary.GetByName(resultName), amount);
	    if (resultName.ToLower().Contains("camp fire")) Helper.InventoryItemRemove(playerInv, DatablockDictionary.GetByName(resultName), amount);
            if (resultName.ToLower().Contains("revolver") && !user.CompletedQuests["PVP"].Contains(2)) CompleteAction(netUser, GetQuest(2, "PVP"), amount);
            
            if (!user.CompletedQuests["PVP"].Contains(8))
            {
                if (blueprint.resultItem.name.ToLower().Contains("m4") && user.CompletedArguments["8 - M4"] < GetQuest(8, "PVP").Arguments["M4"])
                {
                    for (var i = 0; i < amount; i++)
                    {
                        user.CompletedArguments["8 - M4"]++;
                        CompleteAction(netUser, GetQuest(8, "PVP"));
                    }
                }
                if (blueprint.resultItem.name.ToLower().Contains("p250") && user.CompletedArguments["8 - P250"] < GetQuest(8, "PVP").Arguments["P250"])
                {
                    for (var i = 0; i < amount; i++)
                    {
                        user.CompletedArguments["8 - P250"]++;
                        CompleteAction(netUser, GetQuest(8, "PVP"));
                    }
                }
                if (blueprint.resultItem.name == "Shotgun" && user.CompletedArguments["8 - Shotgun"] < GetQuest(8, "PVP").Arguments["Shotgun"])
                {
                    for (var i = 0; i < amount; i++)
                    {
                        user.CompletedArguments["8 - Shotgun"]++;
                        CompleteAction(netUser, GetQuest(8, "PVP"));
                    }
                }
            }

            if (!user.CompletedQuests["PVE"].Contains(9))
            {
                if (resultName.ToLower().Contains("f1"))
                {
                    for (var i = 0; i < amount; i++)
                        if (user.CompletedArguments["9 - F1 Grenade"] < GetQuest(9, "PVE").Arguments["F1 Grenade"])
                        {
                            user.CompletedArguments["9 - F1 Grenade"]++;
                            CompleteAction(netUser, GetQuest(9, "PVE"));
                        }
                }

                if (resultName.ToLower().Contains("explosive charge"))
                {
                    for (var i = 0; i < amount; i++)
                        if (user.CompletedArguments["9 - C4"] < GetQuest(9, "PVE").Arguments["C4"])
                        {
                            user.CompletedArguments["9 - C4"]++;
                            CompleteAction(netUser, GetQuest(9, "PVE"));
                        }
                }
            }
        }

        private Dictionary<int, Vector3> boxList = new Dictionary<int, Vector3>();
        private void OnItemRemoved(Inventory fromInv, int slot, IInventoryItem item)
        {
            if (fromInv == null) return;

            var lootable = fromInv.GetComponent<LootableObject>();
            if (lootable == null || !lootable.destroyOnEmpty || lootable.NumberOfSlots != 12) return;

            var boxID = lootable.networkViewID.id;
            if (boxID == 0) return;

            if (boxList.ContainsKey(boxID) && boxList[boxID] != lootable.transform.position) boxList.Remove(boxID);
            if (!boxList.ContainsKey(boxID))
            {
                foreach (var player in PlayerClient.All.Where(player => player != null && fromInv.IsAnAuthorizedLooter(player.netPlayer)))
                {
                    var lootName = lootable.name.Replace("(Clone)", "");

                    var user = GetQuestUser(player.userID);
                    switch (lootName)
                    {
                        case "SupplyCrate":
                        {
                            boxList.Add(boxID, lootable.transform.position);
                            if (!user.CompletedQuests["PVE"].Contains(8)) CompleteAction(player.netUser, GetQuest(8, "PVE"));
                            if (!user.CompletedQuests["PVP"].Contains(9)) CompleteAction(player.netUser, GetQuest(9, "PVP"));
                            if (!user.CompletedQuests["PVE"].Contains(5)) CompleteAction(player.netUser, GetQuest(5, "PVE"));
                            break;
                        }
                        case "WeaponLootBox":
                        {
                            boxList.Add(boxID, lootable.transform.position);
                            if (!user.CompletedQuests["PVP"].Contains(4)) CompleteAction(player.netUser, GetQuest(4, "PVP"));
                            if (!user.CompletedQuests["PVE"].Contains(5)) CompleteAction(player.netUser, GetQuest(5, "PVE"));
                            break;
                        }
                        case "BoxLoot":
                        case "AmmoLootBox":
                        case "MedicalLootBox":
                        {
                            if (!user.CompletedQuests["PVE"].Contains(5))
                            {
                                boxList.Add(boxID, lootable.transform.position);
                                CompleteAction(player.netUser, GetQuest(5, "PVE"));
                            }

                            break;
                        }
                    }

                    break;
                }
            }
        }

        private void OnGather(Inventory reciever, ResourceTarget obj, ResourceGivePair item, int collected)
        {
            try
            {
                if (item == null || reciever == null || obj == null || collected < 1) return;
                var netUser = NetUser.Find(reciever.networkView.owner);
                if (netUser == null) return;

                var user = GetQuestUser(netUser.userID);
                var clan = Clans.Find(netUser.userID);
            
                switch (item.ResourceItemName)
                {
                    case "Wood":
                    {
                        if (!user.CompletedQuests["PVE"].Contains(2))
                        {
                            CompleteAction(netUser, GetQuest(2, "PVE"), clan != null ? (int)(collected * clan.Level.BonusGatheringWood / 100) : 0 + collected);
                        }
                        if (!user.CompletedQuests["PVE"].Contains(7) && user.CompletedArguments["7 - Wood"] < GetQuest(7, "PVE").Arguments["Wood"])
                        {
                            user.CompletedArguments["7 - Wood"] += clan != null
                                ? (int)(collected * clan.Level.BonusGatheringWood / 100)
                                : 0 + collected;
                            CompleteAction(netUser, GetQuest(7, "PVE"), clan != null ? (int)(collected * clan.Level.BonusGatheringWood / 100) : 0 + collected);
                        }

                        break;
                    }
                    case "Metal Ore":
                    {
                        if (!user.CompletedQuests["PVE"].Contains(4) && user.CompletedArguments["4 - Metal"] < GetQuest(4, "PVE").Arguments["Metal"])
                        {
                            user.CompletedArguments["4 - Metal"] += clan != null
                                ? (int)(collected * clan.Level.BonusGatheringWood / 100)
                                : 0 + collected;
                            CompleteAction(netUser, GetQuest(4, "PVE"),
                                clan != null ? (int)(collected * clan.Level.BonusGatheringWood / 100) : 0 + collected);
                        }
                        if (!user.CompletedQuests["PVE"].Contains(7) && user.CompletedArguments["7 - Ores"] < GetQuest(7, "PVE").Arguments["Ores"])
                        {
                            user.CompletedArguments["7 - Ores"] += clan != null
                                ? (int)(collected * clan.Level.BonusGatheringWood / 100)
                                : 0 + collected;
                            CompleteAction(netUser, GetQuest(7, "PVE"), clan != null ? (int)(collected * clan.Level.BonusGatheringWood / 100) : 0 + collected);
                        }

                        break;
                    }
                    case "Sulfur Ore":
                    {
                        if (!user.CompletedQuests["PVE"].Contains(4) && user.CompletedArguments["4 - Sulfur"] < GetQuest(4, "PVE").Arguments["Sulfur"])
                        {
                            user.CompletedArguments["4 - Sulfur"] += clan != null
                                ? (int)(collected * clan.Level.BonusGatheringWood / 100)
                                : 0 + collected;
                            CompleteAction(netUser, GetQuest(4, "PVE"), clan != null ? (int)(collected * clan.Level.BonusGatheringWood / 100) : 0 + collected);
                        }
                        if (!user.CompletedQuests["PVE"].Contains(7) &&user.CompletedArguments["7 - Ores"] < GetQuest(7, "PVE").Arguments["Ores"])
                        {
                            user.CompletedArguments["7 - Ores"] += clan != null
                                ? (int)(collected * clan.Level.BonusGatheringWood / 100)
                                : 0 + collected;
                            CompleteAction(netUser, GetQuest(7, "PVE"), clan != null ? (int)(collected * clan.Level.BonusGatheringWood / 100) : 0 + collected);
                        }
                        break;
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        private void CompleteAction(NetUser netUser, Quest quest, int count = 1)
        {
            var user = GetQuestUser(netUser.userID);
            user.CompletedActions[quest.Description] += count;
            
            if (user.CompletedActions[quest.Description] < quest.NeededActions) return;
            
            user.CompletedQuests[quest.Branch].Add(quest.ID);
            rust.Notice(netUser, $"Вы успешно выполнили квест \"{quest.Branch} №{quest.ID}\"!");

            SaveData();

            for (int i = 1; i <= 10; i++)
            {
                if (user.CompletedQuests["PVE"].Contains(i)) continue;
                return;
            }
            for (int i = 1; i <= 10; i++)
            {
                if (user.CompletedQuests["PVP"].Contains(i)) continue;
                return;
            }

            var message = $"[QuestInfo]\r\n nИгрок [{Users.GetUsername(netUser.userID)}:{netUser.userID}:{Users.GetBySteamID(netUser.userID).HWID}] открыл квест №11.";
            if (quest.ID == 11) message = $"[QuestInfo]\r\n Игрок [{Users.GetUsername(netUser.userID)}:{netUser.userID}:{Users.GetBySteamID(netUser.userID).HWID}] выполнил все квесты!";

            webrequest.EnqueueGet($"https://api.vk.com/method/messages.send?message={message}&group_id=213390559&random_id={UnityEngine.Random.Range(0, 999999)}&peer_id=2000000002&access_token=89b649080717f943e20c54833c819e11794f219ed6c940cf42b1104e3dcfa8da39ad500cdd4a1cddb12ea&v=5.131", (a, b) => { }, this);
        }

        [ChatCommand("addargs")]
        private void CMD_WipeQuests(NetUser user, string cmd, string[] args)
        {
            if (!user.admin) return;
            foreach (var questUser in QuestUsers)
            {
                if (questUser.CompletedArguments == null) questUser.CompletedArguments = new Dictionary<string, int>();
                foreach (var quest in Quests)
                {
                    foreach (var argument in quest.Arguments.Where(argument => !questUser.CompletedArguments.ContainsKey($"{quest.ID} - {argument.Key}")))
                        questUser.CompletedArguments.Add($"{quest.ID} - {argument.Key}", 0);

                    if (!questUser.CompletedActions.ContainsKey(quest.Description))
                        questUser.CompletedActions.Add(quest.Description, 0);
                }
            }
            SaveData();
        }
        [ChatCommand("request")]
        private void CMD_ReQuest(NetUser user, string cmd, string[] args)
        {
            if (!user.admin) return;
            var uData = Users.Find(args[0]);

            var questUser = GetQuestUser(uData.SteamID);

            var branch = args[1];
            var id = int.Parse(args[2]);

            questUser.CompletedQuests[branch].Add(id);
            rust.Notice(NetUser.FindByUserID(uData.SteamID), $"Вы успешно выполнили квест \"{branch} №{id}\"!");

            SaveData();
        }
        [ChatCommand("easyup")]
        private void CMD_EasyUp(NetUser user, string cmd, string[] args)
        {
            if (!user.admin) return;
            var questUser = GetQuestUser(user.userID);

            foreach (var quest in Quests.Where(quest => quest.ID != 11))
                questUser.CompletedQuests[quest.Branch].Add(quest.ID);

            SaveData();
        }   
        [ChatCommand("clrstate")]
        private void CMD_ClearStats(NetUser user, string cmd, string[] args)
        {
            if (!user.admin) return;
            var uData = Users.Find(args[0]);

            GetQuestUser(uData.SteamID).EarnedQuests = new Dictionary<string, List<int>> { { "PVP", new List<int>() }, { "PVE", new List<int>() }, { "PVP&PVE", new List<int>() } };
            GetQuestUser(uData.SteamID).CompletedArguments = new Dictionary<string, int>();
            GetQuestUser(uData.SteamID).CompletedQuests = new Dictionary<string, List<int>> { { "PVP", new List<int>() }, { "PVE", new List<int>() }, { "PVP&PVE", new List<int>() } };
            GetQuestUser(uData.SteamID).CompletedActions = new Dictionary<string, int>();

            foreach (var quest in Quests)
            {
                foreach (var argument in quest.Arguments) GetQuestUser(uData.SteamID).CompletedArguments.Add($"{quest.ID} - {argument.Key}", 0);
                GetQuestUser(uData.SteamID).CompletedActions.Add(quest.Description, 0);
            }
                
            rust.Notice(user, $"Вы обнулили \"{uData.Username}\"!");
            rust.Notice(NetUser.FindByUserID(uData.SteamID), $"Вас обнулил \"{user.displayName}\"!");
        }

        private class QuestVm : MonoBehaviour
        {
            public PlayerClient PlayerClient;
            public BQuests BQuests;

            private bool isNiceUse;

            [RPC]
            public void PassScrap()
            {
                var inv = PlayerClient.rootControllable.idMain.GetComponent<Inventory>();
                if (inv == null) return;

                var scrapCount = Helper.InventoryItemCount(inv, DatablockDictionary.GetByName("Primed 556 Casing"));
                if (scrapCount == 0) return;

                Helper.InventoryItemRemove(inv, DatablockDictionary.GetByName("Primed 556 Casing"), scrapCount);
                BQuests.CompleteAction(PlayerClient.netUser, GetQuest(11, "PVP&PVE"), scrapCount);

                Broadcast.Message(Helper.GetPlayerClient(PlayerClient.userID).netUser, $"Вы успешно сдали \"SCRAP\" в количестве {scrapCount}!", "Квесты");
            }

            [RPC]
            public void OnItemAction(string itemName, string option, int slot)
            {
                if (isNiceUse) return;
                if (itemName == "TEA") BQuests.BTeaBoosts?.CallHook("OnTeaUse", PlayerClient.netUser);
                else
                {
                    isNiceUse = true;
                    StartCoroutine(Coroutine());
                }
            }
            private IEnumerator Coroutine()
            {
                yield return new WaitForSeconds(2.25f);
                isNiceUse = false;
            }

            [RPC]
            public void GetQuests()
            {
                foreach (var args in from quest in Quests
                         let id = quest.ID
                         let branch = quest.Branch
                         let rewards =
                             quest.Rewards.Aggregate("",
                                 (current, reward) => current + $"{reward.Key} - {reward.Value}шт\n")
                         let description = Users.GetBySteamID(PlayerClient.userID).Language == "ENG" ? Translates[quest.Description] : quest.Description
                         let completed = GetQuestUser(PlayerClient.userID).CompletedQuests[branch].Contains(id)
                         let rewarded = GetQuestUser(PlayerClient.userID).EarnedQuests[branch].Contains(id)
                         let nAct = quest.NeededActions
                         let act = GetQuestUser(PlayerClient.userID).CompletedActions[quest.Description]
                                     select new object[] { id, branch, rewards.Replace("Primed 556 Casing", "Scrap").Replace("Small Water Bottle", "TEA"), description, completed, rewarded, nAct, act }) SendRPC("AddQuest", args);
            }

            [RPC]   
            public void GetReward(int questID, string branch)
            {
                var quest = GetQuest(questID, branch);
                if (!GetQuestUser(PlayerClient.userID).EarnedQuests[branch].Contains(questID)) BQuests.OnQuestCompleted(PlayerClient.netUser, quest);
            }

            private void SendRPC(string rpcName, params object[] args) =>
                GetComponent<Facepunch.NetworkView>().RPC(rpcName, PlayerClient.netPlayer, args);
        }
    }
}