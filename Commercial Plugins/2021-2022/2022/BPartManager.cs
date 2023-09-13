using RustExtended;
using UnityEngine;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("BPartManager", "systemXcrackedZ", "2.1.4")]
    internal class BPartManager : RustLegacyPlugin
    {
        public List<ulong> loadedPlayers = new List<ulong>();

        private void LoadVM(PlayerClient playerClient)
        {
            if (playerClient != null && playerClient.netPlayer != null)
            {
                var partManagerVM = playerClient.gameObject.GetComponent<PartManagerVM>();
                if (partManagerVM == null)
                {
                    var playerVM = playerClient.gameObject.AddComponent<PartManagerVM>();
                    playerVM.playerClient = playerClient;
                    playerVM.bPartManager = this;
                    loadedPlayers.Add(playerClient.userID);
                }
            }
        }
        private void UnloadVM(PlayerClient playerClient)
        {
            if (playerClient != null && playerClient.netPlayer != null)
            {
                var partManagerVM = playerClient.gameObject.GetComponent<PartManagerVM>();
                if (partManagerVM != null)
                {
                    UnityEngine.Object.Destroy(partManagerVM);
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

        internal class PartManagerVM : MonoBehaviour
        {
            public BPartManager bPartManager;
            public PlayerClient playerClient;

            [RPC]
            public void SpawnPart(string partName)
            {
                Inventory inventory = playerClient.controllable.GetComponent<Inventory>();
                Helper.InventoryItemRemove(inventory, DatablockDictionary.GetByName(partName), 1);

                if (partName.ToLower().Contains("weapon"))
                {
                    Vector3 position = playerClient.lastKnownPosition - new Vector3(0, 1f, 0);
                    Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

                    NetCull.InstantiateStatic("WeaponLootBox", position, rotation);
                }
                if (partName.ToLower().Contains("armor"))
                {
                    Vector3 position = playerClient.lastKnownPosition - new Vector3(0, 1f, 0);
                    Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

                    NetCull.InstantiateStatic("AmmoLootBox", position, rotation);
                }

                SendRPC("SpawnCancelled");
            }

            public void SendRPC(string rpcName, params object[] args)
            {
                playerClient.gameObject.GetComponent<Facepunch.NetworkView>().RPC(rpcName, playerClient.netPlayer, args);
            }
        }
    }
}
