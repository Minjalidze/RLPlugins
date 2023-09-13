using System;
using Oxide.Core;
using System.Linq;
using RustExtended;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BRaidServer", "systemXcrackedZ", "1.2.3")]
    internal class BRaidServer : RustLegacyPlugin
    {
        private List<ulong> loadedPlayers = new List<ulong>();
        private class Group
        {
            public string Name { get; set; }
            public ulong Leader { get; set; }
            public List<ulong> Members { get; set; }
            public int Points { get; set; }
            public string AvatarURL { get; set; }

            public int Kills { get; set; }
            public int Deaths { get; set; }
            public int Activity { get; set; }
        }
        private class PluginData
        {
            public PluginData(List<Group> groups, string wipeStartTime, string weaponBlockTime, string raidBlockTime, string wipeTime)
            {
                Groups = groups;
                WipeStartTime = wipeStartTime;
                WeaponBlockTime = weaponBlockTime;
                RaidBlockTime = raidBlockTime;
                WipeTime = wipeTime;
            }

            public List<Group> Groups { get; set; }
            public string WipeStartTime { get; set; }
            public string WeaponBlockTime { get; set; }
            public string RaidBlockTime { get; set; }
            public string WipeTime { get; set; }
        }

        private PluginData pluginData;

        private readonly Dictionary<string, bool> BlockStates = new Dictionary<string, bool>
        {
            { "IsWeaponBlock", true }, 
            { "IsRaidBlock", true }, 
            { "IsWipe", false }
        };
        private readonly Dictionary<ulong, Group> InviteRequests = new Dictionary<ulong, Group>();

        private Group GetGroupByName(string name) =>
            pluginData.Groups.Find(party => party.Name == name);
        private Group GetGroupByUserID(ulong userID) =>
            pluginData.Groups.Find(party => party.Members.Contains(userID));
        private Group GetGroupByLeader(ulong userID) =>
            pluginData.Groups.Find(party => party.Leader == userID);

        private Dictionary<int, string> Rewards = new Dictionary<int, string>
        {
            { 1, "200RUB" },
            { 2, "200RUB" },
            { 3, "200RUB" },
            { 4, "200RUB" },
            { 5, "200RUB" },
            { 6, "200RUB" },
            { 7, "200RUB" },
            { 8, "200RUB" },
            { 9, "200RUB" },
            { 10, "200RUB" }
        };

        private void Loaded()
        {
            try
            {
                pluginData = Interface.GetMod().DataFileSystem.ReadObject<PluginData>("RaidServerData");
            }
            catch
            {
                pluginData = new PluginData(new List<Group>(), 
                    DateTime.Now.ToString(), 
                    DateTime.Now.Add(new TimeSpan(PVEHours, 0, 0)).ToString(), 
                    DateTime.Now.Add(new TimeSpan(PVEHours+PVPHours, 0, 0)).ToString(), 
                    DateTime.Now.Add(new TimeSpan(PVEHours + PVPHours+RaidHours, 0, 0)).ToString());
                SavePluginData();
            }
            foreach (var pc in PlayerClient.All) if (pc != null && pc.netPlayer != null) LoadPluginToPlayer(pc);
        }
        private void Unload()
        {
            foreach (ulong loadedPlayer in loadedPlayers)
            {
                PlayerClient playerClient; PlayerClient.FindByUserID(loadedPlayer, out playerClient);
                if (playerClient != null)
                    UnloadPluginFromPlayer(playerClient.gameObject, typeof(RaidVM));
            }
            SavePluginData();
        }
        private void OnServerSave()
        {
            SavePluginData();
            CheckBlockTiming();
        }
        private void SavePluginData() => 
            Interface.GetMod().DataFileSystem.WriteObject("RaidServerData", pluginData);

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

        private readonly string[] Commands =
        {
            "/group create \"название\" - создать группу", "/group invite \"игрок\" - пригласить игрока в группу",
            "/group disband - распустить группу", "/group kick \"игрок\" - выгнать игрока из группы",
            "/group members - список всех участников группы", "/group list - список всех групп",
            "/group avatar \"ссылка\" - установить аватарку группы"
        };
        private readonly string[] Explosives = new string[2] { "Explosive Charge", "F1 Grenade" };
        private readonly string[] Weapons = new string[9] { "HandCannon", "Pipe Shotgun", "Revolver", "P250", "MP5A4", "Shotgun", "9mm Pistol", "Bolt Action Rifle", "M4" };

        private const string defaultAvatar = "http://198.244.249.28/project/unknown/Avatar.jpg";

        private const int maxGroupCount = 3;
        private const int PVEHours = 6, PVPHours = 15, RaidHours = 15;
        private readonly bool IsBlockDamage = false, IsBlockWeapons = false, IsBlockExplosives = false;

        [ChatCommand("raidwipe")]
        private void OnCMD_RaidWipe(NetUser user, string cmd, string[] args)
        {
            if (user.CanAdmin())
            {
                pluginData = new PluginData(new List<Group>(),
                    DateTime.Now.ToString(),
                    DateTime.Now.Add(new TimeSpan(PVEHours, 0, 0)).ToString(),
                    DateTime.Now.Add(new TimeSpan(PVEHours + PVPHours, 0, 0)).ToString(),
                    DateTime.Now.Add(new TimeSpan(PVEHours + PVPHours + RaidHours, 0, 0)).ToString());
                SavePluginData();
            }
        }
        [ChatCommand("group")]
        private void OnCMD_Group(NetUser user, string cmd, string[] args)
        {
            if (args.Length == 0)
            {
                SendMessage(user, "Список команд: ");
                foreach (string command in Commands) SendMessage(user, command);
                return;
            }
            switch (args[0])
            {
                case "create":
                    {
                        if (args.Length < 2)
                        {
                            SendMessage(user, "Использование команды: /group create \"название\".");
                            return;
                        }
                        InitAction("CreateGroup", user, args[1]);
                        break;
                    }
                case "invite":
                    {
                        if (args.Length < 2)
                        {
                            SendMessage(user, "Использование команды: /group invite \"игрок\".");
                            return;
                        }
                        InitAction("GroupInvite", user, args[1]);
                        break;
                    }
                case "disband":
                    {
                        InitAction("GroupDisband", user, args[1]);
                        break;
                    }
                case "kick":
                    {
                        if (args.Length < 2)
                        {
                            SendMessage(user, "Использование команды: /group kick \"игрок\".");
                            return;
                        }
                        InitAction("GroupKick", user, args[1]);
                        break;
                    }
                case "members":
                    {
                        InitAction("GroupMembers", user);
                        break;
                    }
                case "list":
                    {
                        InitAction("GroupList", user);
                        break;
                    }
                case "accept":
                    {
                        InitAction("GroupAccept", user);
                        break;
                    }
                case "avatar":
                    {
                        if (args.Length < 2)
                        {
                            SendMessage(user, "Использование команды: /group avatar \"URL\".");
                            return;
                        }
                        InitAction("GroupAvatar", user);
                        break;
                    }
            }
        }

        [HookMethod("OnBeltUse")]
        public object OnBeltUse(PlayerInventory playerInv, IInventoryItem inventoryItem)
        {
            if (inventoryItem != null)
            {
                NetUser netuser = NetUser.Find(playerInv.networkView.owner);
                if (netuser == null || netuser.admin) return null;

                if (!string.IsNullOrEmpty(Weapons.FirstOrDefault(f => f == inventoryItem.datablock.name)) && IsBlockWeapons && (BlockStates["IsWipe"] || BlockStates["IsWeaponBlock"]))
                {
                    rust.Notice(netuser, $"[WipeBlock]: В данный момент запрещено пользоваться \"{inventoryItem.datablock.name}\"!");
                    return true;
                }
                if (!string.IsNullOrEmpty(Explosives.FirstOrDefault(f => f == inventoryItem.datablock.name)) && IsBlockExplosives && (BlockStates["IsWipe"] || BlockStates["IsRaidBlock"]))
                {
                    rust.Notice(netuser, $"[WipeBlock]: В данный момент запрещено пользоваться \"{inventoryItem.datablock.name}\"!");
                    return true;
                }
                return null;
            }
            return null;
        }

        private object ModifyDamage(TakeDamage takeDamage, DamageEvent evt)
        {
            try
            {
                NetUser attackerUser = evt.attacker.client?.netUser ?? null;
                NetUser victimUser = evt.victim.client?.netUser ?? null;

                if (takeDamage is HumanBodyTakeDamage && attackerUser != null && victimUser != null && attackerUser != victimUser && IsBlockDamage && (BlockStates["IsWipe"] || BlockStates["IsWeaponBlock"])) return NullDamage(evt, true);
                if (!(evt.victim.idMain is Character))
                {
                    ulong victimID = 0, attackerID = attackerUser.userID;
                    if (evt.victim.idMain is DeployableObject) victimID = (evt.victim.idMain as DeployableObject).ownerID;
                    if (evt.victim.idMain is StructureComponent) victimID = (evt.victim.idMain as StructureComponent)._master.ownerID;
                    if (attackerID != victimID && IsBlockDamage && (BlockStates["IsWipe"] || BlockStates["IsRaidBlock"])) return NullDamage(evt);
                }
                return null;
            }
            catch { return null; }
        }

        private void LoadPluginToPlayer(PlayerClient pClient)
        {
            if (pClient.gameObject.GetComponent<RaidVM>() != null) return;
            if (loadedPlayers.Contains(pClient.userID)) loadedPlayers.Remove(pClient.userID);

            RaidVM vm = pClient.gameObject.AddComponent<RaidVM>();
            vm.playerClient = pClient;
            vm.raidServer = this;

            loadedPlayers.Add(pClient.userID);
        }
        private void UnloadPluginFromPlayer(GameObject gameObject, Type plugin)
        {
            if (gameObject.GetComponent(plugin) != null) UnityEngine.Object.Destroy(gameObject.GetComponent(plugin));
        }

        private void CheckBlockTiming()
        {
            TimeSpan dateTimeDiff;
            TimeSpan nullSpan = new TimeSpan(0);

            pluginData.Groups = pluginData.Groups.OrderByDescending(f => f.Points).ToList();
            SavePluginData();

            if (BlockStates["IsWeaponBlock"])
            {
                dateTimeDiff = DateTime.Parse(pluginData.WipeStartTime) - DateTime.Parse(pluginData.WeaponBlockTime);
                if (dateTimeDiff >= nullSpan) BlockStates["IsWeaponBlock"] = false;
            }
            if (BlockStates["IsRaidBlock"])
            {
                dateTimeDiff = DateTime.Parse(pluginData.WipeStartTime) - DateTime.Parse(pluginData.RaidBlockTime);
                if (dateTimeDiff >= nullSpan) BlockStates["IsRaidBlock"] = false;
            }
            if (!BlockStates["IsWipe"])
            {
                dateTimeDiff = DateTime.Parse(pluginData.WipeStartTime) - DateTime.Parse(pluginData.WipeTime);
                if (dateTimeDiff >= nullSpan) BlockStates["IsWipe"] = true;
            }
        }
        private void SendMessage(NetUser user, string message) =>
            rust.SendChatMessage(user, RustExtended.Core.ServerName, $"[COLOR # 32CD32]{message}");
        private void SendRequest(ulong invitedID, Group group)
        {
            InviteRequests.Add(invitedID, group);

            string leaderUserName = NetUser.FindByUserID(group.Leader).playerClient.userName;
            NetUser invitedUser = NetUser.FindByUserID(invitedID);

            SendMessage(invitedUser, $"Игрок \"{leaderUserName}\" пригласил Вас в группу \"{group.Name}\"!");
            SendMessage(invitedUser, "Введите \"/group accept\" в течении 20 секунд для того чтобы принять запрос!");
            timer.Once(20f, () =>
            {
                if (!InviteRequests.ContainsKey(invitedID)) return;
                InviteRequests.Remove(invitedID);
                SendMessage(invitedUser, "Вы не успели принять запрос в группу!");
            });
        }

        private object NullDamage(DamageEvent damage, bool isPlayer = false)
        {
            damage.amount = 0f;
            if (isPlayer) damage.status = LifeStatus.IsAlive;
            return damage;
        }

        private void InitAction(string actionName, NetUser user, params object[] args)
        {
            switch (actionName)
            {
                case "CreateGroup":
                    {
                        string name = (string)args[0];
                        ulong leaderID = user.userID;

                        Group group = GetGroupByLeader(user.userID) ?? GetGroupByUserID(user.userID);
                        if (group != null)
                        {
                            SendMessage(NetUser.FindByUserID(leaderID), "Вы уже состоите в группе!");
                            return;
                        }
                        if (GetGroupByName(name) != null)
                        {
                            SendMessage(NetUser.FindByUserID(leaderID), $"Группа с названием {name} уже существует!");
                            return;
                        }
                        SendMessage(NetUser.FindByUserID(leaderID), $"Вы успешно создали группу с названием {name}!");
                        pluginData.Groups.Add(new Group { Leader = leaderID, Name = name, Members = new List<ulong>(), AvatarURL = defaultAvatar });
                        break;
                    }
                case "GroupDisband":
                    {
                        Group group = GetGroupByLeader(user.userID);
                        if (group == null)
                        {
                            SendMessage(user, "Вы не являетесь лидером ни одной из групп!");
                            return;
                        }

                        foreach (ulong member in group.Members) SendMessage(NetUser.FindByUserID(member), $"Группа \"{group.Name}\" была распущена её лидером!");

                        pluginData.Groups.Remove(group);
                        SendMessage(user, "Вы успешно распустили свою группу!");
                        break;
                    }
                case "GroupKick":
                    {
                        Group group = GetGroupByLeader(user.userID);
                        if (group == null)
                        {
                            SendMessage(user, "Вы не являетесь лидером ни одной из групп!");
                            return;
                        }

                        UserData userData = Users.Find((string)args[0]);
                        if (userData == null)
                        {
                            SendMessage(user, $"Игрок с ником \"{args[0]}\" не найден!");
                            return;
                        }

                        if (!group.Members.Contains(userData.SteamID))
                        {
                            SendMessage(user, $"Игрок \"{args[0]}\" не состоит в Вашей группе!");
                            return;
                        }

                        group.Members.Remove(userData.SteamID);
                        SendMessage(NetUser.FindByUserID(userData.SteamID), $"Вас исключили из группы \"{group.Name}\"!");
                        SendMessage(user, $"Вы исключили игрока {userData.Username} из группы!");
                        break;
                    }
                case "GroupInvite":
                    {
                        Group group = GetGroupByLeader(user.userID);
                        if (group == null)
                        {
                            SendMessage(user, "Вы не являетесь лидером ни одной из групп!");
                            return;
                        }
                        if (group.Members.Count >= maxGroupCount)
                        {
                            SendMessage(user, "В вашей группе достигнуто максимальное число участников.");
                            return;
                        }
                        UserData userData = Users.Find(args[0] as string);
                        if (userData == null)
                        {
                            SendMessage(user, $"Игрок с ником \"{args[0]}\" не найден!");
                            return;
                        }
                        if (GetGroupByUserID(userData.SteamID) != null || GetGroupByLeader(userData.SteamID) != null)
                        {
                            SendMessage(user, $"Игрок с ником \"{args[0]}\" уже состоит в одной из групп!");
                            return;
                        }
                        if (InviteRequests.ContainsKey(userData.SteamID))
                        {
                            SendMessage(user, $"Игрок с ником \"{args[0]}\" уже имеет запрос в одну из групп!");
                            return;
                        }
                        SendRequest(userData.SteamID, group);
                        SendMessage(user, $"Вы пригласили в группу игрока \"{userData.Username}\"");
                        break;
                    }
                case "GroupMembers":
                    {
                        Group group = GetGroupByLeader(user.userID) ?? GetGroupByUserID(user.userID);
                        if (group == null)
                        {
                            SendMessage(user, "Вы не состоите ни в одной из групп!");
                            return;
                        }

                        SendMessage(user, $"Список участников группы \"{group.Name}\" ({group.Members.Count}):");
                        SendMessage(user, $"Лидер: \"{Users.GetBySteamID(group.Leader).Username}\"");
                        int i = 1;
                        foreach (ulong memberID in group.Members)
                        {
                            SendMessage(user, $"{i}) {Users.GetBySteamID(memberID).Username}");
                            i++;
                        }
                        break;
                    }
                case "GroupList":
                    {
                        SendMessage(user, "Список всех групп:");
                        int i = 1;
                        foreach (Group party in pluginData.Groups)
                        {
                            SendMessage(user, $"{i}) Группа: {party.Name}. Лидер: {Users.GetBySteamID(party.Leader).Username}");
                            i++;
                        }
                        break;
                    }
                case "GroupAccept":
                    {
                        if (!InviteRequests.ContainsKey(user.userID))
                        {
                            SendMessage(user, "Вы не имеете активного запроса в группу!");
                            return;
                        }

                        Group group = InviteRequests[user.userID];
                        group.Members.Add(user.userID);

                        SendMessage(user, $"Вы успешно вступили в группу \"{group.Name}\"!");
                        SendMessage(NetUser.FindByUserID(group.Leader), $"Игрок \"{user.playerClient.userName}\" вступил в вашу группу!");

                        InviteRequests.Remove(user.userID);
                        break;
                    }
                case "GroupAvatar":
                    {
                        Group group = GetGroupByLeader(user.userID);
                        if (group == null)
                        {
                            SendMessage(user, "Вы не являетесь лидером ни одной из групп!");
                            return;
                        }

                        group.AvatarURL = args[1] as string;

                        break;
                    }
            }
            SavePluginData();
        }

        internal class RaidVM : MonoBehaviour
        {
            public PlayerClient playerClient;
            public BRaidServer raidServer;

            [RPC]
            public void GetStats()
            {
                SendRPC("ClearGroups");
                Group group = raidServer.GetGroupByLeader(playerClient.userID) ?? raidServer.GetGroupByUserID(playerClient.userID);
                if (group == null)
                {
                    SendRPC("SetStats", "null", 0, 0, 0, 0, 0);
                    return;
                }
                SendRPC("SetStats", raidServer.pluginData.Groups.IndexOf(group), group.Name, group.Points, group.Activity, group.Kills, group.Deaths);
                if (group.AvatarURL != defaultAvatar) SendRPC("SetAvatar", group.AvatarURL);

                if (raidServer.pluginData.Groups.Count > 0)
                {
                    for (int i = 1; i <= 10; i++)
                    {
                        SendRPC("AddGroup", raidServer.pluginData.Groups[i].Name, raidServer.Rewards[i], raidServer.pluginData.Groups[i].Points);
                    }
                }
            }

            public void SendRPC(string rpcName, params object[] param) =>
                GetComponent<Facepunch.NetworkView>().RPC(rpcName, playerClient.netPlayer, param);
        }
    }
}
