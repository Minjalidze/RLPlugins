using UnityEngine;
using RustExtended;

using System;
using System.Linq;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("BMainMod", "systemXcrackedZ", "4.3.2")]
    [Description("Main Mod for Rust Legacy server: Bless Rust.")]
    internal class BMainMod : RustLegacyPlugin
    {
        #region [LIST]: LoadedPlayers -> [Список]: Загруженные игроки
        private static readonly List<ulong> LoadedPlayers = new List<ulong>();
        #endregion

        #region [VOID]: LoadPluginToPlayer(PlayerClient pClient) -> [Метод]: Загрузка плагина игроку
        private void LoadPluginToPlayer(PlayerClient pClient)
        {
            if (pClient.gameObject.GetComponent<ModVM>() != null) return;
            if (LoadedPlayers.Contains(pClient.userID)) LoadedPlayers.Remove(pClient.userID);

            ModVM modVM = pClient.gameObject.AddComponent<ModVM>();
            modVM.playerClient = pClient;
            modVM.mainMod = this;

            LoadedPlayers.Add(pClient.userID);
        }
        #endregion
        #region [HOOK]: OnPlayerConnected(NetUser user) -> [Хук]: При заходе игрока.
        void OnPlayerConnected(NetUser user)
        {
            if (user.playerClient != null) LoadPluginToPlayer(user.playerClient);
        }
        #endregion
        #region [HOOK]: Loaded() -> [Хук]: Загружено
        private void Loaded()
        {
            foreach (PlayerClient pClients in PlayerClient.All.Where(pClients => pClients != null && pClients.netPlayer != null)) LoadPluginToPlayer(pClients);
        }
        #endregion

        #region [VOID]: UnloadPluginFromPlayer(GameObject gameObject, Type plugin) -> [Метод]: Выгрузка плагина от игрока
        private void UnloadPluginFromPlayer(GameObject gameObject, Type plugin)
        {
            if (gameObject.GetComponent(plugin) != null) UnityEngine.Object.Destroy(gameObject.GetComponent(plugin));
        }
        #endregion
        #region [HOOK]: OnPlayerDisconnected(uLink.NetworkPlayer networkPlayer) -> [Хук]: При выходе игрока
        private void OnPlayerDisconnected(uLink.NetworkPlayer networkPlayer)
        {
            for (int i = 0; i < LoadedPlayers.Count; i++)
            {
                PlayerClient _playerClient;
                PlayerClient.FindByUserID(LoadedPlayers[i], out _playerClient);

                if (_playerClient == null || _playerClient.netPlayer == networkPlayer)
                {
                    LoadedPlayers.Remove(LoadedPlayers[i]);
                    break;
                }
            }
        }
        #endregion
        #region [HOOK]: Unload() -> [Хук]: Выгружено
        private void Unload()
        {
            for (int i = 0; i < LoadedPlayers.Count; i++)
            {
                PlayerClient pclient;
                PlayerClient.FindByUserID(LoadedPlayers[i], out pclient);

                if (pclient != null) UnloadPluginFromPlayer(pclient.gameObject, typeof(ModVM));
            }
        }
        #endregion

        internal class ModVM : MonoBehaviour
        {
            private Facepunch.NetworkView _networkView = null;

            public PlayerClient playerClient;
            public BMainMod mainMod; 

            private void Awake() => _networkView = GetComponent<Facepunch.NetworkView>();

            [RPC]
            public void GetAdminStatus() {
                if (NetUser.FindByUserID(playerClient.userID).CanAdmin()) SendRPC("SetAdminStatus", playerClient);
            }
            [RPC]
            public void GetPlayers() {
                for (int i = 0; i < PlayerClient.All.Count; i++) SendRPC("SendPlayer", playerClient, Users.Find(PlayerClient.All[i].userID).Username);
            }

            [RPC]
            public void GetWarps()
            {
                SendRPC("SendAvailableWarps", playerClient, "small");
                SendRPC("SendAvailableWarps", playerClient, "hangar");
                SendRPC("SendAvailableWarps", playerClient, "big");
                SendRPC("SendAvailableWarps", playerClient, "factory");
                SendRPC("SendAvailableWarps", playerClient, "bochki");
                SendRPC("SendAvailableWarps", playerClient, "hackerka");
                SendRPC("SendAvailableWarps", playerClient, "medvega");
            }
            [RPC]
            public void GetKits()
            {
                UserData userData = Users.Find(playerClient.userID);
                if (userData == null) return;

                int rank;

                foreach (object kit in RustExtended.Core.Kits.Keys)
                {
                    List<string> kitList = RustExtended.Core.Kits[kit] as List<string>;

                    string kitRank = kitList.Find(k => k.ToLower().StartsWith("rank"));
                    bool isKitAvailable = string.IsNullOrEmpty(kitRank) || !kitRank.Contains("=");

                    if (!isKitAvailable) { kitRank = kitRank.Split('=')[1].Trim(); isKitAvailable = string.IsNullOrEmpty(kitRank); }
                    if (!isKitAvailable) foreach (string kRank in kitRank.Split(',')) if (isKitAvailable = int.TryParse(kRank, out rank) && rank == userData.Rank) break;

                    if (isKitAvailable) SendRPC("SendAvailableKits", playerClient, kit);
                }
            }
            [RPC]
            public void GetQuests()
            {
                for (int i = 0; i < 10; i++)
                {
                    SendRPC("AddQuest", playerClient, i);
                    for (int t = 0; t < 4; t++)
                        SendRPC("AddQuestArg", playerClient, i, $"ТЕСТОВАЯ НАДПИСЬ #{t}");
                }
            }

            private void SendRPC(string RPCName, PlayerClient player, params object[] param) => _networkView.RPC(RPCName, player.netPlayer, param);
        }
    }
}
