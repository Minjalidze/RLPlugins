using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using RustExtended;

using Oxide.Core;
using Random = Oxide.Core.Random;
//2022 year code
namespace Oxide.Plugins
{
    [Info("BUltimateEvents (21.01)", "systemXcrackedZ", "2.3.1")]
    public class BUltimateEvents : RustLegacyPlugin
    {
        private class Team
        {
            public string Name;
            public List<ulong> Members;
        }
        private class SpawnPoint
        {
            public string Team;
            public string Point;
        }
        private class GamePoint
        {
            public string Team;
            public string Point;
        }
        private class Flag
        {
            public string Team;
            public string Point;
        }
        private class Item
        {
            public string Team;
            public string Name;
            public int Amount;
        }
        private class EiEvent
        {
            public string Name;
            public List<Team> Teams;
            public List<SpawnPoint> SpawnPoints;
            public List<GamePoint> GamePoints;
            public List<Flag> Flags;
            public List<Item> Items;
        }

        private List<EiEvent> _events = new List<EiEvent>();
        private Dictionary<Team, int> FlagCount = new Dictionary<Team, int>();
        private Dictionary<Flag, bool> IsFlagCaptured = new Dictionary<Flag, bool>();
        private Dictionary<ulong, Flag> WaitingDelivery = new Dictionary<ulong, Flag>();
        private Dictionary<ulong, Flag> WaitingCapturing = new Dictionary<ulong, Flag>();

        private KeyValuePair<ulong, ulong> _duelPlayers;

        private Dictionary<ulong, Inventory.Transfer[]> KeepInventory = new Dictionary<ulong, Inventory.Transfer[]>();
        private Dictionary<ulong, Vector3> KeepPosition = new Dictionary<ulong, Vector3>();

        private List<ulong> RegisteredPlayers = new List<ulong>();
        private List<ulong> TeleportedToEvent = new List<ulong>();

        private List<ulong> Captured = new List<ulong>();

        private List<ulong> WaitingRestore = new List<ulong>();
        private List<ulong> WaitingRespawn = new List<ulong>();

        private string _currentEventName = "";
        private bool _isEventStarted = false;
        private bool _clanFFireState = false;
        private int _teleportedPlayersCount = 0;
        private ulong _previouslyWinner = 0;

        [ChatCommand("event")]
        private void OnCMD_Event(NetUser user, string cmd, string[] args)
        {
            if (!user.CanAdmin()) return;
            if (args.Length < 1)
            { SendCommands(user); return; }
            switch (args[0])
            {
                case "sp":
                    {
                        var eiEvent = _events.Find(eEvent => eEvent.Name == args[1]);
                        if (eiEvent == null)
                        { SendCommands(user); return; }
                        eiEvent.SpawnPoints.Add(new SpawnPoint { Team = args[2], Point = user.playerClient.lastKnownPosition.ToString() });
                        SaveData();
                        return;
                    }
                case "gp":
                    {
                        var eiEvent = _events.Find(eEvent => eEvent.Name == args[1]);
                        if (eiEvent == null)
                        { SendCommands(user); return; }
                        eiEvent.GamePoints.Add(new GamePoint { Team = args[2], Point = user.playerClient.lastKnownPosition.ToString() });
                        SaveData();
                        return;
                    }
                case "fp":
                    {
                        var eiEvent = _events.Find(eEvent => eEvent.Name == args[1]);
                        if (eiEvent == null)
                        { SendCommands(user); return; }
                        eiEvent.Flags.Add(new Flag { Team = args[2], Point = user.playerClient.lastKnownPosition.ToString() });
                        SaveData();
                        return;
                    }
                case "kick":
                    {
                        var userData = Users.Find(args[0]);
                        if (userData == null) { SendCommands(user); return; }

                        var userID = userData.SteamID;

                        RestorePlayer(userData.SteamID);
                        if (RegisteredPlayers.Contains(userID))
                            RegisteredPlayers.Remove(userID);

                        var currentEvent = _events.Find(eEvent => eEvent.Name == _currentEventName);
                        currentEvent.Teams.Find(team => team.Members.Contains(userData.SteamID)).Members.Remove(userID);

                        _teleportedPlayersCount--;

                        if (currentEvent.Name == "Захват Флага")
                        {
                            if (WaitingCapturing.ContainsKey(userID))
                                WaitingCapturing.Remove(userID);
                            if (WaitingDelivery.ContainsKey(userID))
                                WaitingDelivery.Remove(userID);
                            if (Captured.Contains(userID))
                                Captured.Remove(userID);
                        }
                        else
                            TeleportedToEvent.Remove(userID);
                        SaveData();
                        return;
                    }
                case "stop":
                    { StopEvent(); return; }
                case "cancel":
                    { CancelEvent(); return; }
            }

            _currentEventName = args[0];
            
            if (_events.Find(eEvent => eEvent.Name == _currentEventName) == null)
            { SendCommands(user); _currentEventName = ""; return; }
            
            NoticeAll($"Запущен ивент: {args[0]}! Для записи на ивент используйте: /goevent");
            _isEventStarted = true;
            Debug.Log($"[EVENT INFO]: Запущен ивент \"{args[0]}\"!");
        }
        [ChatCommand("goevent")]
        private void OnCMD_GoEvent(NetUser user, string cmd, string[] args)
        {
            if (!_isEventStarted || RegisteredPlayers.Contains(user.userID)) { return; }

            RegisterPlayerToEvent(user.userID);
        }
        
        private void RegisterPlayerToEvent(ulong userID)
        {
            var netUser = NetUser.FindByUserID(userID);
            if (netUser == null)
            {
                Debug.Log($"[EVENT WARNING]: UserID {userID} попытался зарегистрироваться на ивент, но NetUser == null!");
                return;
            }
            Debug.Log($"[EVENT INFO]: Зарегистрирован на ивент \"{netUser.displayName}\"!");
            rust.Notice(netUser, "[EVENT]: Вы были успешно зарегистрированы на ивент! Ожидайте начала.");
            var currentEvent = _events.Find(eEvent => eEvent.Name == _currentEventName);
            
            RegisteredPlayers.Add(userID);
            _teleportedPlayersCount++;

            foreach (var playerClient in PlayerClient.All)
            {
                SendMessage(playerClient.netUser, 
                    $"\"{Users.GetBySteamID(userID).Username}\" зарегистрировался на ивент! Участников: \"{_teleportedPlayersCount}\".");
                SendMessage(playerClient.netUser, 
                    $"Чтобы попасть на ивент введите команду: /goevent");
            }
            
            if (currentEvent.Name == "Турнир Одиночек") { return; }
            
            foreach (var playerClient in PlayerClient.All)
                SendMessage(playerClient.netUser, 
                    $"Участников для автоматического начала ивента: \"{_teleportedPlayersCount}/12\"");
            
            if (currentEvent.Name == "Захват Флага" && _teleportedPlayersCount == 12) StopEvent();
        }

        private void StopEvent()
        {
            NoticeAll($"Телепортация на ивент была закончена.");
            Debug.Log($"[EVENT INFO]: Остановлена телепортация на ивент \"{_currentEventName}\"!");

            if (!_isEventStarted) return;
            _isEventStarted = false;

            switch (_currentEventName)
            {
                case "Захват Флага": StartFlagEvent(); return;
                case "Турнир Одиночек": StartTournamentEvent(); return;
            }
        }
        private void CancelEvent()
        {
            NoticeAll($"Ивент \"{_currentEventName}\" был окончен!");
            Debug.Log($"[EVENT INFO]: Окончен ивент \"{_currentEventName}\"!");

            FlagCount.Clear();
            IsFlagCaptured.Clear();
            WaitingDelivery.Clear();
            WaitingCapturing.Clear();

            RegisteredPlayers.Clear();
            Captured.Clear();

            if (_currentEventName == "Турнир Одиночек") { foreach (var teleported in TeleportedToEvent) { RestorePlayer(teleported); } }
            else if (_currentEventName == "Захват Флага") { foreach (var eEvent in _events) { foreach (var eventTeam in eEvent.Teams) { foreach (var member in eventTeam.Members) { RestorePlayer(member); } eventTeam.Members.Clear(); } } }

            _currentEventName = "";
            _isEventStarted = false;
            _clanFFireState = false;
            _teleportedPlayersCount = 0;
            _previouslyWinner = 0;

            TeleportedToEvent.Clear();
            SaveData();

            timer.Once(1f, () => { rust.RunServerCommand($"oxide.reload {Name}"); });
        }

        private void StartFlagEvent()
        {
            var currentEvent = _events.Find(eEvent => eEvent.Name == _currentEventName);

            var whiteTeam = currentEvent.Teams[0];
            var blackTeam = currentEvent.Teams[1];

            FlagCount.Add(whiteTeam, 0);
            FlagCount.Add(blackTeam, 0);

            foreach (var flag in currentEvent.Flags)
                IsFlagCaptured.Add(flag, false);
            
            foreach (var whiteTeamMember in whiteTeam.Members)
            {
                var user = NetUser.FindByUserID(whiteTeamMember);
                
                SendMessage(user, "Игра началась! Вы играете за команду: \"Белые\"!");
                
                var spawnPoints = currentEvent.SpawnPoints.FindAll(point => point.Team == whiteTeam.Name);
                var randomTeleportPoint = Random.Range(0, spawnPoints.Count);
                
                RegisteredPlayers.Remove(whiteTeamMember);
                SavePlayer(user.userID);
                
                timer.Once(0.5f, () =>
                {
                    Helper.TeleportTo(user, ToVector3(spawnPoints[randomTeleportPoint].Point));
                    foreach (var item in currentEvent.Items.Where(itemTeam => itemTeam.Team == whiteTeam.Name)) 
                        GiveItem(user, item.Name, item.Amount);
                    
                    var character = user.playerClient.controllable.idMain;
                    var damage = character.takeDamage as HumanBodyTakeDamage;
                    if (damage == null) return;
                    damage.maxHealth = 200;
                    damage.Heal(character.idMain, 200 - damage.health);
                });
            }
            foreach (var blackTeamMember in blackTeam.Members)
            {
                var user = NetUser.FindByUserID(blackTeamMember);
                SendMessage(user, "Игра началась! Вы играете за команду: \"Чёрные\"!");
                
                var spawnPoints = currentEvent.SpawnPoints.FindAll(point => point.Team == blackTeam.Name);
                var randomTeleportPoint = Random.Range(0, spawnPoints.Count);
                
                RegisteredPlayers.Remove(blackTeamMember);
                SavePlayer(user.userID);
                
                timer.Once(0.5f, () =>
                {
                    Helper.TeleportTo(user, ToVector3(spawnPoints[randomTeleportPoint].Point));
                    foreach (var item in currentEvent.Items.Where(itemTeam => itemTeam.Team == blackTeam.Name)) 
                        GiveItem(user, item.Name, item.Amount);
                    
                    var character = user.playerClient.controllable.idMain;
                    var damage = character.takeDamage as HumanBodyTakeDamage;
                    damage.maxHealth = 200;
                    damage.Heal(character.idMain, 200 - damage.health);
                });
            }
            SaveData();
        }
        private void StartTournamentEvent()
        {
            var currentEvent = _events.Find(eEvent => eEvent.Name == _currentEventName);

            foreach (var registered in RegisteredPlayers)
            {
                var netUser = NetUser.FindByUserID(registered);
                if (netUser == null)
                {
                    Debug.Log($"[EVENT WARNING]: UserID {registered} попытка телепорта на ивент, но NetUser == null!");
                    continue;
                }

                TeleportedToEvent.Add(registered);
                SavePlayer(registered);

                timer.Once(0.5f, () =>
                {
                    Helper.TeleportTo(netUser, ToVector3(currentEvent.SpawnPoints.Find(f => f.Team == "White").Point));
                    foreach (var item in currentEvent.Items.Where(itemTeam => itemTeam.Team == "White"))
                    { GiveItem(netUser, item.Name, item.Amount); }
                });
            }
            RegisteredPlayers.Clear();
            SelectArenaDuel();
            SaveData();
        }

        private void TeleportToArena(ulong playerOne, ulong playerTwo)
        {
            var currentEvent = _events.Find(eEvent => eEvent.Name == _currentEventName);

            var wGamePoints = currentEvent.GamePoints.FindAll(point => point.Team == "White");
            var bGamePoints = currentEvent.GamePoints.FindAll(point => point.Team == "Black");

            var randomWhite = Random.Range(0, wGamePoints.Count);
            var randomBlack = Random.Range(0, bGamePoints.Count);

            var netUserOne = NetUser.FindByUserID(_duelPlayers.Key);
            var netUserTwo = NetUser.FindByUserID(_duelPlayers.Value);

            Helper.TeleportTo(netUserOne, ToVector3(wGamePoints[randomWhite].Point));
            Helper.TeleportTo(netUserTwo, ToVector3(bGamePoints[randomBlack].Point));

            SetPlayerState(netUserOne.playerClient);
            SetPlayerState(netUserTwo.playerClient);

            Debug.Log($"[EVENT INFO]: \"{Users.GetBySteamID(_duelPlayers.Key).Username}\" vs \"{Users.GetBySteamID(_duelPlayers.Value).Username}\"!");
            foreach (var playerClient in PlayerClient.All)
            {
                rust.Notice(playerClient.netUser, $"[EVENT]: \"{Users.GetBySteamID(_duelPlayers.Key).Username}\" vs \"{Users.GetBySteamID(_duelPlayers.Value).Username}\"");
            }
        }
        private void BackToArena(ulong userID)
        {
            var currentEvent = _events.Find(eEvent => eEvent.Name == _currentEventName);
            var spawnPoints = currentEvent.SpawnPoints.FindAll(point => point.Team == "White")[0];

            var netUser = NetUser.FindByUserID(userID);
            if (netUser == null)
            {
                Debug.Log($"[EVENT WARNING]: UserID {userID} попытка вернуться на арену, но NetUser == null!");
                return;
            }

            Helper.TeleportTo(netUser, ToVector3(spawnPoints.Point));
        }

        private void SavePlayer(ulong userID)
        {
            var netUser = NetUser.FindByUserID(userID);
            if (netUser == null)
            {
                Debug.Log($"[EVENT WARNING]: UserID {userID} попытка сохранения лута, но NetUser == null!");
                return;
            }

            var inventory = netUser.playerClient.rootControllable.idMain.GetComponent<Inventory>();

            KeepInventory.Add(userID, inventory.GenerateOptimizedInventoryListing(Inventory.Slot.KindFlags.Armor | Inventory.Slot.KindFlags.Belt | Inventory.Slot.KindFlags.Default));
            KeepPosition.Add(userID, netUser.playerClient.lastKnownPosition);

            inventory.Clear();
        }
        private void RestorePlayer(ulong userID)
        {
            var netUser = NetUser.FindByUserID(userID);
            if (netUser == null)
            {
                Debug.Log($"[EVENT WARNING]: UserID {userID} попытка возвращения лута, но NetUser == null!");
                return;
            }

            if (!KeepInventory.ContainsKey(userID) || !KeepInventory.ContainsKey(userID)) { return; }

            var inv = netUser.playerClient.rootControllable.idMain.GetComponent<Inventory>();
            inv.Clear();

            for (int i = 0; i < KeepInventory[userID].Length; i++)
            {
                IInventoryItem IItem = inv.AddItem(ref KeepInventory[userID][i].addition);
                inv.MoveItemAtSlotToEmptySlot(inv, IItem.slot, KeepInventory[userID][i].item.slot);
            }

            Helper.TeleportTo(netUser, KeepPosition[userID]);

            KeepInventory.Remove(userID);
            KeepPosition.Remove(userID);
        }
        private void SetPlayerState(PlayerClient playerClient)
        {
            var character = playerClient.controllable.idMain;
            var tDamage = character.takeDamage as HumanBodyTakeDamage;
            if (tDamage != null)
            {
                tDamage.maxHealth = 100;
                tDamage.Heal(character.idMain, 100 - tDamage.health);
            }

            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("9mm Ammo"), 50);
            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Large Medkit"), 5);
        }

        private void RemoveLoot(ulong userID)
        {
            PlayerClient playerClient;
            PlayerClient.FindByUserID(userID, out playerClient);

            foreach (var collider in Physics.OverlapSphere(playerClient.lastKnownPosition, 2f))
            {
                var inv = playerClient.controllable.GetComponent<Inventory>();
                inv.Clear();

                if (collider.gameObject.name.ToLower().Contains("lootsack"))
                {
                    NetCull.Destroy(collider.GetComponent<LootableObject>().gameObject);
                }
            }
        }

        private void OnPlayerDisconnected(uLink.NetworkPlayer networkPlayer)
        {
            var netUser = NetUser.Find(networkPlayer);
            if (netUser == null)
            {
                Debug.Log($"[EVENT WARNING]: PlayerID {networkPlayer.id} попытка удаления с ивента после выхода, но NetUser == null!");
                return;
            }
            var userID = netUser.userID;

            if (WaitingDelivery.ContainsKey(userID))
                WaitingDelivery.Remove(userID);
            if (WaitingCapturing.ContainsKey(userID))
                WaitingCapturing.Remove(userID);

            if (Captured.Contains(userID))
                Captured.Remove(userID);

            if (WaitingRespawn.Contains(userID))
                WaitingRespawn.Remove(userID);
            if (!WaitingRestore.Contains(userID))
                WaitingRestore.Add(userID);

            if (RegisteredPlayers.Contains(userID))
                {RegisteredPlayers.Remove(userID); _teleportedPlayersCount--;}

            if (TeleportedToEvent.Contains(userID))
                TeleportedToEvent.Remove(userID);
        }
        private void OnKilled(TakeDamage takeDamage, DamageEvent damage)
        {
            try
            {
                if (_currentEventName == "" || !(takeDamage is HumanBodyTakeDamage))
                    return;
                
                var attackerUser = damage.attacker.client.netUser; if (attackerUser == null) return;
                var victimUser = damage.victim.client.netUser; if (victimUser == null) return;

                if (_currentEventName == "Захват Флага" && !_isEventStarted)
                {
                    RemoveLoot(victimUser.userID);

                    var currentEvent = _events.Find(eEvent => eEvent.Name == _currentEventName);

                    var attackerTeam = currentEvent.Teams.Find(team => team.Members.Contains(attackerUser.userID)); if (attackerTeam == null) {return;}
                    var victimTeam = currentEvent.Teams.Find(team => team.Members.Contains(victimUser.userID)); if (victimTeam == null) {return;}

                    WaitingRespawn.Add(victimUser.userID);
                    if (Captured.Contains(victimUser.userID)) Captured.Remove(victimUser.userID);

                    SaveData();
                }
                if (_currentEventName == "Турнир Одиночек" && !_isEventStarted)
                {
                    if (victimUser == attackerUser || (_duelPlayers.Key != attackerUser.userID && _duelPlayers.Key != victimUser.userID)) return;

                    RemoveLoot(victimUser.userID);

                    var clanAttacker = Users.GetBySteamID(attackerUser.userID).Clan;
                    if (clanAttacker != null)
                    {
                        var clanVictim = Users.GetBySteamID(victimUser.userID).Clan;
                        if (clanVictim != null)
                            if (clanAttacker == clanVictim && !clanAttacker.FrendlyFire && _clanFFireState) 
                                { clanAttacker.FrendlyFire = true; _clanFFireState = false; }
                    }

                    TeleportedToEvent.Remove(victimUser.userID);
                    WaitingRestore.Add(victimUser.userID);

                    if (TeleportedToEvent.Count == 1)
                    {
                        CancelEvent(); 
                        NoticeAll($"[EVENT]: Победитель на ивенте: \"{Users.GetBySteamID(attackerUser.userID).Username}\"! Поздравим!", 8f);
                        return;
                    }

                    _previouslyWinner = attackerUser.userID;

                    rust.Notice(attackerUser, "Вы проходите в следующий этап!");
                    NoticeAll($"[EVENT]: \"{Users.GetBySteamID(attackerUser.userID).Username}\" Win!");

                    BackToArena(attackerUser.userID);
                    SelectArenaDuel();
                    SaveData();
                }
            }
            catch (Exception e)
            {
                if (_currentEventName == "" || !(takeDamage is HumanBodyTakeDamage)) return;
                Debug.LogError($"[EVENT ERROR]: {e.Message}");
            }
        }
        private void OnGetClientMove(HumanController controller, Vector3 origin)
        {
            try
            {
                var playerClient = controller.playerClient;
                
                var lastKnownPosition = playerClient.lastKnownPosition;
                var currentEvent = _events.Find(eEvent => eEvent.Name == _currentEventName);

                if (currentEvent.Name != "Захват Флага") return;
                
                var playerTeam = currentEvent.Teams.Find(point => point.Members.Contains(playerClient.userID));
                if (playerTeam == null) return;

                var teamFlag = currentEvent.Flags.Find(flag => flag.Team == playerTeam.Name);
                var enemyFlag = currentEvent.Flags.Find(flag => flag.Team != playerTeam.Name);

                var inv = playerClient.controllable.GetComponent<Inventory>();
                    
                if (Vector3.Distance(lastKnownPosition, ToVector3(teamFlag.Point)) < 3 && Captured.Contains(playerClient.userID))
                {
                    if (Captured.Contains(playerClient.userID) && !WaitingDelivery.ContainsKey(playerClient.userID))
                    {
                        WaitingDelivery.Add(playerClient.userID, enemyFlag);
                        timer.Once(5f, () =>
                        {
                            if (!(Vector3.Distance(lastKnownPosition, ToVector3(teamFlag.Point)) < 3) || !Captured.Contains(playerClient.userID)) return;
                               
                            FlagCount[playerTeam]++; IsFlagCaptured[enemyFlag] = false;
                            Captured.Remove(playerClient.userID); WaitingDelivery.Remove(playerClient.userID);
                                
                            inv.RemoveItem(37);

                            var vest = "Cloth Vest";
                            if (playerTeam.Name == "Black") vest = "Kevlar Vest";
                                
                            GiveItem(NetUser.FindByUserID(playerClient.userID), vest, 1);
                            rust.Notice(NetUser.FindByUserID(playerClient.userID), "Вы успешно доставили флаг на свою базу!");
                                
                            if (FlagCount[playerTeam] == 3) CancelEvent();
                        });
                    }
                }
                else
                {
                    if (WaitingDelivery.ContainsKey(playerClient.userID)) WaitingDelivery.Remove(playerClient.userID);
                }
                if (Vector3.Distance(lastKnownPosition, ToVector3(enemyFlag.Point)) < 3 && !Captured.Contains(playerClient.userID))
                {
                    if (Captured.Contains(playerClient.userID) || WaitingCapturing.ContainsKey(playerClient.userID) || IsFlagCaptured[enemyFlag]) return;
                        
                    WaitingCapturing.Add(playerClient.userID, enemyFlag);
                    timer.Once(5f, () =>
                    {
                        if (Captured.Contains(playerClient.userID)) return;
                        WaitingCapturing.Remove(playerClient.userID);
                                
                        if (!(Vector3.Distance(lastKnownPosition, ToVector3(enemyFlag.Point)) < 3)) return;
                                
                        Captured.Add(playerClient.userID); IsFlagCaptured[enemyFlag] = true;
                                    
                        inv.RemoveItem(37);
                        GiveItem(NetUser.FindByUserID(playerClient.userID), "Leather Vest", 1);
                        rust.Notice(NetUser.FindByUserID(playerClient.userID), "Вы захватили флаг! Отнесите его к своей базе!");
                    });
                }
                else
                {
                    if (WaitingCapturing.ContainsKey(playerClient.userID)) WaitingCapturing.Remove(playerClient.userID);
                }
            }
            catch { /*stfu legacy*/ }
        }
        private void OnPlayerSpawn(PlayerClient playerClient)
        {
            if (WaitingRespawn.Contains(playerClient.userID))
            {
                var currentEvent = _events.Find(eEvent => eEvent.Name == _currentEventName);
                var playerTeam = currentEvent.Teams.Find(team => team.Members.Contains(playerClient.userID));
                
                var inv = playerClient.rootControllable.idMain.GetComponent<Inventory>();
                inv.Clear();
                
                timer.Once(0.5f, () =>
                {
                    BackToArena(playerClient.userID);

                    foreach (var item in currentEvent.Items.Where(item => item.Team == playerTeam.Name))
                        GiveItem(playerClient.netUser, item.Name, item.Amount);

                    WaitingRespawn.Remove(playerClient.userID);
                    
                    var character = playerClient.controllable.idMain;
                    var damage = character.takeDamage as HumanBodyTakeDamage;
                    damage.maxHealth = 200;
                    damage.Heal(character.idMain, 200 - damage.health);
                });
            }
            if (WaitingRestore.Contains(playerClient.userID))
            {
                RestorePlayer(playerClient.userID);
                WaitingRestore.Remove(playerClient.userID);
            }
        }
        private object ModifyDamage(TakeDamage takeDamage, DamageEvent damage)
        {
            try
            {
                var attackerUser = damage.attacker.client.netUser;
                if (attackerUser == null) return null;
                var victimUser = damage.victim.client.netUser;
                if (victimUser == null) return null;

                if (_currentEventName != "Захват Флага") return null;
                var currentEvent = _events.Find(eEvent => eEvent.Name == _currentEventName);

                var attackerTeam = currentEvent.Teams.Find(team => team.Members.Contains(attackerUser.userID));
                if (attackerTeam == null) {return null;}
                var victimTeam = currentEvent.Teams.Find(team => team.Members.Contains(victimUser.userID));
                if (victimTeam == null) {return null;}

                if (attackerTeam == victimTeam)
                {
                    damage.amount = 0;
                    return damage;
                }
                damage.amount = 25;
                return damage;
            }
            catch { /*stfu legacy*/ }
            return null;
        }

        #region [DATA]

        private void LoadData()
        {
            _events = Interface.GetMod().DataFileSystem.ReadObject<List<EiEvent>>("UltimateEventsData");
            if (_events.Count == 0)
            {
                _events.Add(new EiEvent
                {
                    Name = "Турнир Одиночек",
                    Teams = new List<Team>(),
                    SpawnPoints = new List<SpawnPoint>(),
                    GamePoints = new List<GamePoint>(),
                    Flags = new List<Flag>(),
                    Items = new List<Item>
                    {
                        new Item { Team = "White", Name = "Leather Helmet", Amount = 1},
                        new Item { Team = "White", Name = "Leather Vest", Amount = 1},
                        new Item { Team = "White", Name = "Leather Pants", Amount = 1},
                        new Item { Team = "White", Name = "Leather Boots", Amount = 1},
                        new Item { Team = "White", Name = "P250", Amount = 1},
                        new Item { Team = "White", Name = "Large Medkit", Amount = 10},
                        new Item { Team = "White", Name = "9mm Ammo", Amount = 150},
                        //////////////////////////////////////////////////////////////
                        new Item { Team = "Black", Name = "Leather Helmet", Amount = 1},
                        new Item { Team = "Black", Name = "Leather Vest", Amount = 1},
                        new Item { Team = "Black", Name = "Leather Pants", Amount = 1},
                        new Item { Team = "Black", Name = "Leather Boots", Amount = 1},
                        new Item { Team = "Black", Name = "P250", Amount = 1},
                        new Item { Team = "Black", Name = "Large Medkit", Amount = 10},
                        new Item { Team = "Black", Name = "9mm Ammo", Amount = 150}
                    }
                });
                _events.Add(new EiEvent
                {
                    Name = "Захват Флага",
                    Teams = new List<Team>
                    { 
                        new Team { Name = "White", Members = new List<ulong>() }, 
                        new Team { Name = "Black", Members = new List<ulong>() } 
                    },
                    SpawnPoints = new List<SpawnPoint>(),
                    GamePoints = new List<GamePoint>(),
                    Flags = new List<Flag>(),
                    Items = new List<Item>
                    {
                        new Item { Team = "White", Name = "Cloth Helmet", Amount = 1},
                        new Item { Team = "White", Name = "Cloth Vest", Amount = 1},
                        new Item { Team = "White", Name = "Cloth Pants", Amount = 1},
                        new Item { Team = "White", Name = "Cloth Boots", Amount = 1},
                        new Item { Team = "White", Name = "P250", Amount = 1},
                        new Item { Team = "White", Name = "Large Medkit", Amount = 10},
                        new Item { Team = "White", Name = "9mm Ammo", Amount = 150},
                        //////////////////////////////////////////////////////////////
                        new Item { Team = "Black", Name = "Kevlar Helmet", Amount = 1},
                        new Item { Team = "Black", Name = "Kevlar Vest", Amount = 1},
                        new Item { Team = "Black", Name = "Kevlar Pants", Amount = 1},
                        new Item { Team = "Black", Name = "Kevlar Boots", Amount = 1},
                        new Item { Team = "Black", Name = "P250", Amount = 1},
                        new Item { Team = "Black", Name = "Large Medkit", Amount = 10},
                        new Item { Team = "Black", Name = "9mm Ammo", Amount = 150}
                    }
                });
                
                SaveData();
            }
        }
        private void SaveData() { Interface.GetMod().DataFileSystem.WriteObject("UltimateEventsData", _events); }

        private void Loaded() 
        {
            LoadData();
            Debug.Log("[EVENT INFO]: Плагин успешно инициализирован!");
        }
        private void Unload() { SaveData(); }
        private void OnServerSave() { SaveData(); }
        
        #endregion
        
        private void SelectArenaDuel()
        {
            GetRandomPlayers();

            var clanOne = Users.GetBySteamID(_duelPlayers.Key).Clan;
            if (clanOne != null)
            {
                var clanTwo = Users.GetBySteamID(_duelPlayers.Value).Clan;
                if (clanTwo != null) { if (clanOne == clanTwo && clanOne.FrendlyFire) { _clanFFireState = true; clanOne.FrendlyFire = false; } }
            }

            timer.Once(4.0f, () => { TeleportToArena(_duelPlayers.Key, _duelPlayers.Value); });
        }
        private void GiveItem(NetUser user, string item, int amount)
        {
            var preference = Inventory.Slot.Preference.Define(Inventory.Slot.Kind.Armor,false,Inventory.Slot.KindFlags.Belt);
            var inv = user.playerClient.rootControllable.idMain.GetComponent<Inventory>();
            inv.AddItemAmount(DatablockDictionary.GetByName(item), amount, preference);
        }
        private void SendCommands(NetUser user)
        {
            SendMessage(user, "Список доступных команд:");
            SendMessage(user, "/event sp - установить точку спавна: /event sp <ивент> <команда>");
            SendMessage(user, "/event gp - установить точку игрового действия: /event gp <ивент> <команда>");
            SendMessage(user, "/event fp - установить точку флага: /event fp <ивент> <команда>");
            SendMessage(user, "/event kick - кикнуть игрока с ивента: /event kick <игрок>");
            SendMessage(user, "/event stop - закрыть телепорт на ивент.");
            SendMessage(user, "/event cancel - досрочно завершить ивент.");
        }
        
        private void SendMessage(NetUser user, string message) => rust.SendChatMessage(user, "Ultimate Events", "[COLOR # 00FA9A]" + message);
        private void NoticeAll(string message, float duration = 4F)
        { foreach (var playerClient in PlayerClient.All) rust.Notice(playerClient.netUser, message, "!", duration); }

        private void GetRandomPlayers()
        {
            var playerOne = TeleportedToEvent[Random.Range(0, TeleportedToEvent.Count)];
            if (playerOne == _previouslyWinner)
            {
                while (playerOne == _previouslyWinner)
                {
                    playerOne = TeleportedToEvent[Random.Range(0, TeleportedToEvent.Count)];
                }
            }
            var playerTwo = playerOne;

            while (playerTwo == playerOne || playerTwo == _previouslyWinner)
            {
                playerTwo = TeleportedToEvent[Random.Range(0, TeleportedToEvent.Count)];

                if (TeleportedToEvent.Count <= 2 && playerTwo != playerOne && playerTwo == _previouslyWinner) break;
            }

            _duelPlayers = new KeyValuePair<ulong, ulong>(playerOne, playerTwo);
        }
        private Vector3 ToVector3(string vector)
        {
            if (vector.StartsWith("(") && vector.EndsWith(")")) 
                vector = vector.Substring(1, vector.Length - 2);
            
            var sArray = vector.Split(',');
            return new Vector3(float.Parse(sArray[0]), float.Parse(sArray[1]), float.Parse(sArray[2]));
        }
    }
}