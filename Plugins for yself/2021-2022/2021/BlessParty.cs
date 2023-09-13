using System.Collections.Generic;
using Oxide.Core;
using RustExtended;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BlessParty", "systemXcrackedZ", "1.0.0")]
    internal class BlessParty : RustLegacyPlugin
    {
        #region [CLASS] -> Главный Класс. Группы.
        private class Party
        {
            public string Name { get; set; }
            public ulong Leader { get; set; }
            public List<ulong> Members { get; set; }
        }
        #endregion

        #region [LIST & DICTIONARY] -> Списки и словари. Хранение данных о группах.
        private static List<Party> _parties = new List<Party>();
        private static readonly Dictionary<ulong, Party> InviteRequests = new Dictionary<ulong, Party>();
        private static readonly string[] Commands =
        {
            "/party create \"название\" - создать группу", "/party invite \"игрок\" - пригласить игрока в группу",
            "/party disband - распустить группу", "/party kick \"игрок\" - выгнать игрока из группы",
            "/party members - список всех участников группы", "/party list - список всех групп"
        };
        #endregion

        #region [Party] -> Действия с классом Party.

        private static Party GetPartyByLeader(ulong userID)
        {
            return _parties.Find(party => party.Leader == userID);
        }
        private static Party GetPartyByUserID(ulong userID)
        {
            return _parties.Find(party => party.Members.Contains(userID));
        }
        private static Party GetPartyByName(string name)
        {
            return _parties.Find(party => party.Name == name);
        }

        #endregion

        #region [CMD] -> Команда. /party.

        [ChatCommand("party")]
        private void OnCMD_Party(NetUser user, string cmd, string[] args)
        {
            if (args.Length == 0)
            {
                SendMessage(user, "Список команд: ");
                foreach (var command in Commands) SendMessage(user, command);
                return;
            }

            switch (args[0])
            {
                case "create":
                    if (args.Length < 2)
                    {
                        SendMessage(user, "Использование команды: /party create \"название\".");
                        return;
                    }

                    CreateParty(args[1], user.userID);
                    return;
                case "invite":
                    if (args.Length < 2)
                    {
                        SendMessage(user, "Использование команды: /party invite \"игрок\".");
                        return;
                    }

                    PartyInvite(user, args[1]);
                    return;
                case "disband":
                    PartyDisband(user);
                    return;
                case "kick":
                    if (args.Length < 2)
                    {
                        SendMessage(user, "Использование команды: /party kick \"игрок\".");
                        return;
                    }

                    PartyKick(user, args[1]);
                    return;
                case "members":
                    PartyMembers(user);
                    return;
                case "list":
                    PartyList(user);
                    return;
                case "accept":
                    PartyAccept(user);
                    break;
            }
        }

        #endregion

        #region [BOOL] -> Логика. Функции команд.

        private bool CreateParty(string name, ulong leaderID)
        {
            if (GetPartyByLeader(leaderID) != null)
            {
                SendMessage(NetUser.FindByUserID(leaderID), "Вы уже состоите в группе!");
                return false;
            }

            if (GetPartyByName(name) != null)
            {
                SendMessage(NetUser.FindByUserID(leaderID), $"Группа с названием {name} уже существует!");
                return false;
            }

            SendMessage(NetUser.FindByUserID(leaderID), $"Вы успешно создали группу с названием {name}!");
            _parties.Add(new Party {Leader = leaderID, Name = name, Members = new List<ulong>()});
            SavePluginData();
            return true;
        }

        private bool PartyDisband(NetUser user)
        {
            var party = GetPartyByLeader(user.userID);
            if (party == null)
            {
                SendMessage(user, "Вы не являетесь лидером ни одной из групп!");
                return false;
            }

            foreach (var member in party.Members)
                SendMessage(NetUser.FindByUserID(member), $"Группа {party.Name} была распущена её лидером!");
            _parties.Remove(party);
            SendMessage(user, "Вы успешно распустили свою группу!");
            SavePluginData();
            return true;
        }

        private bool PartyInvite(NetUser leader, string userName)
        {
            var party = GetPartyByLeader(leader.userID);
            if (party == null)
            {
                SendMessage(leader, "Вы не являетесь лидером ни одной из групп!");
                return false;
            }

            var userData = Users.Find(userName);
            if (userData == null)
            {
                SendMessage(leader, $"Игрок с ником \"{userName}\" не найден!");
                return false;
            }

            if (GetPartyByUserID(userData.SteamID) != null)
            {
                Debug.Log("1");
                SendMessage(leader, $"Игрок с ником \"{userName}\" уже состоит в одной из групп!");
                Debug.Log("2");
                return false;
            }

            if (InviteRequests.ContainsKey(userData.SteamID))
            {
                SendMessage(leader, $"Игрок с ником \"{userName}\" уже имеет запрос в одну из групп!");
                return false;
            }

            SendRequest(userData.SteamID, party);
            SendMessage(leader, $"Вы пригласили в группу игрока \"{userData.Username}\"");
            return true;
        }

        private bool PartyKick(NetUser leader, string userName)
        {
            var party = GetPartyByLeader(leader.userID);
            if (party == null)
            {
                SendMessage(leader, "Вы не являетесь лидером ни одной из групп!");
                return false;
            }

            var userData = Users.Find(userName);
            if (userData == null)
            {
                SendMessage(leader, $"Игрок с ником \"{userName}\" не найден!");
                return false;
            }

            if (!party.Members.Contains(userData.SteamID))
            {
                SendMessage(leader, $"Игрок \"{userName}\" не состоит в Вашей группе!");
                return false;
            }

            party.Members.Remove(userData.SteamID);
            SendMessage(NetUser.FindByUserID(userData.SteamID), $"Вас исключили из группы \"{party.Name}\"!");
            SendMessage(leader, $"Вы исключили игрока {userData.Username} из группы!");
            SavePluginData();
            return true;
        }

        private bool PartyAccept(NetUser user)
        {
            if (!InviteRequests.ContainsKey(user.userID))
            {
                SendMessage(user, "Вы не имеете активного запроса в группу!");
                return false;
            }

            var party = InviteRequests[user.userID];
            party.Members.Add(user.userID);
            SendMessage(user, $"Вы успешно вступили в группу \"{party.Name}\"!");
            SendMessage(NetUser.FindByUserID(party.Leader),
                $"Игрок \"{user.playerClient.userName}\" вступил в вашу группу!");
            InviteRequests.Remove(user.userID);
            SavePluginData();
            return true;
        }

        private bool PartyMembers(NetUser user)
        {
            var party = GetPartyByLeader(user.userID);
            if (party == null)
            {
                party = GetPartyByUserID(user.userID);
                if (party == null)
                {
                    SendMessage(user, "Вы не состоите ни в одной из групп!");
                    return false;
                }
            }

            var membersCount = 0;
            if (party.Members != null) membersCount = party.Members.Count;
            SendMessage(user, $"Список участников группы \"{party.Name}\" ({membersCount}):");
            SendMessage(user, $"Лидер: \"{Users.GetBySteamID(party.Leader).Username}\"");
            if (party.Members == null) return false;
            foreach (var memberID in party.Members) SendMessage(user, Users.GetBySteamID(memberID).Username);
            return true;
        }

        private void PartyList(NetUser user)
        {
            SendMessage(user, "Список всех групп:");
            foreach (var party in _parties)
                SendMessage(user, $"Группа: {party.Name}. Лидер: {Users.GetBySteamID(party.Leader).Username}");
        }

        #endregion

        #region [VOID] -> Методы. Действия с данными и уроном.

        private object ModifyDamage(TakeDamage takedamage, DamageEvent damage)
        {
            try
            {
                if (damage.attacker.client == null || damage.victim.client == null) return null;
                if (damage.attacker.client != null && damage.attacker.client != damage.victim.client &&
                    damage.victim.client != null && damage.damageTypes != DamageTypeFlags.damage_explosion &&
                    damage.attacker.client.rootControllable.idMain.GetComponent<Inventory>().activeItem.datablock.name
                        .Contains("Shotgun")) return null;
                if (damage.attacker.client == null || damage.attacker.client == damage.victim.client ||
                    damage.victim.client == null || damage.damageTypes == DamageTypeFlags.damage_explosion) return null;
                var attackerUser = damage.attacker.client.netUser;
                var victimUser = damage.victim.client.netUser;
                var attackerParty = GetPartyByLeader(attackerUser.userID) ?? GetPartyByUserID(attackerUser.userID);
                var victimParty = GetPartyByLeader(victimUser.userID) ?? GetPartyByUserID(victimUser.userID);
                if (attackerParty == null || victimParty == null) return null;
                if (attackerParty.Name != victimParty.Name) return null;
                damage.amount = 0f;
                return damage;
            }
            catch
            {
                return null;
            }
        }
        private void Loaded()
        {
            _parties = Interface.GetMod().DataFileSystem.ReadObject<List<Party>>("PartyData");
        }
        private void OnServerSave()
        {
            SavePluginData();
        }
        private static void SavePluginData()
        {
            Interface.GetMod().DataFileSystem.WriteObject("PartyData", _parties);
        }

        #endregion

        #region [VOID] -> Методы. Действия с игроком. 
        private void SendRequest(ulong invitedID, Party party)
        {
            InviteRequests.Add(invitedID, party);
            var leaderUserName = NetUser.FindByUserID(party.Leader).playerClient.userName;
            var invitedUser = NetUser.FindByUserID(invitedID);
            SendMessage(invitedUser, $"Игрок \"{leaderUserName}\" пригласил Вас в группу \"{party.Name}\"!");
            SendMessage(invitedUser, "Введите \"/party accept\" в течении 20 секунд для того чтобы принять запрос!");
            timer.Once(20f, () =>
            {
                if (!InviteRequests.ContainsKey(invitedID)) return;
                InviteRequests.Remove(invitedID);
                SendMessage(invitedUser, "Вы не успели принять запрос в группу!");
            });
        }
        private void SendMessage(NetUser user, string message)
        {
            rust.SendChatMessage(user, "Party", $"[COLOR # FA8072]{message}");
        }

        #endregion
    }
}