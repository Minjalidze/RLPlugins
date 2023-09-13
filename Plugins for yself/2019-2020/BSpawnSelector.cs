using RustExtended;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BSpawnSelector", "systemXcrackedZ", "4.3.1")]
    internal class BSpawnSelector : RustLegacyPlugin
    {
        private readonly List<ulong> _loadedPlayers = new List<ulong>();

        public class PlayerData
        {
            [JsonProperty("User ID")] public ulong SteamID { get; set; }
            [JsonProperty("Spawns")] public Dictionary<string, string> Spawns { get; set; }
            [JsonProperty("UseMap")] public bool UseMap { get; set; }
        }
        public static List<PlayerData> Players;

        public static PlayerData GetPlayerData(ulong steamID)
        { 
            var user = Players.Find(f => f.SteamID == steamID);
            if (user == null)
                Players.Add(new PlayerData
                {
                    Spawns = new Dictionary<string, string>(),
                    SteamID = steamID,
                    UseMap = true
                });
            user = Players.Find(f => f.SteamID == steamID);
            return user;
        }

        private void OnKilled(TakeDamage takeDamage, DamageEvent damage)
        {
            try
            {
                var user = damage.victim.client?.netUser;
                if (user == null) return;

                var data = GetPlayerData(user.userID);

                var selector = user.playerClient.gameObject.GetComponent<SelectorVM>();
                selector.SendRPC("ClearCamps");

                if (data.UseMap)
                {
                    foreach (var item in Helper.GetPlayerSpawns(user)) 
                        selector.SendRPC("ReceiveCamp", item.ToString());
                }
                else
                {
                    foreach (var spawn in from spawn in data.Spawns
                             let rustManagement = RustServerManagement.Get()
                             from deployedSpawn in from obj in rustManagement.playerSpawns
                                 where obj.ownerID == user.userID
                                 select obj.GetComponent<DeployedRespawn>()
                                 into deployedSpawn
                                 where deployedSpawn != null && deployedSpawn.IsValidToSpawn() &&
                                       !(Vector3.Distance(ToVector3(spawn.Value), deployedSpawn.GetSpawnPos()) > 1.0f)
                                 select deployedSpawn
                             select spawn) selector.SendRPC("ReceiveSpawn", spawn.Key);
                }

                selector.SendRPC("OnDeath", data.UseMap);
            }
            catch
            {
                // ignored
            }
        }

        private void LoadPluginToPlayer(PlayerClient pClient)
        {
            if (pClient.gameObject.GetComponent<SelectorVM>() != null) return;
            if (_loadedPlayers.Contains(pClient.userID)) _loadedPlayers.Remove(pClient.userID);

            SelectorVM vm = pClient.gameObject.AddComponent<SelectorVM>();
            vm.PlayerClient = pClient;

            _loadedPlayers.Add(pClient.userID);
        }
        private static void UnloadPluginFromPlayer(GameObject gameObject, Type plugin)
        {
            if (gameObject.GetComponent(plugin) != null) UnityEngine.Object.Destroy(gameObject.GetComponent(plugin));
        }

        [ChatCommand("savespawn")]
        private void CMD_SaveSpawn(NetUser user, string cmd, string[] args)
        {
            if (args.Length == 0)
            {
                rust.SendChatMessage(user, "SpawnSelector", "Вы можете указать только свои спальники/кровати!");
                rust.SendChatMessage(user, "SpawnSelector", "Вы можете запомнить спальник/кровать по её названию для респавна.");
                rust.SendChatMessage(user, "SpawnSelector", "Использование команды: /savespawn <название>");
                return;
            }

            var spawnName = args[0].ToString();

            const float distance = 10.0f;

            var lookObject = Helper.GetLookObject(Helper.GetLookRay(user), distance);
            if (lookObject == null) return;

            var obj = lookObject.GetComponent<DeployableObject>();
            if (obj == null) return;

            if (obj.name.ToLower().Contains("sleeping") || obj.name.ToLower() == "bed")
            {
                if (obj.ownerID != user.userID)
                {
                    rust.SendChatMessage(user, "SpawnSelector", "Вы можете указать только свои спальники/кровати!");
                    rust.SendChatMessage(user, "SpawnSelector", "Вы можете запомнить спальник/кровать по её названию для респавна.");
                    rust.SendChatMessage(user, "SpawnSelector", "Использование команды: /savespawn <название>");
                    return;
                }
                if (GetPlayerData(user.userID).Spawns.ContainsKey(spawnName))
                {
                    rust.SendChatMessage(user, "SpawnSelector", "Спальник с таким названием уже запомнили!");
                    rust.SendChatMessage(user, "SpawnSelector", "Вы можете указать только свои спальники/кровати!");
                    rust.SendChatMessage(user, "SpawnSelector", "Вы можете запомнить спальник/кровать по её названию для респавна.");
                    rust.SendChatMessage(user, "SpawnSelector", "Использование команды: /savespawn <название>");
                    return;
                }
                if (GetPlayerData(user.userID).Spawns.ContainsValue(lookObject.transform.position.ToString()))
                {
                    rust.SendChatMessage(user, "SpawnSelector", "Спальник на который вы смотрите уже запомнили!");
                    rust.SendChatMessage(user, "SpawnSelector", "Вы можете указать только свои спальники/кровати!");
                    rust.SendChatMessage(user, "SpawnSelector", "Вы можете запомнить спальник/кровать по её названию для респавна.");
                    rust.SendChatMessage(user, "SpawnSelector", "Использование команды: /savespawn <название>");
                    return;
                }

                rust.SendChatMessage(user, "SpawnSelector", $"Вы успешно сохранили спальник/кровать для респавна \"{spawnName}\"!");
                GetPlayerData(user.userID).Spawns.Add(spawnName, lookObject.transform.position.ToString());
                SaveData();
                return;
            }
            rust.SendChatMessage(user, "SpawnSelector", "Вы можете указать только свои спальники/кровати!");
            rust.SendChatMessage(user, "SpawnSelector", "Вы можете запомнить спальник/кровать по её названию для респавна.");
            rust.SendChatMessage(user, "SpawnSelector", "Использование команды: /savespawn <название>");
        }

        private void Loaded()
        {
            Players = Interface.GetMod().DataFileSystem
                .ReadObject<List<PlayerData>>("BSpawnSelectorData");

            foreach (var pc in PlayerClient.All.Where(pc => pc != null && pc.netPlayer != null)) LoadPluginToPlayer(pc);
        }
        private void Unload()
        {
            SaveData();
            foreach (var loadedPlayer in _loadedPlayers)
            {
                PlayerClient playerClient; 
                PlayerClient.FindByUserID(loadedPlayer, out playerClient);
                if (playerClient != null) UnloadPluginFromPlayer(playerClient.gameObject, typeof(SelectorVM));
            }
        }
        private void SaveData() =>
            Interface.Oxide.DataFileSystem.WriteObject("BSpawnSelectorData", Players);
        private void OnServerSave() =>
            SaveData();

        private void OnPlayerConnected(NetUser user)
        {
            if (user.playerClient != null)
                LoadPluginToPlayer(user.playerClient);
        }
        private void OnPlayerDisconnected(uLink.NetworkPlayer networkPlayer)
        {
            NetUser user = NetUser.Find(networkPlayer);
            if (user != null) _loadedPlayers.Remove(user.userID);
        }

        public static Vector3 ToVector3(string vector)
        {
            if (vector.StartsWith("(") && vector.EndsWith(")"))
                vector = vector.Substring(1, vector.Length - 2);

            var sArray = vector.Split(',');
            return new Vector3(float.Parse(sArray[0]), float.Parse(sArray[1]), float.Parse(sArray[2]));
        }

        internal class SelectorVM : MonoBehaviour
        {
            public PlayerClient PlayerClient;

            [RPC]
            public void ChangeUseMap(bool newUseMap)
            {
                GetPlayerData(PlayerClient.userID).UseMap = newUseMap;
            }

            [RPC]
            public void TrySpawnScreen(string spawnName)
            {
                var data = GetPlayerData(PlayerClient.userID);
                var rustManagement = RustServerManagement.Get();
                foreach (var spawn in from obj in rustManagement.playerSpawns where obj.ownerID == PlayerClient.userID select obj.GetComponent<DeployedRespawn>() into spawn where spawn != null && spawn.IsValidToSpawn() && !(Vector3.Distance(ToVector3(data.Spawns[spawnName]), spawn.GetSpawnPos()) > 1.0f) select spawn)
                {
                    spawn.MarkSpawnedOn();

                    NetUser user;
                    if (!NetUser.Find(PlayerClient, out user))
                    {
                        Debug.LogWarning("No NetUser for client", PlayerClient);
                    }

                    user.truthDetector.NoteTeleported(spawn.GetSpawnPos());
                    var character = Character.SummonCharacter(user.networkPlayer, ":player_soldier", spawn.GetSpawnPos(), spawn.GetSpawnRot());
                    if ((bool)character)
                    {
                        character.controller.GetComponent<AvatarSaveRestore>().LoadAvatar();
                        PlayerClient.lastKnownPosition = character.eyesOrigin;
                        PlayerClient.hasLastKnownPosition = true;
                        Hooks.ServerManagement_SpawnPlayer(PlayerClient, true);
                    }
                    break;
                }
            }
            [RPC]
            public void TrySpawn()
            {
                RustServerManagement.Get().SpawnPlayer(PlayerClient, false);
            }
            [RPC]
            public void TrySpawnCamp(string sVector3)
            {
                var rustManagement = RustServerManagement.Get();
                foreach (var spawn in from obj in rustManagement.playerSpawns where obj.ownerID == PlayerClient.userID select obj.GetComponent<DeployedRespawn>() into spawn where spawn != null && spawn.IsValidToSpawn() && !(Vector3.Distance(ToVector3(sVector3), spawn.GetSpawnPos()) > 1.0f) select spawn)
                {
                    spawn.MarkSpawnedOn();

                    NetUser user;
                    if (!NetUser.Find(PlayerClient, out user))
                    {
                        Debug.LogWarning("No NetUser for client", PlayerClient);
                    }

                    user.truthDetector.NoteTeleported(spawn.GetSpawnPos());
                    var character = Character.SummonCharacter(user.networkPlayer, ":player_soldier", spawn.GetSpawnPos(), spawn.GetSpawnRot());
                    if ((bool)character)
                    {
                        character.controller.GetComponent<AvatarSaveRestore>().LoadAvatar();
                        PlayerClient.lastKnownPosition = character.eyesOrigin;
                        PlayerClient.hasLastKnownPosition = true;
                        Hooks.ServerManagement_SpawnPlayer(PlayerClient, true);
                    }
                    break;
                }
            }

            public void SendRPC(string rpcName, params object[] param) => GetComponent<Facepunch.NetworkView>().RPC(rpcName, PlayerClient.netPlayer, param);
        }
    }
}
