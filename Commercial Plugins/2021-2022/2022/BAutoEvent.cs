using RageMods;
using Oxide.Core;
using UnityEngine;
using RustExtended;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("BAutoEvent", "systemXcrackedZ", "5.3.2")]
    internal class BAutoEvent : RustLegacyPlugin
    {
        #region [VARIABLES: Dictionary & KeyValuePair]
        private readonly Dictionary<string, int> IventItems = new Dictionary<string, int>
        {
            { "Cloth Helmet", 1 }, { "Cloth Vest", 1 }, { "Cloth Pants", 1 }, { "Cloth Boots", 1 }, // БРОНЯ
            { "Shotgun", 1 }, { "Shotgun Shells", 100 }, // ОРУЖИЕ
            { "Large Medkit", 20 } // ХИЛЛ
        };

        private readonly Dictionary<ulong, Inventory.Transfer[]> KeepInventory = new Dictionary<ulong, Inventory.Transfer[]>();
        private readonly Dictionary<ulong, Vector3> KeepPosition = new Dictionary<ulong, Vector3>();

        private KeyValuePair<ulong, ulong> duelPlayers = new KeyValuePair<ulong, ulong>();
        #endregion
        #region [VARIABLES: Players & Logic]
        private bool isEventStarted, clanFFireState;
        private readonly PlayerMods PlayerMods = new PlayerMods();
        private ulong winnerOne, winnerTwo, winnerThree, previouslyWinner;
        private bool IsOnDuel(ulong userID) => duelPlayers.Key == userID || duelPlayers.Value == userID;
        #endregion
        #region [VARIABLES: List]
        private readonly List<ulong> LoadedPlayers = new List<ulong>(), RegisteredPlayers = new List<ulong>(), WaitingRestore = new List<ulong>();
        #endregion

        #region [EVENT MANAGEMENT] -> Управление ивентами.
        public void StartEventTP()
        {
            AnnounceToAll("Внимание! Началась регистрация на ивент!");
            timer.Once(1.2f, () =>
            {
                AnnounceToAll("При телепорте сохраняется Ваша позиция и инвентарь!");
                timer.Once(1.2f, () => AnnounceToAll("Для тот, чтобы попасть на ивент - откройте инвентарь и нажмите на кнопку слева-посередине Вашего экрана."));
            });

            isEventStarted = true;
            PlaySoundToAll("HelloPidar");
            RPCToAll("OEventTPStart");
        }
        public void StopEventTP()
        {
            EventTeleporter eventTeleporter = new GameObject("EventTeleporter").AddComponent<EventTeleporter>();

            eventTeleporter.LocalUsers = new List<NetUser>();
            eventTeleporter.bAutoEvent = this;

            RPCToAll("OEventTPStop");

            for (int i = 0; i < RegisteredPlayers.Count; i++)
            {
                NetUser user = NetUser.FindByUserID(RegisteredPlayers[i]);

                eventTeleporter.LocalUsers.Add(user);
                SavePlayer(user);

                timer.Once(0.5f, () => { foreach (KeyValuePair<string, int> iventItem in IventItems) GiveItem(user, iventItem.Key, iventItem.Value); });
            }

            eventTeleporter.StartTP();
        }
        public void CancelEvent()
        {
            AnnounceToAll($"Ивент был успешно окончен!");
            AnnounceToAll($"1) \"{Users.GetBySteamID(winnerOne).Username}\", 2) \"{Users.GetBySteamID(winnerTwo).Username}\", 3) \"{Users.GetBySteamID(winnerThree).Username}\"");

            winnerOne = 0; winnerTwo = 0; winnerThree = 0;

            isEventStarted = false;
            clanFFireState = false;

            duelPlayers = new KeyValuePair<ulong, ulong>(0, 0);

            for (int i = 0; i < RegisteredPlayers.Count; i++) RestorePlayer(NetUser.FindByUserID(RegisteredPlayers[i]));
            RegisteredPlayers.Clear();

            RPCToAll("OEventTPStop");
        }
        #endregion
        #region [ADMINISTRATION] -> Администрирование.
        public void RegisterToEvent(NetUser user)
        {
            rust.Notice(user, "[Event]: Вы были успешно зарегистрированы на ивент! Ожидайте начала.");
            RegisteredPlayers.Add(user.userID);

            SendMessageToAll($"\"{Users.GetBySteamID(user.userID).Username}\" зарегистрировался на ивент! Участников: \"{RegisteredPlayers.Count}\".");
            SendMessageToAll("Чтобы попасть на ивент - откройте инвентарь...");
            SendMessageToAll("...и нажмите на кнопку слева-посередине Вашего экрана.");

            AutoEventVM autoEventVM = GetAutoEventVM(user.playerClient);
            autoEventVM.SendRPC("OEventTPStop");
        }
        public void KickFromEvent(string userName)
        {
            UserData userData = Users.Find(userName);
            NetUser user = NetUser.FindByUserID(userData.SteamID);

            if (IsOnDuel(user.userID))
            {
                GoToWaiting(NetUser.FindByUserID(duelPlayers.Key));
                GoToWaiting(NetUser.FindByUserID(duelPlayers.Value));
            }

            RegisteredPlayers.Remove(userData.SteamID);
            RestorePlayer(user);

            rust.Notice(user, "Вы были исключены с ивента!");
        }
        #endregion
        #region [MOVING] -> Перемещения.
        private void GoToDuel()
        {
            GamePoint gamePoint;
            List<GamePoint> gamePoints;
            NetUser userOne, userTwo;

            gamePoints = eventData.GamePoints;

            timer.Once(3f, () =>
            {
                userOne = NetUser.FindByUserID(duelPlayers.Key);
                userTwo = NetUser.FindByUserID(duelPlayers.Value);

                gamePoint = gamePoints[Core.Random.Range(0, gamePoints.Count)];
                Helper.TeleportTo(userOne, new Vector3(gamePoint.X, gamePoint.Y, gamePoint.Z));

                gamePoint = gamePoints.Find(f => f != gamePoint);
                Helper.TeleportTo(userTwo, new Vector3(gamePoint.X, gamePoint.Y, gamePoint.Z));

                SetPlayerState(NetUser.FindByUserID(duelPlayers.Key).playerClient);
                SetPlayerState(NetUser.FindByUserID(duelPlayers.Value).playerClient);

                AnnounceOnEvent($"\"{Users.GetBySteamID(duelPlayers.Key).Username}\" vs \"{Users.GetBySteamID(duelPlayers.Value).Username}\"");
            });
        }
        public void GoToWaiting(NetUser user)
        {
            if (user.playerClient == null || user.playerClient.netPlayer == null)
            {
                WaitingRestore.Add(user.userID);
                RegisteredPlayers.Remove(user.userID);
                return;
            }
            SpawnPoint spawnPoint;
            List<SpawnPoint> spawnPoints;

            spawnPoints = eventData.SpawnPoints;
            spawnPoint = spawnPoints[Core.Random.Range(0, spawnPoints.Count)];

            Helper.TeleportTo(user, new Vector3(spawnPoint.X, spawnPoint.Y, spawnPoint.Z));
        }
        #endregion

        #region [EVENT PLAYER ACTIONS]
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

        private void GiveItem(NetUser user, string item, int amount)
        {
            var preference = Inventory.Slot.Preference.Define(Inventory.Slot.Kind.Armor, false, Inventory.Slot.KindFlags.Belt);
            var inv = user.playerClient.rootControllable.idMain.GetComponent<Inventory>();
            inv.AddItemAmount(DatablockDictionary.GetByName(item), amount, preference);
        }
        #endregion
        #region [DUEL PLAYER ACTIONS]
        public void SetDuelPlayers()
        {
            ulong[] playersDuel = new ulong[2];
            List<ulong> validUsersOne, validUsersTwo;

            validUsersOne = RegisteredPlayers.FindAll(f => f != previouslyWinner);
            playersDuel[0] = validUsersOne[Core.Random.Range(0, validUsersOne.Count)];

            if (RegisteredPlayers.Count > 2)
                validUsersTwo = RegisteredPlayers.FindAll(f => f != playersDuel[0] && f != previouslyWinner);
            else
                validUsersTwo = RegisteredPlayers.FindAll(f => f != playersDuel[0]);

            playersDuel[1] = validUsersTwo[Core.Random.Range(0, validUsersTwo.Count)];

            var clanOne = Users.GetBySteamID(playersDuel[0]).Clan;
            if (clanOne != null)
            {
                var clanTwo = Users.GetBySteamID(playersDuel[1]).Clan;
                if (clanTwo != null && clanOne == clanTwo && clanOne.FrendlyFire) { clanFFireState = true; clanOne.FrendlyFire = false; }
            }

            duelPlayers = new KeyValuePair<ulong, ulong>(playersDuel[0], playersDuel[1]);
            GoToDuel();
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

            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Shotgun Shells"), 50);
            Helper.GiveItem(playerClient, DatablockDictionary.GetByName("Large Medkit"), 5);
        }
        #endregion
        
        #region [HOOK] Loaded()
        private void Loaded()
        {
            try { eventData = Interface.GetMod().DataFileSystem.ReadObject<EventData>("AutoEventData"); }
            catch { eventData = new EventData(); SaveData(); }
            for (int i = 0; i < PlayerClient.All.Count; i++) LoadVM(PlayerClient.All[i]);
        }
        #endregion
        #region [HOOK] Unload()
        private void Unload()
        {
            SaveData();
            for (int i = 0; i < PlayerClient.All.Count; i++)
                UnloadVM(PlayerClient.All[i]);
        }
        #endregion
        #region [HOOK] OnServerSave()
        private void OnServerSave() => SaveData();
        #endregion
        #region [HOOK] OnPlayerSpawn(PlayerClient playerClient)
        private void OnPlayerSpawn(PlayerClient playerClient)
        {
            if (WaitingRestore.Contains(playerClient.userID))
            { RestorePlayer(playerClient.netUser); WaitingRestore.Remove(playerClient.userID); }
        }
        #endregion
        #region [HOOK] OnKilled(TakeDamage takeDamage, DamageEvent damage)
        private void OnKilled(TakeDamage takeDamage, DamageEvent damage)
        {
            try
            {
                if (isEventStarted)
                {
                    NetUser attackerUser = damage.attacker.client.netUser; if (attackerUser == null) return;
                    NetUser victimUser = damage.victim.client.netUser; if (victimUser == null) return;

                    if (attackerUser.displayName == victimUser.displayName && takeDamage is HumanBodyTakeDamage)
                    {
                        WaitingRestore.Add(victimUser.userID);
                        RegisteredPlayers.Remove(victimUser.userID);
                        return;
                    }

                    if (IsOnDuel(attackerUser.userID) && IsOnDuel(victimUser.userID) && takeDamage is HumanBodyTakeDamage)
                    {
                        RemovePlayerSack(victimUser.userID);

                        ClanData clanAttacker = Users.GetBySteamID(attackerUser.userID).Clan;
                        if (clanAttacker != null)
                        {
                            ClanData clanVictim = Users.GetBySteamID(victimUser.userID).Clan;
                            if (clanVictim != null)
                                if (clanAttacker == clanVictim && !clanAttacker.FrendlyFire && clanFFireState)
                                { clanAttacker.FrendlyFire = true; clanFFireState = false; }
                        }

                        WaitingRestore.Add(victimUser.userID);
                        RegisteredPlayers.Remove(victimUser.userID);
                        previouslyWinner = attackerUser.userID;

                        if (RegisteredPlayers.Count == 2) winnerThree = victimUser.userID;

                        if (RegisteredPlayers.Count == 1)
                        {
                            winnerTwo = victimUser.userID;
                            winnerOne = attackerUser.userID;
                            CancelEvent();
                            return;
                        }

                        GoToWaiting(attackerUser);
                        AnnounceOnEvent($"\"{Users.GetBySteamID(attackerUser.userID).Username}\" Win!");

                        SetDuelPlayers();
                        rust.Notice(attackerUser, "Вы проходите в следующий этап!");
                    }
                }
            }
            catch
            {

            }
        }
        #endregion
        #region [HOOK] OnPlayerConnected(NetUser user)
        private void OnPlayerConnected(NetUser user)
        {
            PlayerClient playerClient = user.playerClient;
            if (playerClient != null)
                LoadVM(playerClient);
        }
        #endregion
        #region [HOOK] OnPlayerDisconnected(uLink.NetworkPlayer networkPlayer)
        private void OnPlayerDisconnected(uLink.NetworkPlayer networkPlayer)
        {
            foreach (ulong steamID in LoadedPlayers)
            {
                PlayerClient pClient;
                PlayerClient.FindByUserID(steamID, out pClient);
                if (pClient == null || pClient.netPlayer == networkPlayer)
                {
                    LoadedPlayers.Remove(steamID);

                    if (RegisteredPlayers.Contains(steamID))
                    {
                        if (!WaitingRestore.Contains(steamID)) WaitingRestore.Add(steamID);
                        RegisteredPlayers.Remove(steamID);
                    }

                    break;
                }
            }
        }
        #endregion

        private void OnPlayerInitialized(NetUser user, PlayerMods playerMods)
        {
            playerMods.UploadSound("HelloPidar", "http://rage.hostfun.top/project/darkrust/HELLOPIDAR.ogg");
        }

        #region [CMD] -> EMenu: OnCMD_EMenu(NetUser user, string cmd, string[] args)
        [ChatCommand("emenu")]
        private void OnCMD_EMenu(NetUser user, string cmd, string[] args)
        {
            if (user.CanAdmin() || Users.Find(user.userID).Rank > 99)
            {
                AutoEventVM autoEventVM = GetAutoEventVM(user.playerClient);

                autoEventVM.SendRPC("ClearSpawnPoints");
                autoEventVM.SendRPC("ClearGamePoints");
                autoEventVM.SendRPC("ClearPlayers");
                autoEventVM.SendRPC("CloseEMenu");

                for (int i = 0; i < RegisteredPlayers.Count; i++) autoEventVM.SendRPC("AddPlayer", Users.GetBySteamID(RegisteredPlayers[i]).Username);
                for (int i = 0; i < eventData.SpawnPoints.Count; i++) autoEventVM.SendRPC("AddSpawnPoint", new Vector3(eventData.SpawnPoints[i].X, eventData.SpawnPoints[i].Y, eventData.SpawnPoints[i].Z).ToString());
                for (int i = 0; i < eventData.GamePoints.Count; i++) autoEventVM.SendRPC("AddGamePoint", new Vector3(eventData.GamePoints[i].X, eventData.GamePoints[i].Y, eventData.GamePoints[i].Z).ToString());

                autoEventVM.SendRPC("SetEventState", isEventStarted);
                autoEventVM.SendRPC("OpenEMenu");
            }
        }
        [ChatCommand("emenuc")]
        private void OnCMD_EMenuC(NetUser user, string cmd, string[] args)
        {
            if (user.CanAdmin() || Users.Find(user.userID).Rank > 99)
            {
                AutoEventVM autoEventVM = GetAutoEventVM(user.playerClient);
                autoEventVM.SendRPC("CloseEMenu");
            }
        }
        #endregion

        #region [DATA] -> Save.
        private void SaveData() => Interface.GetMod().DataFileSystem.WriteObject("AutoEventData", eventData);
        #endregion
        #region [CONVERT] -> String To Vector3
        private Vector3 ToVector3(string vector)
        {
            if (vector.StartsWith("(") && vector.EndsWith(")"))
                vector = vector.Substring(1, vector.Length - 2);

            var sArray = vector.Split(',');
            return new Vector3(float.Parse(sArray[0]), float.Parse(sArray[1]), float.Parse(sArray[2]));
        }
        #endregion

        #region [POINT HOOKS] -> Point Management
        #region [POINT HOOK] CreateSpawnPoint(Vector3 point)
        public void CreateSpawnPoint(Vector3 point) => eventData.SpawnPoints.Add(new SpawnPoint(point.x, point.y, point.z));
        #endregion
        #region [POINT HOOK] CreateGamePoint(Vector3 point)
        public void CreateGamePoint(Vector3 point) => eventData.GamePoints.Add(new GamePoint(point.x, point.y, point.z));
        #endregion
        #region [POINT HOOK] RemoveSpawnPoint(SpawnPoint spawnPoint)
        public void RemoveSpawnPoint(SpawnPoint spawnPoint) => eventData.SpawnPoints.Remove(spawnPoint);
        #endregion
        #region [POINT HOOK] RemoveGamePoint(GamePoint gamePoint)
        public void RemoveGamePoint(GamePoint gamePoint) => eventData.GamePoints.Remove(gamePoint);
        #endregion
        #endregion

        #region [ANNOUNCES]
        private void PlaySoundToAll(string soundName)
        {
            for (int i = 0; i < PlayerClient.All.Count; i++)
            {
                //if (Users.Find(PlayerClient.All[i].userID).Rank > 99)
                {
                    PlayerMods.playerClient = PlayerClient.All[i];
                    //PlayerMods.PlaySound(soundName);
                }
            }
        }
        private void AnnounceOnEvent(string message)
        {
            PlayerClient playerClient;
            for (int i = 0; i < RegisteredPlayers.Count; i++)
            {
                PlayerClient.FindByUserID(RegisteredPlayers[i], out playerClient);
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
        #endregion
        #region [VM MANAGEMENT]
        private AutoEventVM GetAutoEventVM(PlayerClient playerClient) => playerClient.gameObject.GetComponent<AutoEventVM>();
        private void RPCToAll(string rpcName, params object[] args)
        {
            for (int i = 0; i < PlayerClient.All.Count; i++) //if (Users.Find(PlayerClient.All[i].userID).Rank > 99) 
                    GetAutoEventVM(PlayerClient.All[i]).SendRPC(rpcName, args);
        }

        private void LoadVM(PlayerClient playerClient)
        {
            if (playerClient != null && playerClient.netPlayer != null)
            {
                AutoEventVM raidBlockVM = GetAutoEventVM(playerClient);
                if (raidBlockVM == null)
                {
                    AutoEventVM playerVM = playerClient.gameObject.AddComponent<AutoEventVM>();
                    playerVM.localPlayer = playerClient;
                    playerVM.bAutoEvent = this;
                    LoadedPlayers.Add(playerClient.userID);
                }
            }
        }
        private void UnloadVM(PlayerClient playerClient)
        {
            if (playerClient != null && playerClient.netPlayer != null)
            {
                AutoEventVM raidBlockVM = GetAutoEventVM(playerClient);
                if (raidBlockVM != null)
                    UnityEngine.Object.Destroy(raidBlockVM);
            }
        }
        #endregion

        #region [CLASS: EventTeleporter]
        internal class EventTeleporter : MonoBehaviour
        {
            public List<NetUser> LocalUsers;
            public BAutoEvent bAutoEvent;

            public void StartTP() => StartCoroutine(Teleport());
            private IEnumerator Teleport()
            {
                for (int i = 0; i < LocalUsers.Count; i++)
                { bAutoEvent.GoToWaiting(LocalUsers[i]); yield return new WaitForSeconds(0.25f); }
                bAutoEvent.SetDuelPlayers();
            }
        }
        #endregion
        #region [CLASS: AutoEventVM]
        internal class AutoEventVM : MonoBehaviour
        {
            public PlayerClient localPlayer;
            public BAutoEvent bAutoEvent;

            [RPC]
            public void EventRegister() => bAutoEvent.RegisterToEvent(localPlayer.netUser);
            [RPC]
            public void KickFromEvent(string userName) => bAutoEvent.KickFromEvent(userName);

            [RPC]
            public void EventStartTP() => bAutoEvent.StartEventTP();
            [RPC]
            public void EventStopTP() => bAutoEvent.StopEventTP();
            [RPC]
            public void EventCancel() => bAutoEvent.CancelEvent();

            [RPC]
            public void CreateSpawnPoint() => bAutoEvent.CreateSpawnPoint(localPlayer.lastKnownPosition);
            [RPC]
            public void CreateGamePoint() => bAutoEvent.CreateGamePoint(localPlayer.lastKnownPosition);

            [RPC]
            public void RemoveSpawnPoint(string sVector3) => bAutoEvent.RemoveSpawnPoint(bAutoEvent.eventData.SpawnPoints.Find(f => Vector3.Distance(bAutoEvent.ToVector3(sVector3), new Vector3(f.X, f.Y, f.Z)) < 1f));
            [RPC]
            public void RemoveGamePoint(string sVector3) => bAutoEvent.RemoveGamePoint(bAutoEvent.eventData.GamePoints.Find(f => Vector3.Distance(bAutoEvent.ToVector3(sVector3), new Vector3(f.X, f.Y, f.Z)) < 1f));

            public void SendRPC(string rpcName, params object[] args) => localPlayer.networkView.RPC(rpcName, localPlayer.netPlayer, args);
        }
        #endregion
        #region [CLASS: Plugin Data]
        public class SpawnPoint
        {
            public SpawnPoint(float x, float y, float z) { X = x; Y = y; Z = z; }
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
        }
        public class GamePoint
        {
            public GamePoint(float x, float y, float z) { X = x; Y = y; Z = z; }
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
        }

        public class EventData
        {
            [JsonProperty("Точки Спавна")]
            public List<SpawnPoint> SpawnPoints = new List<SpawnPoint>();
            [JsonProperty("Точки Игры")]
            public List<GamePoint> GamePoints = new List<GamePoint>();
        }
        private EventData eventData;
        #endregion
    }
}
