using RustExtended;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BSpawnSelector", "systemXcrackedZ", "2.3.1")]
    internal class BSpawnSelector : RustLegacyPlugin
    {
        private List<ulong> loadedPlayers = new List<ulong>();
        private void OnKilled(TakeDamage takeDamage, DamageEvent damage)
        {
            try
            {
                NetUser user = damage.victim.client?.netUser;
                if (user != null)
                {
                    SelectorVM selector = user.playerClient.gameObject.GetComponent<SelectorVM>();
                    selector.SendRPC("ClearCamps");
                    foreach (var item in Helper.GetPlayerSpawns(user))
                    {
                        selector.SendRPC("ReceiveCamp", item.ToString());
                    }
                    selector.SendRPC("OnDeath");
                }
            }
            catch { }
        }

        private void LoadPluginToPlayer(PlayerClient pClient)
        {
            if (pClient.gameObject.GetComponent<SelectorVM>() != null) return;
            if (loadedPlayers.Contains(pClient.userID)) loadedPlayers.Remove(pClient.userID);

            SelectorVM vm = pClient.gameObject.AddComponent<SelectorVM>();
            vm.playerClient = pClient;

            loadedPlayers.Add(pClient.userID);
        }
        private void UnloadPluginFromPlayer(GameObject gameObject, Type plugin)
        {
            if (gameObject.GetComponent(plugin) != null) UnityEngine.Object.Destroy(gameObject.GetComponent(plugin));
        }

        private void Loaded()
        {
            foreach (var pc in PlayerClient.All) if (pc != null && pc.netPlayer != null) LoadPluginToPlayer(pc);
        }
        private void Unload()
        {
            foreach (ulong loadedPlayer in loadedPlayers)
            {
                PlayerClient playerClient; PlayerClient.FindByUserID(loadedPlayer, out playerClient);
                if (playerClient != null)
                    UnloadPluginFromPlayer(playerClient.gameObject, typeof(SelectorVM));
            }
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

        public static Vector3 ToVector3(string vector)
        {
            if (vector.StartsWith("(") && vector.EndsWith(")"))
                vector = vector.Substring(1, vector.Length - 2);

            var sArray = vector.Split(',');
            return new Vector3(float.Parse(sArray[0]), float.Parse(sArray[1]), float.Parse(sArray[2]));
        }

        internal class SelectorVM : MonoBehaviour
        {
            public PlayerClient playerClient;

            [RPC]
            public void TryRespawn()
            {
                RustServerManagement.Get().SpawnPlayer(playerClient, false);
            }
            [RPC]
            public void TryRespawnCamp(string sVector3)
            {
                RustServerManagement RustManagement = RustServerManagement.Get();
                foreach (DeployableObject Obj in RustManagement.playerSpawns)
                {
                    if (Obj.ownerID != playerClient.userID) continue;
                    DeployedRespawn Spawn = Obj.GetComponent<DeployedRespawn>();
                    if (Spawn == null || !Spawn.IsValidToSpawn() || (Vector3.Distance(ToVector3(sVector3), Spawn.GetSpawnPos()) > 1.0f)) continue;

                    Spawn.MarkSpawnedOn();

                    NetUser user;
                    if (!NetUser.Find(playerClient, out user))
                    {
                        Debug.LogWarning("No NetUser for client", playerClient);
                    }

                    user.truthDetector.NoteTeleported(Spawn.GetSpawnPos());
                    Character character = Character.SummonCharacter(user.networkPlayer, ":player_soldier", Spawn.GetSpawnPos(), Spawn.GetSpawnRot());
                    if ((bool)character)
                    {
                        character.controller.GetComponent<AvatarSaveRestore>().LoadAvatar();
                        playerClient.lastKnownPosition = character.eyesOrigin;
                        playerClient.hasLastKnownPosition = true;
                        Hooks.ServerManagement_SpawnPlayer(playerClient, true);
                    }
                    break;
                }
            }
            public void SendRPC(string rpcName, params object[] param) => GetComponent<Facepunch.NetworkView>().RPC(rpcName, playerClient.netPlayer, param);
        }
    }
}
