using Oxide.Core;
using UnityEngine;
using RustExtended;

using System.Reflection;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("BLootableManager", "systemXcrackedZ", "2.3.2")]
    internal class BLootableManager : RustLegacyPlugin
    {
        public class OnlyResourcePosition
        {
            public OnlyResourcePosition(float x, float y, float z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
        }
        public List<OnlyResourcePosition> onlyResourcePositions;

        private FieldInfo lootablePlayer;
        private List<ulong> loadedPlayers = new List<ulong>();

        private static readonly List<string> AllowedResources = new List<string>
        {
            "Metal Ore", "Sulfur Ore", "Stones", "Metal Fragments", "Sulfur", "Wood", "Cloth", "Leather",
            "Animal Fat", "Low Grade Fuel", "Charcoal", "Explosives", "Paper", "Low Quality Metal", "Gunpowder"
        };

        private void OnItemDeployed(DeployableObject component, IDeployableItem item)
        {
            if (component.gameObject.GetComponent<LootableObject>() == null || !item.datablock.name.ToLower().Contains("storage")) return;

            LootableManagerVM lootableManagerVM = item.character.netUser.playerClient.gameObject.GetComponent<LootableManagerVM>();
            if (lootableManagerVM != null)
            {
                Vector3 lootablePosition = component.gameObject.transform.position;
                lootableManagerVM.SendRPC("OResourceM", lootablePosition.x, lootablePosition.y, lootablePosition.z);
            }
        }
        private void OnItemAdded(Inventory inventory, int slot, IInventoryItem item)
        {
            LootableObject lootable = inventory.GetLocal<LootableObject>();
            if (lootable == null || !IsOnlyResourceLoot(lootable)) return;

            if (!AllowedResources.Contains(item.datablock.name) && (uLink.NetworkPlayer)lootablePlayer.GetValue(lootable) != uLink.NetworkPlayer.unassigned)
            {
                NetUser netUser = (NetUser)((uLink.NetworkPlayer)lootablePlayer.GetValue(lootable)).GetLocalData();
                Inventory playerInventory = netUser.playerClient.controllable.GetComponent<Character>().GetComponent<Inventory>();

                inventory.MoveItemAtSlotToEmptySlot(playerInventory, slot, GetEmptySlot(playerInventory));

                rust.Notice(netUser, "В этот ящик разрешено класть только ресурсы!");
            }
        }

        private void LoadVM(PlayerClient playerClient)
        {
            if (playerClient != null && playerClient.netPlayer != null)
            {
                LootableManagerVM lootableManagerVM = playerClient.gameObject.GetComponent<LootableManagerVM>();
                if (lootableManagerVM == null)
                {
                    LootableManagerVM playerVM = playerClient.gameObject.AddComponent<LootableManagerVM>();
                    playerVM.playerClient = playerClient;
                    playerVM.bLootableManager = this;
                    loadedPlayers.Add(playerClient.userID);
                }
            }
        }
        private void UnloadVM(PlayerClient playerClient)
        {
            if (playerClient != null && playerClient.netPlayer != null)
            {
                LootableManagerVM lootableManagerVM = playerClient.gameObject.GetComponent<LootableManagerVM>();
                if (lootableManagerVM != null)
                    UnityEngine.Object.Destroy(lootableManagerVM);
            }
        }

        private void OnPlayerConnected(NetUser user)
        {
            PlayerClient playerClient = user.playerClient;
            if (playerClient != null)
                LoadVM(playerClient);
        }
        private void OnPlayerDisconnected(uLink.NetworkPlayer player)
        {
            NetUser user = NetUser.Find(player);
            if (user != null)
                loadedPlayers.Remove(user.userID);
        }

        private void Loaded()
        {
            lootablePlayer = typeof(LootableObject).GetField("_currentlyUsingPlayer", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));

            try
            { LoadData(); }
            catch
            { onlyResourcePositions = new List<OnlyResourcePosition>(); }

            foreach (var playerClient in PlayerClient.All)
                LoadVM(playerClient);
        }
        private void Unload()
        {
            SaveData();
            foreach (ulong loadedPlayer in loadedPlayers)
                UnloadVM(Helper.GetPlayerClient(loadedPlayer));
        }
        private void OnServerSave() => SaveData();

        private void LoadData() => onlyResourcePositions = Interface.GetMod().DataFileSystem.ReadObject<List<OnlyResourcePosition>>("OnlyResourcePositionsData");
        private void SaveData() => Interface.GetMod().DataFileSystem.WriteObject("OnlyResourcePositionsData", onlyResourcePositions);

        private void OnKilled(TakeDamage takeDamage, DamageEvent damage)
        {
            if (damage.amount < takeDamage.health) return;

            NetUser attacker = damage.attacker.client?.netUser ?? null;
            if (attacker == null) return;

            LootableObject victim = damage.victim.idMain.GetLocal<LootableObject>();
            if (victim != null && victim.gameObject.name.ToLower().Contains("woodbox"))
            {
                Vector3 position = victim.gameObject.transform.position;

                OnlyResourcePosition onlyResourcePosition = onlyResourcePositions.Find(f => f.X == position.x && f.Y == position.y && f.Z == position.z);
                if (onlyResourcePosition != null)
                    onlyResourcePositions.Remove(onlyResourcePosition);
                SaveData();
            }
        }

        private int GetEmptySlot(Inventory inventory)
        {
            for (int i = 0; i < inventory.slotCount; i++)
                if (inventory.IsSlotVacant(i))
                    return i;

            return -1;
        }
        public void SetOnlyResource(float x, float y, float z)
        {
            Vector3 position = new Vector3(x, y, z);
            foreach (Collider collider in Physics.OverlapSphere(position, 1.5f))
            {
                if (collider.gameObject.GetComponent<LootableObject>() != null)
                {
                    onlyResourcePositions.Add(new OnlyResourcePosition(x, y, z));
                    SaveData();
                }
            }
        }
        private bool IsOnlyResourceLoot(LootableObject lootableObject)
        {
            foreach (OnlyResourcePosition onlyResourcePosition in onlyResourcePositions)
                if (Vector3.Distance(lootableObject.transform.position, PositionToVector3(onlyResourcePosition)) < 1.5f)
                    return true;

            return false;
        }

        private Vector3 PositionToVector3(OnlyResourcePosition onlyResourcePosition) => new Vector3(onlyResourcePosition.X, onlyResourcePosition.Y, onlyResourcePosition.Z);

        internal class LootableManagerVM : MonoBehaviour
        {
            public PlayerClient playerClient;
            public BLootableManager bLootableManager;

            [RPC]
            public void SetOnlyResource(float x, float y, float z)
            {
                bLootableManager.SetOnlyResource(x, y, z);
            }
            [RPC]
            public void GOnlyLoot()
            {
                foreach (OnlyResourcePosition onlyResourcePosition in bLootableManager.onlyResourcePositions)
                    SendRPC("SOnlyLoot", onlyResourcePosition.X, onlyResourcePosition.Y, onlyResourcePosition.Z);
            }

            public void SendRPC(string rpcName, params object[] args) => playerClient.gameObject.GetComponent<Facepunch.NetworkView>().RPC(rpcName, playerClient.netPlayer, args);
        }
    }
}
