using System.Collections.Generic;
using RustExtended;
using UnityEngine;
using System;

namespace Oxide.Plugins
{
    [Info("SXCZRaidAnnouncer", "systemXcrackedZ", "1.0.0")]
    internal class SXCZRaidAnnouncer : RustLegacyPlugin
    {
        private string Reg_MSG = "";
        private string Raid_MSG = "";
        private string username = "";

        private const string cName = "Raid Announcer";
        private const string TOKEN = "9a55d1a9f071f24ea2fd27e1977216c0bc07563165b088b05230a3af8719fc96767626c993bdbf56c4f68";

        private int user_id = 0;

        class UserRegister
        {
            public ulong uID;
            public int uVK;
        }
        private List<UserRegister> registered;

        UserRegister GetRegister(ulong uID)
        {
            return registered.Find(f => f.uID == uID);
        }

        void Loaded()
        {
            registered = Core.Interface.GetMod().DataFileSystem.ReadObject<List<UserRegister>>("RaidAnnouncerData");
        }
        void OnServerSave()
        {
            SaveData();
        }
        void SaveData()
        {
            Core.Interface.GetMod().DataFileSystem.WriteObject("RaidAnnouncerData", registered);
        }

        [ChatCommand("raidunreg")]
        void CMD_RaidUnregister(NetUser user, string cmd, string[] args)
        {
            if (GetRegister(user.userID) == null)
            {
                rust.SendChatMessage(user, cName, "Вы не зарегистрированы.");
                return;
            }
            registered.Remove(GetRegister(user.userID));
            rust.SendChatMessage(user, cName, "Вы успешно удалили регистрацию.");
            SaveData();
        }
        [ChatCommand("raidreg")]
        void CMD_RaidRegister(NetUser user, string cmd, string[] args)
        {
            if (GetRegister(user.userID) != null)
            {
                rust.SendChatMessage(user, cName, "Вы уже зарегистрированы.");
                rust.SendChatMessage(user, cName, "Если вы не регистрировали ВК - напишите от этом в группу сервера.");
                return;
            }
            if (args.Length == 0)
            {
                rust.SendChatMessage(user, cName, "Вам необходимо отправить любое сообщение в ЛС группы для корректной работы и");
                rust.SendChatMessage(user, cName, "указать свой айди в VK. Получить его можно на сайте https://regvk.com/id/");
                return;
            }
            int z;
            if (!int.TryParse(args[0], out z))
            {
                rust.SendChatMessage(user, cName, "Вам необходимо отправить любое сообщение в ЛС группы для корректной работы и");
                rust.SendChatMessage(user, cName, "указать свой айди в VK. Получить его можно на сайте https://regvk.com/id/");
                return;
            }
            if (registered.Find(f => f.uVK == int.Parse(args[0])) != null)
            {
                rust.SendChatMessage(user, cName, "Указанный ВК уже зарегистрирован.");
                rust.SendChatMessage(user, cName, "Если вы не регистрировали ВК - напишите от этом в группу сервера.");
                return;
            }
            username = Users.GetBySteamID(user.userID).Username;
            user_id = int.Parse(args[0]);
            Reg_MSG = "Зарегистрирован.";
            SendRegisterMessage();
        }

        private void OnKilled(TakeDamage damage, DamageEvent evt)
        {
            if (evt.victim.idMain == null || evt.attacker.idMain == null) return;
            var structure = evt.victim.idMain as StructureComponent;
            var attackerNetUser = evt.attacker.client.netUser;
            if (structure != null && attackerNetUser != null && !attackerNetUser.admin && evt.damageTypes == DamageTypeFlags.damage_explosion)
            {
                if (structure._master.ownerID == attackerNetUser.userID) return;
                user_id = GetRegister(structure._master.ownerID).uVK;
                Raid_MSG = string.Format("[ОПОВЕЩЕНИЕ]: Внимание, Вас рейдят! Ник рейдера: {0}", Users.GetBySteamID(attackerNetUser.userID).Username);
                SendRaidMessage();
            }
        }

        void SendRegisterMessage()
        {
            webrequest.EnqueueGet("https://api.vk.com/method/messages.send?user_id=" + user_id + $"&message=\"{Reg_MSG}\"&access_token={TOKEN}&v=5.73", (code, response) => GetRegisterCallback(code, response), this);
        }
        void SendRaidMessage()
        {
            webrequest.EnqueueGet("https://api.vk.com/method/messages.send?user_id=" + user_id + $"&message=\"{Raid_MSG}\"&access_token={TOKEN}&v=5.73", (code, response) => GetCallback(code, response), this);
        }

        void GetRegisterCallback(int code, string response)
        {
            NetUser user = NetUser.FindByUserID(Users.GetByUserName(username).SteamID); if (user == null) return;
            if (response == null || code != 200)
            {
                rust.SendChatMessage(user, cName, "Вам необходимо отправить любое сообщение в ЛС группы для корректной работы и");
                rust.SendChatMessage(user, cName, "указать свой айди в VK. Получить его можно на сайте https://regvk.com/id/");
                return;
            }
            if (response != null || code == 200)
            {
                registered.Add(new UserRegister
                {
                    uID = user.userID,
                    uVK = user_id
                });
                rust.SendChatMessage(user, cName, "Вы успешно зарегистрировались.");
                rust.SendChatMessage(user, cName, "Если вам не пришло сообщение \"Зарегистрирован\" от группы,");
                rust.SendChatMessage(user, cName, "введите /raidunreg и зарегистрируйтесь заново.");
                rust.SendChatMessage(user, cName, "Вам необходимо отправить любое сообщение в ЛС группы для корректной работы и");
                rust.SendChatMessage(user, cName, "указать свой айди в VK. Получить его можно на сайте https://regvk.com/id/");
                SaveData();
            }
        }
        void GetCallback(int code, string response)
        {
            if (response == null || code != 200)
            {
                return;
            }
        }
    }
}