using Oxide.Core;
using UnityEngine;
using RustExtended;

using System;
using System.Linq;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("BRPGPlayerSystem", "systemXcrackedZ", "4.3.2")]
    [Description("RPG Player System for Rust Legacy server: Bless Rust.")]
    internal class BQuests : RustLegacyPlugin
    {
        [Flags]
        private enum ActionType
        {
            KillPlayer = 0,

            KillWolf = 1,
            KillBear = 2,

            KillMutantWolf = 3,
            KillMutantBear = 4,

            KillBoar = 5,
            KillRabbit = 6,
            KillChicken = 7,

            F1Explode = 8,
            C4Explode = 9,

            Craft = 10,

            LootWeapon = 11,
            LootSupply = 12,
            LootBox = 13,
            LootAmmo = 14,
            LootMedical = 15,

            GatherWood = 16,
            GatherStone = 17,
            GatherMetal = 18,
            GatherSulfur = 19,

            WalkDistance = 20
        }

        private Dictionary<string, string> messageColors = new Dictionary<string, string>
        {
            {"Main", "[COLOR # FFFFFF]" },
            //
            {"Money", "[COLOR # 00FF00]" },
            {"XP", "[COLOR # FFFF00]" },
            //
            {"Reward", "[COLOR # 05E0FF]" },
            {"Now", "[COLOR # 00BFFF]" },
            //
            {"Cancelled", "[COLOR # FE2E2E]" },
            //
            {"LimeGreen", "[COLOR # C8FE2E]" }
        };
        private Dictionary<int, int> XPToLevel = new Dictionary<int, int>
        {   //КВЕСТ, КОЛИЧЕСТВО
            //1-5 LVL (+2 EXP/LEVEL)
            {1, 2 },        {2, 4 },        {3, 6 },        {4, 8 },        {5, 10 },
            
            //6-10 LVL (+3 EXP/LEVEL)
            {6, 13 },       {7, 16 },       {8, 19 },       {9, 22 },       {10, 25 },
            
            //11-15 LVL (+4 EXP/LEVEL)
            {11, 29 },      {12, 33 },      {13, 37 },      {14, 41 },      {15, 45 },
            
            //16-20 LVL (+5 EXP/LEVEL)
            {16, 50 },      {17, 55 },      {18, 60 },      {19, 65 },      {20, 70 },
            
            //21-25 LVL (+6 EXP/LEVEL)
            {21, 76 },      {22, 82 },      {23, 88 },      {24, 94 },      {25, 100 },
            
            //26-30 LVL (+7 EXP/LEVEL)
            {26, 107 },     {27, 114 },     {28, 120 },     {29, 126 },     {30, 132 },
            
            //31-35 LVL (+8 EXP/LEVEL)
            {31, 140 },     {32, 148 },     {33, 156 },     {34, 164 },     {35, 172 }
        };
        private Dictionary<int, int> LevelSkillPoints = new Dictionary<int, int>
        {

        };

        private static Dictionary<int, Quest> quests = new Dictionary<int, Quest>();
        private static Dictionary<ulong, QuestUser> users = new Dictionary<ulong, QuestUser>();

        private class Quest
        {
            public Quest(int minRank, int minLevel, string taskText, int interval, ActionType ActionType, float countActions, string result, ulong rewardXP, ulong rewardMoney)
            {
                MinRank = minRank; MinLevel = minLevel;
                TaskText = taskText;
                Interval = interval;
                ActionType = ActionType; CountActions = countActions;
                Result = result;
                RewardXP = rewardXP; RewardMoney = rewardMoney;
            }
            
            [JsonProperty("Минимальный Ранг")] public int MinRank { get; set; }             //Минимальный ранг для выполнения квеста = 0; Если > 0, добавляй в конец
            [JsonProperty("Минимальный Уровень")] public int MinLevel { get; set; }            //Минимальный уровень для выполнения квеста = 0; Если > 0, добавляй в конец

            [JsonProperty("Текст задания")] public string TaskText { get; set; }         //Текст, который выводится игроку
            [JsonProperty("Интервал выполнения")] public int Interval { get; set; }            //Интервал в секундах, через который можно снова выполнить квест

            [JsonProperty("Тип действия")] public ActionType ActionType { get; set; }          //Тип действия, которое нужно выполнить = 1;
            [JsonProperty("Количество действий")] public float CountActions { get; set; }      //Количество для ActionType = 1;
            [JsonProperty("Результат")] public string Result { get; set; }         //Примечание (Предмет, который скрафтить)

            [JsonProperty("Награда (XP)")] public ulong RewardXP { get; set; }          //Награда XP = 0;
            [JsonProperty("Награда ($)")] public ulong RewardMoney { get; set; }       //Награда $ = 0;
        }
        private class QuestUser
        {
            public QuestUser(int quest, int playerXP, int playerLevel, Dictionary<int, string> completed, Dictionary<int, float> actionsCompleted)
            {
                Quest = quest;
                PlayerXP = playerXP; PlayerLevel = playerLevel;
                Completed = completed; ActionsCompleted = actionsCompleted;
            }
            
            [JsonProperty("Номер задания")] public int Quest { get; set; }

            [JsonProperty("Опыт игрока")] public int PlayerXP { get; set; }
            [JsonProperty("Уровень игрока")] public int PlayerLevel { get; set; }

            [JsonProperty("Выполненные задания")] public Dictionary<int, string> Completed { get; set; }
            [JsonProperty("Прогресс выполнения")] public Dictionary<int, float> ActionsCompleted { get; set; }
        }
        private QuestUser GetUserData(ulong userID)
        {
            QuestUser data;
            if (!users.TryGetValue(userID, out data))
            {
                data = new QuestUser(-1, 0, 1, new Dictionary<int, string>(), new Dictionary<int, float>());
                users.Add(userID, data);
            }
            return data;
        }

        private object OnPlayerAction(PlayerClient playerClient, QuestUser user, ActionType ActionType)
        {
            int questID = user.Quest;
            if (questID == -1) return null;

            IncreaseCompletedActions(user, questID);

            Quest quest = quests[questID];
            if (quest.CountActions == 1)
            {
                CompleteQuest(playerClient.netUser, questID);
                return null;
            }

            if (quest.CountActions > 1)
            {
                if (user.ActionsCompleted.ContainsKey(questID))
                {
                    if (user.ActionsCompleted[questID] >= quest.CountActions)
                    {
                        CompleteQuest(playerClient.netUser, questID);
                    }
                }
            }

            return null;
        }

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
                            if (user.Quest == -1) return;

                            Quest quest = quests[user.Quest];
                            if (quest.ActionType == ActionType.LootSupply && lootableName == "SupplyCrate")
                            {
                                OnPlayerAction(playerClient, user, ActionType.LootSupply);
                                lootableList.Add(lootableID, lootableObject.transform.position);
                            }
                            if (quest.ActionType == ActionType.LootWeapon && lootableName == "WeaponLootBox")
                            {
                                OnPlayerAction(playerClient, user, ActionType.LootWeapon);
                                lootableList.Add(lootableID, lootableObject.transform.position);
                            }
                            if (quest.ActionType == ActionType.LootBox && lootableName == "BoxLoot")
                            {
                                OnPlayerAction(playerClient, user, ActionType.LootBox);
                                lootableList.Add(lootableID, lootableObject.transform.position);
                            }
                            if (quest.ActionType == ActionType.LootAmmo && lootableName == "AmmoLootBox")
                            {
                                OnPlayerAction(playerClient, user, ActionType.LootAmmo);
                                lootableList.Add(lootableID, lootableObject.transform.position);
                            }
                            if (quest.ActionType == ActionType.LootMedical && lootableName == "MedicalLootBox")
                            {
                                OnPlayerAction(playerClient, user, ActionType.LootMedical);
                                lootableList.Add(lootableID, lootableObject.transform.position);
                            }
                        }
                    }
                }
            }
        }
        #endregion
        #region [HOOK] [OnGather] (ActionType 16 - 19)
        #endregion

        private void CompleteQuest(NetUser user, int questID)
        {
            QuestUser QuestUser = GetUserData(user.userID);
            if (QuestUser.Quest == questID)
            {
                QuestUser.Quest = -1;
                Quest quest = quests[questID];

                Economy.Get(user.userID).Balance += quest.RewardMoney;
                QuestUser.PlayerXP += (int)quest.RewardXP;

                if (QuestUser.Completed.ContainsKey(questID)) QuestUser.Completed[questID] = DateTime.Now.ToString();
                else QuestUser.Completed.Add(questID, DateTime.Now.ToString());
            }
        }
        private void ClearCompletedActions(QuestUser user, int questID)
        {
            if (user.ActionsCompleted.ContainsKey(questID)) user.ActionsCompleted.Remove(questID);
            SavePluginData();
        }
        private void IncreaseCompletedActions(QuestUser user, int questID, float count = 1)
        {
            if (user.ActionsCompleted.ContainsKey(questID))
                user.ActionsCompleted[questID] += count;
            else
                user.ActionsCompleted.Add(questID, count);

            SavePluginData();
        }

        private void Loaded()
        {
            quests = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<int, Quest>>("BQuestsConfig");
            users = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, QuestUser>>("BQuestsUserData");
        }
        private void SavePluginData() => Interface.GetMod().DataFileSystem.WriteObject("SQuestsUserData", users);

        private void SendMessage(NetUser user, string message, int index = 0) => rust.SendChatMessage(user, "Квесты", $"{messageColors.ElementAt(index)}{message}");
        private void LogCMD(NetUser user, string cmd, string[] args)
        {
            string text = $"Command [{user.displayName}:{user.userID}] /" + cmd;
            foreach (string arg in args) text += " " + arg;
            Helper.LogChat(text, true);
        }
        private string SecondsToTime(int seconds)
        {
            string time = string.Empty;
            TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);

            if (timeSpan.Days > 0) time += $"{timeSpan.Days} дней ";
            if (timeSpan.Hours > 0) time += $"{timeSpan.Hours} часов ";
            if (timeSpan.Minutes > 0) time += $"{timeSpan.Minutes} минут ";
            if (timeSpan.Seconds > 0) time += $"{timeSpan.Seconds} секунд";

            if (time == string.Empty) time = "0 секунд";
            return time;
        }
    }
}
