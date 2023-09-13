using UnityEngine;
using RustExtended;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("BRaidBlock", "systemXcrackedZ", "1.3.2")]
    internal class BRaidBlock : RustLegacyPlugin
    {
        private readonly Dictionary<Vector3, int> blockZones = new Dictionary<Vector3, int>();
        
        private const float blockDistance = 70f; // УКАЗЫВАЕТСЯ В МЕТРАХ
        private const int blockTime = 600; // УКАЗЫВАЕТСЯ В СЕКУНДАХ

        private readonly List<ulong> loadedPlayers = new List<ulong>();
        private readonly List<ulong> enteredPlayers = new List<ulong>();

        private void OnKilled(TakeDamage takeDamage, DamageEvent damage)
        {
            try
            {
                if (damage.victim.idMain != null && damage.attacker.idMain != null)
                {
                    NetUser damageUser = damage.attacker.client.netUser;

                    if (!damageUser.admin && damage.damageTypes == DamageTypeFlags.damage_explosion &&
                        (damage.victim.idMain is DeployableObject || damage.victim.idMain is StructureComponent) &&
                        (takeDamage is StructureComponentTakeDamage || takeDamage is ProtectionTakeDamage))
                    {
                        StructureComponent structureComponent = damage.victim.idMain as StructureComponent;
                        DeployableObject deployableObject = damage.victim.idMain as DeployableObject;

                        if (structureComponent != null && deployableObject == null && damageUser != null && damageUser.userID != structureComponent._master.ownerID)
                        {
                            Vector3 point;
                            Vector3 structurePosition = structureComponent.transform.position;

                            if (IsInRaidZone(structurePosition, out point))
                                CancelRaidBlock(point);

                            timer.Once(1.2f, () => StartRaidBlock(structurePosition));
                        }
                        if (structureComponent == null && deployableObject != null && damageUser != null && damageUser.userID != deployableObject.ownerID)
                        {
                            Vector3 point, deployPosition = deployableObject.transform.position;

                            if (!IsInRaidZone(deployPosition, out point)) timer.Once(1.2f, () => StartRaidBlock(deployPosition));
                        }
                    }
                }
            }
            catch { }
        }
        private void OnStructureBuilt(StructureComponent component, IStructureComponentItem item)
        {
            NetUser user = item.character.netUser;
            if (user == null) return;

            if (IsInRaidZone(item.character.playerClient.lastKnownPosition))
            {
                timer.Once(0.01f, () => NetCull.Destroy(component.gameObject)); item.inventory.AddItemSomehow(item.datablock, Inventory.Slot.Kind.Belt, item.slot, 1);
                rust.Notice(user, $"Запрещено строиться находясь в зоне РейдБлока!");
            }
        }
        private void OnItemDeployed(DeployableObject component, IDeployableItem item)
        {
            NetUser user = item.character.netUser;
            if (user == null) return;

            if (IsInRaidZone(item.character.playerClient.lastKnownPosition)
            && !item.datablock.name.ToLower().Contains("barricade") && !item.datablock.name.ToLower().Contains("storage") && !item.datablock.name.ToLower().Contains("explosive"))
            {
                timer.Once(0.01f, () => NetCull.Destroy(component.gameObject)); item.inventory.AddItemSomehow(item.datablock, Inventory.Slot.Kind.Belt, item.slot, 1);
                rust.Notice(user, $"Запрещено строиться находясь в зоне РейдБлока!");
            }
        }

        [ChatCommand("home")]
        private void Home(NetUser user, string cmd, string[] args) => CommandExecutor(user, cmd, args);
        [ChatCommand("tp")]
        private void TP(NetUser user, string cmd, string[] args) => CommandExecutor(user, cmd, args);
        [ChatCommand("destroy")]
        private void Destroy(NetUser user, string cmd, string[] args) => CommandExecutor(user, cmd, args);
        [ChatCommand("sell")]
        private void Sell(NetUser user, string cmd, string[] args) => CommandExecutor(user, cmd, args);
        [ChatCommand("shop")]
        private void Shop(NetUser user, string cmd, string[] args) => CommandExecutor(user, cmd, args);

        [ChatCommand("clan")]
        private void Clan(NetUser user, string cmd, string[] args) => CommandExecutor(user, cmd, args);

        private void StartRaidBlock(Vector3 point)
        {
            blockZones.Add(point, blockTime);
            timer.Repeat(1f, blockTime - 2, () => 
            {
                if (blockZones.ContainsKey(point))
                {
                    blockZones[point]--;

                    if (blockZones[point] <= 5)
                        CancelRaidBlock(point);
                }
            });
        }
        private void CancelRaidBlock(Vector3 point) => blockZones.Remove(point);

        private bool IsInRaidZone(Vector3 point)
        {
            foreach (KeyValuePair<Vector3, int> raidZone in blockZones)
                if (Vector3.Distance(point, raidZone.Key) < blockDistance)
                    return true;

            return false;
        }
        private bool IsInRaidZone(Vector3 point, out Vector3 raidPoint)
        {
            foreach (Vector3 raidPosition in blockZones.Keys)
                if (Vector3.Distance(point, raidPosition) < blockDistance)
                {
                    raidPoint = raidPosition;
                    return true;
                }

            raidPoint = new Vector3();
            return false;
        }

        private void OnGetClientMove(HumanController controller, Vector3 origin)
        {
            Vector3 point;
            if (IsInRaidZone(controller.playerClient.lastKnownPosition, out point) && !enteredPlayers.Contains(controller.playerClient.userID))
            {
                int time = blockZones[point];

                enteredPlayers.Add(controller.playerClient.userID);
                GetRaidBlockVM(controller.playerClient).SendRPC("OERaidZone", time);
                rust.Notice(controller.netUser, "Вы в зоне РейдБлока! Запрещены некоторые команды и строительство.");
            }
            if (enteredPlayers.Contains(controller.playerClient.userID) && !IsInRaidZone(controller.playerClient.lastKnownPosition))
            {
                enteredPlayers.Remove(controller.playerClient.userID);
                GetRaidBlockVM(controller.playerClient).SendRPC("OLRaidZone");
                rust.Notice(controller.netUser, "Вы вне зоны РейдБлока! Команды и строительство разрешены.");
            }
        }
        private bool IsCMDBlocked(Vector3 point) => IsInRaidZone(point);

        private void Loaded()
        {
            for (int i = 0; i < PlayerClient.All.Count; i++)
            {
                LoadVM(PlayerClient.All[i]);
                if (enteredPlayers.Contains(PlayerClient.All[i].userID)) enteredPlayers.Remove(PlayerClient.All[i].userID);
                GetRaidBlockVM(PlayerClient.All[i]).SendRPC("OLRaidZone");
            }
        }
        private void Unload()
        {
            for (int i = 0; i < PlayerClient.All.Count; i++)
                UnloadVM(PlayerClient.All[i]);
        }

        private void OnPlayerConnected(NetUser user)
        {
            PlayerClient playerClient = user.playerClient;
            if (playerClient != null)
                LoadVM(playerClient);
        }
        private void OnPlayerDisconnected(uLink.NetworkPlayer networkPlayer)
        {
            foreach (ulong steamID in loadedPlayers)
            {
                PlayerClient pClient;
                PlayerClient.FindByUserID(steamID, out pClient);
                if (pClient == null || pClient.netPlayer == networkPlayer)
                {
                    loadedPlayers.Remove(steamID);

                    if (enteredPlayers.Contains(steamID)) enteredPlayers.Remove(steamID);

                    break;
                }
            }
        }

        private void LoadVM(PlayerClient playerClient)
        {
            if (playerClient != null && playerClient.netPlayer != null)
            {
                RaidBlockVM raidBlockVM = GetRaidBlockVM(playerClient);
                if (raidBlockVM == null)
                {
                    RaidBlockVM playerVM = playerClient.gameObject.AddComponent<RaidBlockVM>();
                    playerVM.localPlayer = playerClient;
                    loadedPlayers.Add(playerClient.userID);
                }
            }
        }
        private void UnloadVM(PlayerClient playerClient)
        {
            if (playerClient != null && playerClient.netPlayer != null)
            {
                RaidBlockVM raidBlockVM = GetRaidBlockVM(playerClient);
                if (raidBlockVM != null)
                    UnityEngine.Object.Destroy(raidBlockVM);
            }
        }

        private void CommandExecutor(NetUser user, string cmd, string[] args)
        {
            Vector3 point;
            if (cmd == "clan" && args.Length >= 1 && args[0] == "warp")
            {
                if (IsInRaidZone(user.playerClient.lastKnownPosition, out point))
                {
                    rust.Notice(user, $"Нельзя использовать команду \"{cmd}\" в зоне рейдблока!");
                    return;
                }
                else
                {
                    Commands.Clan(user, Users.GetBySteamID(user.userID), cmd, args);
                    return;
                }
            }

            if (IsInRaidZone(user.playerClient.lastKnownPosition, out point))
            {
                rust.Notice(user, $"Нельзя использовать команду \"{cmd}\" в зоне рейдблока!");
                return;
            }
            try
            {
                switch (cmd)
                {
                    case "home": { Commands.Home(user, Users.GetBySteamID(user.userID), cmd, args); return; }
                    case "tp": { Commands.Teleport(user, Users.GetBySteamID(user.userID), cmd, args); return; }
                    case "destroy": { Commands.Destroy(user, Users.GetBySteamID(user.userID), cmd, args); return; }
                    case "sell": { Economy.ShopSell(user, Users.GetBySteamID(user.userID), cmd, args); return; }
                    case "shop": { Economy.ShopList(user, Users.GetBySteamID(user.userID), cmd, args); return; }
                   
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.InnerException);
                Debug.Log(ex.StackTrace);
                Debug.Log(ex.TargetSite);
                Debug.Log(ex.Source);
                Debug.Log(ex.HelpLink);
            }
        }

        private RaidBlockVM GetRaidBlockVM(PlayerClient playerClient) => playerClient.gameObject.GetComponent<RaidBlockVM>();

        internal class RaidBlockVM : MonoBehaviour
        {
            public PlayerClient localPlayer;

            public void SendRPC(string rpcName, params object[] args) => localPlayer.networkView.RPC(rpcName, localPlayer.netPlayer, args);
        }
    }
}
