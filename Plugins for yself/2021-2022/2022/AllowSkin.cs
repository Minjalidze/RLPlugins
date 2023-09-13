using System;
using Oxide.Core;
using UnityEngine;
using System.Collections.Generic;
using RustExtended;

namespace Oxide.Plugins
{
    [Info("AllowSkin", "kusarigama", "2.2.3")]
    internal class AllowSkin : RustLegacyPlugin
    {
        private List<ulong> AllowUsers;
        private List<ulong> loadedPlayers = new List<ulong>();

        private void LoadPluginToPlayer(PlayerClient pClient)
        {
            if (pClient.gameObject.GetComponent<UserVM>() != null) return;
            if (loadedPlayers.Contains(pClient.userID)) loadedPlayers.Remove(pClient.userID);

            UserVM vm = pClient.gameObject.AddComponent<UserVM>();
            vm.playerClient = pClient;
            vm.allowSkin = this;

            loadedPlayers.Add(pClient.userID);
        }
        private void UnloadPluginFromPlayer(GameObject gameObject, Type plugin)
        {
            if (gameObject.GetComponent(plugin) != null) UnityEngine.Object.Destroy(gameObject.GetComponent(plugin));
        }

        private void Loaded()
        {
            foreach (var pc in PlayerClient.All) if (pc != null && pc.netPlayer != null) LoadPluginToPlayer(pc);

            try
            {
                AllowUsers = Interface.GetMod().DataFileSystem.ReadObject<List<ulong>>("AllowedUsers");
            }
            catch
            {
                AllowUsers = new List<ulong>();
                SavePluginData();
            }
        }
        private void Unload()
        {
            SavePluginData();
            foreach (ulong loadedPlayer in loadedPlayers)
            {
                PlayerClient playerClient; PlayerClient.FindByUserID(loadedPlayer, out playerClient);
                if (playerClient != null)
                    UnloadPluginFromPlayer(playerClient.gameObject, typeof(UserVM));
            }
        }

        private void OnServerSave() =>
            SavePluginData();
        private void SavePluginData() =>
            Interface.GetMod().DataFileSystem.WriteObject("AllowedUsers", AllowUsers);

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

        [ChatCommand("addduty")]
        private void CMD_AddDuty(NetUser netUser, string command, string[] arguments)
        {
            if (netUser.admin)
            {
                if (arguments.Length != 0)
                {
                    UserData userData = Users.All.Find(predicate => predicate.Username == arguments[0] || predicate.Username.ToLower().Contains(arguments[0]) || predicate.SteamID.ToString() == arguments[0]);
                    if (userData != null)
                    {
                        bool isAllowUser = AllowUsers.Contains(userData.SteamID);
                        if (!isAllowUser)
                        {
                            rust.Notice(netUser, "You give a duty for \"%USERNAME%\".".Replace("%USERNAME%", userData.Username));
                            AllowUsers.Add(userData.SteamID);

                            SavePluginData();
                        }
                        else rust.Notice(netUser, "\"%USERNAME%\" already have a duty.".Replace("%USERNAME%", userData.Username));
                    }
                    else rust.Notice(netUser, "\"%USERNAME%\" is null.".Replace("%USERNAME%", userData.Username));
                }
                else rust.Notice(netUser, "Please, entry a PlayerName or SteamID.");
            }
            else rust.Notice(netUser, "You are not administrator.");
        }
        [ChatCommand("unduty")]
        private void CMD_UnDuty(NetUser netUser, string command, string[] arguments)
        {
            if (netUser.admin)
            {
                if (arguments.Length != 0)
                {
                    UserData userData = Users.All.Find(predicate => predicate.Username == arguments[0] || predicate.Username.ToLower().Contains(arguments[0]) || predicate.SteamID.ToString() == arguments[0]);
                    if (userData != null)
                    {
                        bool isAllowUser = AllowUsers.Contains(userData.SteamID);
                        if (isAllowUser)
                        {
                            rust.Notice(netUser, "You remove a duty from \"%USERNAME%\".".Replace("%USERNAME%", userData.Username));
                            AllowUsers.Remove(userData.SteamID);

                            SavePluginData();
                        }
                        else rust.Notice(netUser, "\"%USERNAME%\" has not a duty.".Replace("%USERNAME%", userData.Username));
                    }
                    else rust.Notice(netUser, "\"%USERNAME%\" is null.".Replace("%USERNAME%", userData.Username));
                }
                else rust.Notice(netUser, "Please, entry a PlayerName or SteamID.");
            }
            else rust.Notice(netUser, "You are not administrator.");
        }

        [ConsoleCommand("oxide.addduty")]
        private void CCMD_AddDuty(ConsoleSystem.Arg argument)
        {
            if (argument.Args.Length != 0)
            {
                UserData userData = Users.All.Find(predicate => predicate.Username.ToLower().Contains(argument.Args[0]) || predicate.Username.ToLower() == argument.Args[0] || predicate.SteamID.ToString() == argument.Args[0]);
                if (userData != null)
                {
                    bool isAllowUser = AllowUsers.Contains(userData.SteamID);
                    if (!isAllowUser)
                    {
                        Debug.Log("[%SERVERNAME%]: You give a duty for \"%USERNAME%\".".Replace("%USERNAME%", userData.Username).Replace("%SERVERNAME%", "AllowSkin"));
                        AllowUsers.Add(userData.SteamID);

                        SavePluginData();
                    }
                    else Debug.Log("[%SERVERNAME%]: \"%USERNAME%\" already have a duty.".Replace("%USERNAME%", userData.Username).Replace("%SERVERNAME%", "AllowSkin"));
                }
                else Debug.Log("[%SERVERNAME%]: Please, entry a PlayerName or SteamID.".Replace("%SERVERNAME%", "AllowSkin"));
            }
            else Debug.Log("[%SERVERNAME%]: Usage - oxide.addduty <PlayerName or SteamID>".Replace("%SERVERNAME%", "AllowSkin"));
        }
        [ConsoleCommand("oxide.unduty")]
        private void CCMD_UnDuty(ConsoleSystem.Arg argument)
        {
            if (argument.Args.Length != 0)
            {
                UserData userData = Users.All.Find(predicate => predicate.Username.ToLower().Contains(argument.Args[0]) || predicate.Username.ToLower() == argument.Args[0] || predicate.SteamID.ToString() == argument.Args[0]);
                if (userData != null)
                {
                    bool isAllowUser = AllowUsers.Contains(userData.SteamID);
                    if (isAllowUser)
                    {
                        Debug.Log("[%SERVERNAME%]: You remove a duty from \"%USERNAME%\".".Replace("%USERNAME%", userData.Username).Replace("%SERVERNAME%", "AllowSkin"));
                        AllowUsers.Remove(userData.SteamID);

                        SavePluginData();
                    }
                    else Debug.Log("[%SERVERNAME%]: \"%USERNAME%\" has not a duty.".Replace("%USERNAME%", userData.Username).Replace("%SERVERNAME%", "AllowSkin"));
                }
                else Debug.Log("[%SERVERNAME%]: Please, entry a PlayerName or SteamID.".Replace("%SERVERNAME%", "AllowSkin"));
            }
            else Debug.Log("[%SERVERNAME%]: Usage - oxide.unduty <PlayerName or SteamID>".Replace("%SERVERNAME%", "AllowSkin"));
        }

        internal class UserVM : MonoBehaviour
        {
            public PlayerClient playerClient;
            public AllowSkin allowSkin;

            [RPC]
            public void GetDuty()
            {
                bool isTrue = allowSkin.AllowUsers.Contains(playerClient.userID);

                SendRPC("SetDuty", isTrue);
                Debug.Log($"{playerClient.userName} : {isTrue}");
            }

            public void SendRPC(string rpcName, params object[] param) => GetComponent<Facepunch.NetworkView>().RPC(rpcName, playerClient.netPlayer, param);
        }
    }
}