using RustExtended;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BRanksSelect", "systemXcrackedZ", "2.1.17")]
    internal class BRanksSelect : RustLegacyPlugin
    {
        public List<ulong> loadedPlayers = new List<ulong>();

        private void LoadVM(PlayerClient playerClient)
        {
            if (playerClient != null && playerClient.netPlayer != null)
            {
                var CustomObjectsVM = playerClient.gameObject.GetComponent<RanksSelectVM>();
                if (CustomObjectsVM == null)
                {
                    var playerVM = playerClient.gameObject.AddComponent<RanksSelectVM>();
                    playerVM.playerClient = playerClient;
                    playerVM.bRanksSelect = this;
                    loadedPlayers.Add(playerClient.userID);
                }
            }
        }
        private void UnloadVM(PlayerClient playerClient)
        {
            if (playerClient != null && playerClient.netPlayer != null)
            {
                var CustomObjectsVM = playerClient.gameObject.GetComponent<RanksSelectVM>();
                if (CustomObjectsVM != null)
                {
                    UnityEngine.Object.Destroy(CustomObjectsVM);
                }
            }
        }

        private void OnPlayerConnected(NetUser user)
        {
            var playerClient = user.playerClient;
            if (playerClient != null)
            {
                LoadVM(playerClient);
            }
        }
        private void OnPlayerDisconnected(uLink.NetworkPlayer player)
        {
            var user = NetUser.Find(player);
            if (user != null)
                loadedPlayers.Remove(user.userID);
        }

        private void Loaded()
        {
            foreach (var playerClient in PlayerClient.All)
            {
                LoadVM(playerClient);
            }
        }
        private void Unload()
        {
            foreach (var loadedPlayer in loadedPlayers)
            {
                UnloadVM(Helper.GetPlayerClient(loadedPlayer));
            }
        }

        internal class RanksSelectVM : MonoBehaviour
        {
            public BRanksSelect bRanksSelect;
            public PlayerClient playerClient;

            [RPC]
            public void GetSelectState()
            {
                UserData userData = Users.GetBySteamID(playerClient.userID);
                SendRPC("SetSelectState", userData.Rank);
            }

            [RPC]
            public void SetRank(int rank)
            {
                UserData userData = Users.GetBySteamID(playerClient.userID);
                userData.Rank = rank;
                var message = $"Вы выбрали ранг {RustExtended.Core.Ranks[rank]}!";
                ConsoleNetworker.SendClientCommand(playerClient.netPlayer, $"chat.add RANK {message.Quote()}");
            }

            public void SendRPC(string rpcName, params object[] args)
            {
                playerClient.gameObject.GetComponent<Facepunch.NetworkView>().RPC(rpcName, playerClient.netPlayer, args);
            }
        }
    }
}
