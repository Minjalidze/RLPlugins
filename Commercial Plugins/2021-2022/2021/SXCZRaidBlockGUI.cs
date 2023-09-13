using System.Collections.Generic;
using RustExtended;
using UnityEngine;
using RageMods;
using System;

namespace Oxide.Plugins
{
    [Info("SXCZRaidBlockGUI", "systemXcrackedZ", "1.0.0")]
    internal class SXCZRaidBlockGUI : RustLegacyPlugin
    {
        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~MAIN~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//

        #region [STRINGs] -> [Строки]
        private readonly string cName = "RaidBlock";
        #endregion
        //
        #region [FLOATs] -> [Плавающие числа]
        private readonly float RaidDuration = 420f;
        private readonly Dictionary<ulong, DateTime> BlockedPlayers = new Dictionary<ulong, DateTime>();
        #endregion

        //---------------------------------------------------------------------//

        #region [VOID] Command Execute Handler -> [Метод] Обработчик Выполнения Команд
        private void CommandExecutor(NetUser user, string cmd, string[] args)
        {
            if (BlockedPlayers.ContainsKey(user.userID))
            {
                rust.SendChatMessage(user, cName, $"Команда недоступна ещё {(int)(BlockedPlayers[user.userID] - DateTime.Now).TotalSeconds} секунд. Использованная команда: \"/{cmd}\"");
                return;
            }
            switch (cmd)
            {
                case "home":
                    Commands.Home(user, Users.GetBySteamID(user.userID), cmd, args);
                    break;
                case "clan":
                    Commands.Clan(user, Users.GetBySteamID(user.userID), cmd, args);
                    break;
                case "tp":
                    Commands.Teleport(user, Users.GetBySteamID(user.userID), cmd, args);
                    break;
                case "buy":
                    Economy.ShopBuy(user, Users.GetBySteamID(user.userID), cmd, args);
                    break;
                case "sell":
                    Economy.ShopSell(user, Users.GetBySteamID(user.userID), cmd, args);
                    break;
                case "destroy":
                    Commands.Destroy(user, Users.GetBySteamID(user.userID), cmd, args);
                    break;
                case "shop":
                    Economy.ShopList(user, Users.GetBySteamID(user.userID), cmd, args);
                    break;
                case "transfer":
                    Commands.Transfer(user, Users.GetBySteamID(user.userID), cmd, args);
                    break;
                case "send":
                    Economy.Send(user, Users.GetBySteamID(user.userID), cmd, args);
                    break;
            }
        }
        #endregion
        //
        #region [VOIDs] RaidBlock Actions -> [Методы] Действия с РейдБлоком
        private void DrawRaidTimer(ulong uID)
        {
            PlayerMods pMod = PlayerMods.GetPlayerMods(uID);

            int CurrentTime = (int)(BlockedPlayers[uID] - DateTime.Now).TotalSeconds;

            float sWidth = pMod.ScreenWidth;
            float sHeight = pMod.ScreenHeight;

            Rect WindowPosition =
                new Rect(sWidth / 1.15f, sHeight / 1.3f, sWidth / 8.847f, sHeight / 11.489f);
            Rect LabelPosition =
                new Rect(sWidth / 1.13f, sHeight / 1.24f, sWidth / 12.8f, sHeight / 54f);

            rust.SendChatMessage(NetUser.FindByUserID(uID), cName, $"Вы добавлены в рейдблок из-за рейда! Осталось: {CurrentTime}");

            pMod.AddWindow(WindowPosition, "Background", "");
            pMod.AddLabel(LabelPosition, "RaidText", $"РЕЙДБЛОК: {CurrentTime} сек.");

            timer.Repeat(1f, CurrentTime, () =>
            {
                if (pMod == null) { return; }

                CurrentTime = (int)(BlockedPlayers[uID] - DateTime.Now).TotalSeconds;
                pMod.DeleteGUI("RaidText");
                pMod.AddLabel(LabelPosition, "RaidText", $"РЕЙДБЛОК: {CurrentTime} сек.");
            });
        }
        private void StartRaidBlock(ulong uID, ulong uID2)
        {
            if (BlockedPlayers.ContainsKey(uID))
                BlockedPlayers.Remove(uID);
            if (BlockedPlayers.ContainsKey(uID2))
                BlockedPlayers.Remove(uID2);

            BlockedPlayers.Add(uID,
                DateTime.Now.AddSeconds(RaidDuration));
            BlockedPlayers.Add(uID2,
                DateTime.Now.AddSeconds(RaidDuration));

            DrawRaidTimer(uID);
            DrawRaidTimer(uID2);
        }
        #endregion
        //
        #region [VOIDs] Plugin Actions -> [Методы] Действия Плагина
        void OnKilled(TakeDamage take, DamageEvent damage)
        {
            try
            {
                if (damage.victim.idMain == null || damage.attacker.idMain == null) return;

                NetUser userRaider =
                    damage.attacker.client.netUser;

                if (damage.victim.idMain is StructureComponent && damage.damageTypes == DamageTypeFlags.damage_explosion)
                {
                    var structure = damage.victim.idMain as StructureComponent;

                    ulong ownerID =
                        structure._master.ownerID;
                    ulong raiderID =
                        userRaider.userID;

                    if (BlockedPlayers.ContainsKey(raiderID) && !BlockedPlayers.ContainsKey(ownerID))
                    {
                        int CurrentTime = (int)(BlockedPlayers[raiderID] - DateTime.Now).TotalSeconds;
                        BlockedPlayers.Add(ownerID, DateTime.Now.AddSeconds(CurrentTime));
                        DrawRaidTimer(ownerID);
                        return;
                    }

                    if (BlockedPlayers.ContainsKey(ownerID))
                    {
                        if (ownerID != raiderID)
                        {
                            int CurrentTime = (int)(BlockedPlayers[ownerID] - DateTime.Now).TotalSeconds;
                            BlockedPlayers.Add(raiderID, DateTime.Now.AddSeconds(CurrentTime));
                            DrawRaidTimer(raiderID);
                        }
                    }
                }
                if (take is HumanBodyTakeDamage)
                {
                    NetUser defenderUser =
                        damage.victim.client.netUser;

                    ulong defenderID =
                        defenderUser.userID;
                    ulong raiderID =
                        userRaider.userID;

                    if (BlockedPlayers.ContainsKey(raiderID) && !BlockedPlayers.ContainsKey(defenderID))
                    {
                        int CurrentTime = (int)(BlockedPlayers[raiderID] - DateTime.Now).TotalSeconds;
                        BlockedPlayers.Add(defenderID, DateTime.Now.AddSeconds(CurrentTime));
                        DrawRaidTimer(defenderID);
                        return;
                    }

                    if (BlockedPlayers.ContainsKey(defenderID))
                    {
                        if (defenderID != raiderID)
                        {
                            int CurrentTime = (int)(BlockedPlayers[defenderID] - DateTime.Now).TotalSeconds;
                            BlockedPlayers.Add(userRaider.userID, DateTime.Now.AddSeconds(CurrentTime));
                            DrawRaidTimer(raiderID);
                        }
                    }
                }
                if (damage.victim.idMain is DeployableObject)
                {
                    var deploy = damage.victim.idMain as DeployableObject;

                    ulong ownerID =
                        deploy.ownerID;
                    ulong raiderID =
                        userRaider.userID;

                    if (BlockedPlayers.ContainsKey(raiderID) && !BlockedPlayers.ContainsKey(ownerID))
                    {
                        int CurrentTime = (int)(BlockedPlayers[raiderID] - DateTime.Now).TotalSeconds;
                        BlockedPlayers.Add(ownerID, DateTime.Now.AddSeconds(CurrentTime));
                        DrawRaidTimer(ownerID);
                        return;
                    }

                    if (BlockedPlayers.ContainsKey(ownerID))
                    {
                        if (ownerID != raiderID)
                        {
                            int CurrentTime = (int)(BlockedPlayers[ownerID] - DateTime.Now).TotalSeconds;
                            BlockedPlayers.Add(userRaider.userID, DateTime.Now.AddSeconds(CurrentTime));
                            DrawRaidTimer(raiderID);
                        }
                    }
                }
            }
            catch { }
        }
        void OnPlayerInitialized(NetUser user, PlayerMods pMod)
        {
            ulong uID =
                user.userID;

            if (BlockedPlayers.ContainsKey(uID))
            {
                DrawRaidTimer(uID);
            }
        }
        #endregion

        //---------------------------------------------------------------------//

        #region [CMDs] Blocked Commands -> [Команды] Заблокированные Команды
        [ChatCommand("home")]
        void CMD_Home(NetUser user, string cmd, string[] args)
        {
            CommandExecutor(user, cmd, args);
        }
        [ChatCommand("clan")]
        void CMD_Clan(NetUser user, string cmd, string[] args)
        {
            CommandExecutor(user, cmd, args);
        }
        [ChatCommand("tp")]
        void CMD_TP(NetUser user, string cmd, string[] args)
        {
            CommandExecutor(user, cmd, args);
        }
        [ChatCommand("buy")]
        void CMD_Buy(NetUser user, string cmd, string[] args)
        {
            CommandExecutor(user, cmd, args);
        }
        [ChatCommand("sell")]
        void CMD_Sell(NetUser user, string cmd, string[] args)
        {
            CommandExecutor(user, cmd, args);
        }
        [ChatCommand("destroy")]
        void CMD_Destroy(NetUser user, string cmd, string[] args)
        {
            CommandExecutor(user, cmd, args);
        }
        [ChatCommand("shop")]
        void CMD_Shop(NetUser user, string cmd, string[] args)
        {
            CommandExecutor(user, cmd, args);
        }
        [ChatCommand("send")]
        void CMD_Send(NetUser user, string cmd, string[] args)
        {
            CommandExecutor(user, cmd, args);
        }
        [ChatCommand("transfer")]
        void CMD_Transfer(NetUser user, string cmd, string[] args)
        {
            CommandExecutor(user, cmd, args);
        }
        #endregion

        /////////////////////////////////////////////////////////////////////////
        // Developed by: systemXcrackedZ                                       //
        // Server: Volchara Rust                                               //
        // VK: https://vk.com/sysxcrackz                                       //
        // Price: 200 rub.                                                     //
        // Использование данного плагина на других серверах без разрешения     //
        // кодера - запрещено.                                                 //
        /////////////////////////////////////////////////////////////////////////

        /////////////////////////////////////////////////////////////////////////
    }
}