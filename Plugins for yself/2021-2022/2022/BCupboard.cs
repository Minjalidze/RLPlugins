using UnityEngine;

using RustExtended;
using RageMods;

using System;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;

#pragma warning disable IDE0051

namespace Oxide.Plugins
{
    [Info("BCupboard", "systemXcrackedZ", "4.1.2")]
    internal class BCupboard : RustLegacyPlugin
    {
        #region [Variable] List<ulong> loadedPlayers
        private List<ulong> loadedPlayers = new List<ulong>();
        #endregion

        #region [Hook] OnUserCommand(IPlayer player, string command, string[] args)
        private object OnUserCommand(IPlayer player, string command, string[] args)
        {
            ulong userID = ulong.Parse(player.Id);
            NetUser netUser = NetUser.FindByUserID(userID);

            if (command == "transfer")
            {
                float distance = 10f;

                GameObject GO = Helper.GetLookObject(Helper.GetLookRay(netUser), distance);
                DeployableObject DO = null;

                if (GO != null)
                    DO = GO.GetComponent<DeployableObject>();

                if (DO != null && DO.gameObject.name.ToLower().Contains("repairbench") && !netUser.admin)
                {
                    rust.Notice(netUser, "Вы не можете передать владение шкафом другому игроку!");
                    return false;
                }

                Commands.Transfer(netUser, Users.GetBySteamID(userID), command, args);
                return false;
            }
            return null;
        }
        #endregion
        #region [Hook] OnItemDeployed(DeployableObject component, IDeployableItem item)
        private void OnItemDeployed(DeployableObject component, IDeployableItem item)
        {
            NetUser user = item.character.netUser;
            if (user == null)
                return;

            if (item.datablock.name == "Repair Bench")
            {
                double fixDistance = Math.Floor(Vector3.Distance(component.transform.position, user.playerClient.lastKnownPosition));
                if (fixDistance <= 1)
                {
                    ReturnDeploy(component, item);
                    Notice(user, $"Запрещено ставить шкаф под себя!");
                    return;
                }

                Vector3 lastPosition = component.transform.position;

                DeployableObject deployableObject;
                if (IsCupboardNearZone(component, out deployableObject))
                {
                    ReturnDeploy(component, item);
                    Notice(user, $"Нельзя поставить шкаф в зоне шкафа игрока \"{Users.GetBySteamID(deployableObject.ownerID).Username}\"!");
                    return;
                }

                StructureComponent structureObject;
                if (IsCupboardNearStruct(lastPosition, out structureObject))
                {
                    Notice(user, "Шкаф установлен. Теперь рядом нельзя построиться в радиусе 35 метров.");
                    Notice(user, "Для того, чтобы рядом могли строиться ваши друзья - дайте им доступ через /share \"ник\".");

                    if (structureObject._master.ownerID != component.ownerID)
                    {
                        structureObject._master.creatorID = structureObject._master.ownerID = user.userID;
                        structureObject._master.CacheCreator();
                    }
                    return;
                }
                ReturnDeploy(component, item);
                Notice(user, "Нельзя поставить шкаф вне постройки!");
                return;
            }
            else
            {
                DeployableObject deployableObject;
                if (IsCupboardNearDeploy(component.transform.position, out deployableObject)
                    && !Users.SharedList(deployableObject.ownerID).Contains(user.userID)
                    && user.userID != deployableObject.ownerID
                    && item.datablock.name != "Explosive Charge" && !item.datablock.name.ToLower().Contains("barricade") && !user.admin)
                {
                    ReturnDeploy(component, item);
                    Notice(user, $"Запрещено строиться рядом со шкафом игрока \"{Users.GetBySteamID(deployableObject.ownerID).Username}\"!");
                }
            }
        }
        #endregion
        #region [Hook] OnStructureBuilt(StructureComponent component, IStructureComponentItem item)
        private void OnStructureBuilt(StructureComponent component, IStructureComponentItem item)
        {
            NetUser user = item.character.netUser;
            if (user == null) return;

            DeployableObject deployableObject;
            if (IsCupboardNearDeploy(component.transform.position, out deployableObject) && !Users.SharedList(deployableObject.ownerID).Contains(user.userID) && user.userID != deployableObject.ownerID && !user.admin)
            {
                ReturnStruct(component, item);
                Notice(user, $"Нельзя строиться в радиусе шкафа игрока \"{Users.GetBySteamID(deployableObject.ownerID).Username}\"!");
            }
        }
        #endregion

        #region [Hook] Loaded()
        private void Loaded()
        {
            for (int i = 0; i < PlayerClient.All.Count; i++)
            {
                LoadVM(PlayerClient.All[i]);
            }
        }
        #endregion
        #region [Hook] Unload()
        private void Unload()
        {
            for (int i = 0; i < PlayerClient.All.Count; i++)
            {
                UnloadVM(PlayerClient.All[i]);
            }
        }
        #endregion

        #region [Hook] OnPlayerConnected(NetUser user)
        private void OnPlayerConnected(NetUser user)
        {
            var playerClient = user.playerClient;
            if (playerClient != null)
            {
                LoadVM(playerClient);
            }
        }
        #endregion
        #region [Hook] OnPlayerDisconnected(uLink.NetworkPlayer player)
        private void OnPlayerDisconnected(uLink.NetworkPlayer player)
        {
            var user = NetUser.Find(player);
            if (user != null)
                loadedPlayers.Remove(user.userID);
        }
        #endregion
        #region [Hook] OnPlayerInitialized(NetUser netuser, PlayerMods plmod)
        private void OnPlayerInitialized(NetUser netuser, PlayerMods plmod) => plmod.ChangeBlueprint("Repair Bench", new Dictionary<string, int> { { "Wood", 5000 } });
        #endregion

        #region [Function] Notice(NetUser user, string message)
        private void Notice(NetUser user, string message)
        {
            rust.Notice(user, message);
        }
        #endregion

        #region [Function] LoadVM(PlayerClient playerClient)
        private void LoadVM(PlayerClient playerClient)
        {
            if (playerClient != null && playerClient.netPlayer != null)
            {
                var cupboardVM = playerClient.gameObject.GetComponent<CupboardVM>();
                if (cupboardVM == null)
                {
                    var playerVM = playerClient.gameObject.AddComponent<CupboardVM>();
                    playerVM.localPlayer = playerClient;
                    loadedPlayers.Add(playerClient.userID);
                }
            }
        }
        #endregion
        #region [Function] UnloadVM(PlayerClient playerClient)
        private void UnloadVM(PlayerClient playerClient)
        {
            if (playerClient != null && playerClient.netPlayer != null)
            {
                var cupboardVM = playerClient.gameObject.GetComponent<CupboardVM>();
                if (cupboardVM != null)
                {
                    UnityEngine.Object.Destroy(cupboardVM);
                }
            }
        }
        #endregion  

        #region [Function] IsCupboardNearDeploy(Vector3 distance, out DeployableObject obj)
        private bool IsCupboardNearDeploy(Vector3 distance, out DeployableObject obj)
        {
            Collider[] colliders = Physics.OverlapSphere(distance, 40.0f);
            for (int i = 0; i < colliders.Length; i++)
            {
                DeployableObject deployableObject = colliders[i].gameObject.GetComponent<DeployableObject>();
                if (deployableObject != null && colliders[i].gameObject.name.ToLower().Contains("repairbench"))
                {
                    obj = deployableObject;
                    return true;
                }
            }
            obj = null;
            return false;
        }
        #endregion
        #region [Function] IsCupboardNearStruct(Vector3 distance, out StructureComponent obj)
        private bool IsCupboardNearStruct(Vector3 distance, out StructureComponent obj)
        {
            StructureComponent[] structureComponents = UnityEngine.Object.FindObjectsOfType<StructureComponent>();
            for (int i = 0; i < structureComponents.Length; i++)
            {
                if (Vector3.Distance(distance, structureComponents[i].transform.position) < 5f)
                {
                    obj = structureComponents[i];
                    return true;
                }
            }
            obj = null;
            return false;
        }
        #endregion
        #region [Function] IsCupboardNearZone(DeployableObject deploy, out DeployableObject obj)
        private bool IsCupboardNearZone(DeployableObject deploy, out DeployableObject obj)
        {
            Collider[] colliders = Physics.OverlapSphere(deploy.transform.position, 82.0f);
            for (int i = 0; i < colliders.Length; i++)
            {
                DeployableObject[] otherDeploys = colliders[i].GetComponents<DeployableObject>();
                for (int z = 0; z < otherDeploys.Length; z++)
                {
                    if (otherDeploys[z].name.ToLower().Contains("repairbench") && deploy != otherDeploys[z])
                    {
                        obj = otherDeploys[z];
                        return true;
                    }
                }
            }
            obj = null;
            return false;
        }
        #endregion

        #region [Function] ReturnDeploy(NetUser user, Component component, IDeployableItem item)
        private void ReturnDeploy(Component component, IDeployableItem item)
        {
            timer.Once(0.01f, () => NetCull.Destroy(component.gameObject)); item.inventory.AddItemSomehow(item.datablock, Inventory.Slot.Kind.Belt, item.slot, 1);
        }
        #endregion
        #region [Function] ReturnStruct(NetUser user, Component component, IStructureComponentItem item)
        private void ReturnStruct(Component component, IStructureComponentItem item)
        {
            timer.Once(0.01f, () => NetCull.Destroy(component.gameObject)); item.inventory.AddItemSomehow(item.datablock, Inventory.Slot.Kind.Belt, item.slot, 1);
        }
        #endregion

        #region [Class] CupboardVM : MonoBehaviour
        internal class CupboardVM : MonoBehaviour
        {
            public PlayerClient localPlayer;

            [RPC]
            public void GetShareState(ulong ownerID)
            {
                bool shareState = Users.SharedList(ownerID).Contains(localPlayer.userID);
                SendRPC("SetShareState", shareState);
            }

            private void SendRPC(string rpcName, params object[] args)
            {
                localPlayer.networkView.RPC(rpcName, localPlayer.netPlayer, args);
            }
        }
        #endregion
    }
}