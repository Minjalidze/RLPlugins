//---------------------------------//
using System;
using System.Collections;
using System.Collections.Generic;
//---------------------------------//
using Oxide.Core;
using UnityEngine;
using RustExtended;
//---------------------------------//

namespace Oxide.Plugins
{
    internal class BEventMaster : RustLegacyPlugin
    {
        private class EventData
        {
            public Dictionary<string, object> Settings = new Dictionary<string, object>
            {   //НАЗВАНИЕ НАСТРОЙКИ    //ЗНАЧЕНИЕ НАСТРОЙКИ
                { "EventName",          "Турнир Одиночек" },
                { "MaxPlayers",         20 },
                { "GiveItems",          true },
                { "IsDuel",             true },
                { "DuelPlayers",        2 },
                { "IsStages",           true }
            };
            public Dictionary<string, Dictionary<string, int>> EventItems = new Dictionary<string, Dictionary<string, int>>
            {
                { "Armor", new Dictionary<string, int>() },
                { "Inventory", new Dictionary<string, int>() },
                { "FastSlots", new Dictionary<string, int>() }
            };

            public List<string> SpawnPoints { get; set; }
            public List<string> GamePoints { get; set; }
        }
        private EventData currentEvent;
        private class EventPreset
        {
            public string PresetName { get; set; }
            public EventData Event { get; set; }
        }
        private List<EventPreset> eventPresets;
        private class EventUser
        {
            public string UserName;
            public ulong UserID;
            public int Stage;
        }
        private List<EventUser> stages = new List<EventUser>();

        private Dictionary<string, bool> eventStates = new Dictionary<string, bool>
        {
            { "IsTeleport", false },
            { "IsStarted", false },
            { "ClanFFire", false }
        };

        private List<ulong> loadedPlayers = new List<ulong>(), playersOnEvent = new List<ulong>(), registeredPlayers = new List<ulong>(), WaitingRestore = new List<ulong>();

        private int currentStage;
        private ulong winnerOne, winnerTwo, winnerThree, previouslyWinner;

        private readonly Dictionary<ulong, Inventory.Transfer[]> KeepInventory = new Dictionary<ulong, Inventory.Transfer[]>();
        private readonly Dictionary<ulong, Vector3> KeepPosition = new Dictionary<ulong, Vector3>();

        private void LoadPluginToPlayer(PlayerClient pClient)
        {
            if (pClient.gameObject.GetComponent<EventVM>() != null) return;
            if (loadedPlayers.Contains(pClient.userID)) loadedPlayers.Remove(pClient.userID);

            EventVM vm = pClient.gameObject.AddComponent<EventVM>();
            vm.playerClient = pClient;
            vm.bEventMaster = this;

            loadedPlayers.Add(pClient.userID);
        }
        private void UnloadPluginFromPlayer(GameObject gameObject, Type plugin)
        {
            if (gameObject.GetComponent(plugin) != null) UnityEngine.Object.Destroy(gameObject.GetComponent(plugin));
        }

        private void Loaded()
        {
            foreach (var pc in PlayerClient.All) if (pc != null && pc.netPlayer != null) LoadPluginToPlayer(pc);
            currentEvent = new EventData { SpawnPoints = new List<string>(), GamePoints = new List<string>() };

            try { eventPresets = Interface.GetMod().DataFileSystem.ReadObject<List<EventPreset>>("EventPresets"); }
            catch { eventPresets = new List<EventPreset>(); SavePluginData(); }
        }
        private void Unload()
        {
            SavePluginData();
            foreach (ulong loadedPlayer in loadedPlayers)
            {
                PlayerClient playerClient; PlayerClient.FindByUserID(loadedPlayer, out playerClient);
                if (playerClient != null) UnloadPluginFromPlayer(playerClient.gameObject, typeof(EventVM));
            }
        }

        private void OnPlayerConnected(NetUser user)
        {
            if (user.playerClient != null) LoadPluginToPlayer(user.playerClient);
            if (eventStates["IsTeleport"]) GetEventVM(user.playerClient).SendRPC("OnEventStart");
        }
        private void OnPlayerDisconnected(uLink.NetworkPlayer networkPlayer)
        {
            NetUser user = NetUser.Find(networkPlayer);
            if (user != null) loadedPlayers.Remove(user.userID);

            if (playersOnEvent.Contains(user.userID)) playersOnEvent.Remove(user.userID);
            if (registeredPlayers.Contains(user.userID)) registeredPlayers.Remove(user.userID);

            EventUser eventUser = stages.Find(f => f.UserID == user.userID);
            if (eventUser != null) stages.Remove(eventUser);
        }

        private void OnServerSave() =>
            SavePluginData();
        private void SavePluginData() =>
            Interface.GetMod().DataFileSystem.WriteObject("EventPresets", eventPresets);

        private void AnnounceOnEvent(string message)
        {
            PlayerClient playerClient;
            for (int i = 0; i < registeredPlayers.Count; i++)
            {
                PlayerClient.FindByUserID(registeredPlayers[i], out playerClient);
                rust.Notice(playerClient.netUser, $"[Event]: {message}", "!", 4f);
            }
        }
        private void AnnounceToAll(string message, float duration = 4f)
        {
            for (int i = 0; i < PlayerClient.All.Count; i++)
            {
                PlayerClient playerClient = PlayerClient.All[i];
                //if (Users.Find(playerClient.userID).Rank > 99)
                rust.Notice(PlayerClient.All[i].netUser, $"[Event]: {message}", "!", duration);
            }
        }
        private void SendMessageToAll(string message)
        {
            for (int i = 0; i < PlayerClient.All.Count; i++)
            {
                PlayerClient playerClient = PlayerClient.All[i];
                //if (Users.Find(playerClient.userID).Rank > 99)
                rust.SendChatMessage(PlayerClient.All[i].netUser, "Event", $"[COLOR # 00FA9A] {message}");
            }
        }
        private void RPCToAll(string rpcName, params object[] args)
        {
            for (int i = 0; i < PlayerClient.All.Count; i++) //if (Users.Find(PlayerClient.All[i].userID).Rank > 99) 
                GetEventVM(PlayerClient.All[i]).SendRPC(rpcName, args);
        }

        private void InitAction(string action, params object[] args)
        {
            switch (action)
            {
                case "StartEvent":
                    {
                        AnnounceToAll("Внимание! Началась регистрация на ивент!");
                        timer.Once(1.2f, () =>
                        {
                            AnnounceToAll("При телепорте сохраняется Ваша позиция и инвентарь!");
                            timer.Once(1.2f, () =>
                            {
                                AnnounceToAll("Для тот, чтобы попасть на ивент - откройте инвентарь и нажмите на кнопку слева-посередине Вашего экрана.");
                                eventStates["IsTeleport"] = true;
                                RPCToAll("OnEventStart");
                            });
                        });

                        break;
                    }
                case "StopEvent":
                    {
                        eventStates["IsTeleport"] = false;
                        eventStates["IsStarted"] = false;

                        currentStage = 0;
                        previouslyWinner = 0;

                        for (int i = 0; i < registeredPlayers.Count; i++) RestorePlayer(NetUser.FindByUserID(registeredPlayers[i]));
                        registeredPlayers.Clear();

                        playersOnEvent.Clear();
                        registeredPlayers.Clear();
                        stages.Clear();

                        RPCToAll("OnEventTPClose");

                        break;
                    }
                case "CloseTeleport":
                    {
                        eventStates["IsTeleport"] = false;
                        eventStates["IsStarted"] = true;

                        EventTeleporter eventTeleporter = new GameObject("EventTeleporter").AddComponent<EventTeleporter>();
                        eventTeleporter.LocalUsers = new List<NetUser>();
                        eventTeleporter.bEventMaster = this;

                        for (int i = 0; i < registeredPlayers.Count; i++)
                        {
                            NetUser user = NetUser.FindByUserID(registeredPlayers[i]);

                            eventTeleporter.LocalUsers.Add(user);
                            SavePlayer(user);

                            timer.Once(0.5f, () =>
                            {
                                foreach (KeyValuePair<string, Dictionary<string, int>> eventItem in currentEvent.EventItems)
                                {
                                    bool isFastSlot = eventItem.Key == "FastSlots";
                                    foreach (KeyValuePair<string, int> keyValuePair in eventItem.Value)
                                        GiveItem(user, keyValuePair.Key, keyValuePair.Value, isFastSlot);
                                }
                            });
                        }

                        RPCToAll("OnEventTPClose");
                        break;
                    }

                case "KickPlayer":
                    {
                        if (playersOnEvent.Contains((ulong)args[0])) playersOnEvent.Remove((ulong)args[0]);
                        if (registeredPlayers.Contains((ulong)args[0])) registeredPlayers.Remove((ulong)args[0]);

                        EventUser eventUser = stages.Find(f => f.UserID == (ulong)args[0]);
                        if (eventUser != null) stages.Remove(eventUser);
                        break;
                    }

                case "AddSpawnPoint":
                    {
                        currentEvent.SpawnPoints.Add(new Vector3((float)args[0], (float)args[1], (float)args[2]).ToString());
                        break;
                    }
                case "AddGamePoint":
                    {
                        currentEvent.SpawnPoints.Add(new Vector3((float)args[0], (float)args[1], (float)args[2]).ToString());
                        break;
                    }

                case "RegisterToEvent":
                    {
                        NetUser user = NetUser.FindByUserID((ulong)args[0]);
                        UserData userData = Users.GetBySteamID((ulong)args[0]);
                        rust.Notice(user, "[Event]: Вы были успешно зарегистрированы на ивент! Ожидайте начала.");
                        registeredPlayers.Add(user.userID);

                        SendMessageToAll($"\"{userData.Username}\" зарегистрировался на ивент! Участников: \"{registeredPlayers.Count}\".");
                        SendMessageToAll($"Максимум участников: \"{currentEvent.Settings["MaxPlayers"]}\".");
                        SendMessageToAll("Чтобы попасть на ивент - откройте инвентарь...");
                        SendMessageToAll("...и нажмите на кнопку слева-посередине Вашего экрана.");

                        break;
                    }
            }
        }

        private void SavePlayer(NetUser user)
        {
            Inventory inventory = user.playerClient.rootControllable.idMain.GetComponent<Inventory>();
            ulong userID = user.userID;

            KeepInventory.Add(userID, inventory.GenerateOptimizedInventoryListing(Inventory.Slot.KindFlags.Armor | Inventory.Slot.KindFlags.Belt | Inventory.Slot.KindFlags.Default));
            KeepPosition.Add(userID, user.playerClient.lastKnownPosition);

            inventory.Clear();
        }
        private void RestorePlayer(NetUser user)
        {
            ulong userID = user.userID;
            if (KeepInventory.ContainsKey(userID) && KeepPosition.ContainsKey(userID))
            {
                Inventory inventory = user.playerClient.rootControllable.idMain.GetComponent<Inventory>();
                inventory.Clear();

                for (int i = 0; i < KeepInventory[userID].Length; i++)
                {
                    IInventoryItem IItem = inventory.AddItem(ref KeepInventory[userID][i].addition);
                    inventory.MoveItemAtSlotToEmptySlot(inventory, IItem.slot, KeepInventory[userID][i].item.slot);
                }

                Helper.TeleportTo(user, KeepPosition[userID] + new Vector3(0.0f, 1.5f, 0.0f));

                KeepInventory.Remove(userID);
                KeepPosition.Remove(userID);
            }
        }
        private void RemovePlayerSack(ulong userID)
        {
            PlayerClient playerClient;
            PlayerClient.FindByUserID(userID, out playerClient);

            Collider[] colliders = Physics.OverlapSphere(playerClient.lastKnownPosition, 2f);

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];

                Inventory inventory = playerClient.controllable.GetComponent<Inventory>();
                inventory.Clear();

                if (collider.gameObject.name.ToLower().Contains("lootsack"))
                    NetCull.Destroy(collider.GetComponent<LootableObject>().gameObject);
            }
        }
        private void GiveItem(NetUser user, string item, int amount, bool isFastSlot = false)
        {
            Inventory.Slot.KindFlags kindFlags = isFastSlot ? Inventory.Slot.KindFlags.Belt : Inventory.Slot.KindFlags.Default;
            Inventory.Slot.Preference preference = Inventory.Slot.Preference.Define(Inventory.Slot.Kind.Armor, false, kindFlags);
            var inv = user.playerClient.rootControllable.idMain.GetComponent<Inventory>();
            inv.AddItemAmount(DatablockDictionary.GetByName(item), amount, preference);
        }

        public void GoToWaiting(NetUser user)
        {
            if (user.playerClient == null || user.playerClient.netPlayer == null)
            {
                WaitingRestore.Add(user.userID);
                registeredPlayers.Remove(user.userID);
                return;
            }
            string spawnPoint;
            List<string> spawnPoints;

            spawnPoints = currentEvent.SpawnPoints;
            spawnPoint = spawnPoints[Core.Random.Range(0, spawnPoints.Count)];

            Helper.TeleportTo(user, ToVector3(spawnPoint));
        }
        public void SetDuelPlayers()
        {

        }

        private Vector3 ToVector3(string vector)
        {
            if (vector.StartsWith("(") && vector.EndsWith(")"))
                vector = vector.Substring(1, vector.Length - 2);

            var sArray = vector.Split(',');
            return new Vector3(float.Parse(sArray[0]), float.Parse(sArray[1]), float.Parse(sArray[2]));
        }

        private EventVM GetEventVM(PlayerClient playerClient) => playerClient.gameObject.GetComponent<EventVM>();

        internal class EventVM : MonoBehaviour
        {
            public PlayerClient playerClient;
            public BEventMaster bEventMaster;

            [RPC]
            public void GetPresets()
            {
                foreach (var preset in bEventMaster.eventPresets) SendRPC("AddPreset", preset.PresetName);
            }
            [RPC]
            public void SavePreset(string presetName)
            {
                bEventMaster.eventPresets.Add(new EventPreset
                {
                    PresetName = presetName,
                    Event = bEventMaster.currentEvent
                });
                bEventMaster.SavePluginData();
            }
            [RPC]
            public void RemovePreset(string presetName)
            {
                bEventMaster.eventPresets.Remove(bEventMaster.eventPresets.Find(f => f.PresetName == presetName));
                bEventMaster.SavePluginData();
            }

            [RPC]
            public void ApplySettings(string settingName, object value) =>
                bEventMaster.currentEvent.Settings[settingName] = value;

            [RPC]
            public void ApplyArmor(string slot1, string slot2, string slot3, string slot4)
            {
                bEventMaster.currentEvent.EventItems["Armor"] = new Dictionary<string, int> { { slot1, 1 }, { slot2, 1 }, { slot3, 1 }, { slot4, 1 } };
            }
            [RPC]
            public void ApplyInventory(string[] items, int[] counts)
            {
                Dictionary<string, int> temp = new Dictionary<string, int>();
                for (int i = 0; i < items.Length; i++) temp.Add(items[i], counts[i]);
                bEventMaster.currentEvent.EventItems["Inventory"] = temp;
            }
            [RPC]
            public void ApplyFastSlots(string[] items, int[] counts)
            {
                Dictionary<string, int> temp = new Dictionary<string, int>();
                for (int i = 0; i < items.Length; i++) temp.Add(items[i], counts[i]);
                bEventMaster.currentEvent.EventItems["FastSlots"] = temp;
            }

            [RPC]
            public void StartEvent() =>
                bEventMaster.InitAction("StartEvent");
            [RPC]
            public void StopEvent() =>
                bEventMaster.InitAction("StopEvent");
            [RPC]
            public void CloseTeleport() =>
                bEventMaster.InitAction("CloseTeleport");

            [RPC]
            public void GetEventPlayers()
            {
                foreach (EventUser eventUser in bEventMaster.stages) SendRPC("AddEventPlayer", eventUser.UserName);
            }
            [RPC]
            public void KickPlayer(string playerName) =>
                bEventMaster.InitAction("KickPlayer", Users.GetByUserName(playerName).SteamID);

            [RPC]
            public void GetAllPoints()
            {
                foreach (string point in bEventMaster.currentEvent.SpawnPoints) SendRPC("SetSpawnPoint", point);
                foreach (string point in bEventMaster.currentEvent.GamePoints) SendRPC("SetGamePoint", point);
            }
            [RPC]
            public void AddSpawnPoint(float x, float y, float z) =>
                bEventMaster.InitAction("AddSpawnPoint", x, y, z);
            [RPC]
            public void AddGamePoint(float x, float y, float z) =>
                bEventMaster.InitAction("AddGamePoint", x, y, z);
            [RPC]
            public void RemoveSpawnPoint(string point) =>
                bEventMaster.currentEvent.SpawnPoints.Remove(point);
            [RPC]
            public void RemoveGamePoint(string point) =>
                bEventMaster.currentEvent.GamePoints.Remove(point);

            [RPC]
            public void EventRegister() =>
                bEventMaster.InitAction("RegisterToEvent", playerClient);

            public void SendRPC(string rpcName, params object[] param) =>
                GetComponent<Facepunch.NetworkView>().RPC(rpcName, playerClient.netPlayer, param);
        }
        internal class EventTeleporter : MonoBehaviour
        {
            public List<NetUser> LocalUsers;
            public BEventMaster bEventMaster;

            public void StartTP() => StartCoroutine(Teleport());
            private IEnumerator Teleport()
            {
                for (int i = 0; i < LocalUsers.Count; i++)
                { bEventMaster.GoToWaiting(LocalUsers[i]); yield return new WaitForSeconds(0.25f); }
                if ((bool)bEventMaster.currentEvent.Settings["IsDuel"]) bEventMaster.SetDuelPlayers();
            }
        }
    }
}