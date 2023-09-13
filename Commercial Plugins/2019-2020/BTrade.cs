using RustExtended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BTrade", "systemXcrackedZ", "1.2.2")]
    internal class BTrade : RustLegacyPlugin
    {
        private Dictionary<ulong, ulong> tradeRequests = new Dictionary<ulong, ulong>();

        public class Trade
        {
            public ulong SelfID { get; set; }
            public bool SelfState = false;
            public Dictionary<string, int> SelfItems { get; set; }

            public ulong PlayerID { get; set; }
            public bool PlayerState = false;
            public Dictionary<string, int> PlayerItems { get; set; }
        }
        public static List<Trade> Trades = new List<Trade>();

        public static Trade GetTrade(ulong steamID) =>
            Trades.Find(f => f.PlayerID == steamID || f.SelfID == steamID);

        [ChatCommand("trade")]
        private void CMD_Trade(NetUser user, string cmd, string[] args)
        {
            if (args.Length == 0)
            {
                rust.SendChatMessage(user, "Trade", "[COLOR#FFFFFF]/trade [COLOR#FF7433]\"ник\"");
                rust.SendChatMessage(user, "Trade", "[COLOR#FFFFFF]Например: /trade [COLOR#FF7433]Papiros");
                return;
            }

            var player = Helper.GetPlayerClient(args[0]);
            if (player == null)
            {
                rust.SendChatMessage(user, "Trade", $"[COLOR#FFFFFF]Игрок с ник-неймом [COLOR#FF7433]\"{args[0]}\" [COLOR#FFFFFF]не найден!");
                rust.SendChatMessage(user, "Trade", "[COLOR#FFFFFF]/trade [COLOR#FF7433]\"ник\"");
                rust.SendChatMessage(user, "Trade", "[COLOR#FFFFFF]Пример: /trade [COLOR#FF7433]Lavandos");
                return;
            }

            if (tradeRequests.ContainsValue(user.userID) || tradeRequests.ContainsKey(user.userID))
            {
                rust.SendChatMessage(user, "Trade", $"[COLOR#FFFFFF]Вы уже содержите запрос на трейд!");
                rust.SendChatMessage(user, "Trade", "[COLOR#FFFFFF]/trade [COLOR#FF7433]\"ник\"");
                rust.SendChatMessage(user, "Trade", "[COLOR#FFFFFF]Пример: /trade [COLOR#FF7433]Lavandos");
                return;
            }
            if (GetTrade(user.userID) != null)
            {
                rust.SendChatMessage(user, "Trade", $"[COLOR#FFFFFF]Вы уже обмениваетесь!");
                rust.SendChatMessage(user, "Trade", "[COLOR#FFFFFF]/trade [COLOR#FF7433]\"ник\"");
                rust.SendChatMessage(user, "Trade", "[COLOR#FFFFFF]Пример: /trade [COLOR#FF7433]Lavandos");
                return;
            }

            if (tradeRequests.ContainsValue(player.userID) || tradeRequests.ContainsKey(player.userID))
            {
                rust.SendChatMessage(user, "Trade", $"[COLOR#FFFFFF][COLOR#FFFFFF]Игрок с ник-неймом [COLOR#FF7433]\"{args[0]}\" [COLOR#FFFFFF] уже содержит запрос на трейд!");
                rust.SendChatMessage(user, "Trade", "[COLOR#FFFFFF]/trade [COLOR#FF7433]\"ник\"");
                rust.SendChatMessage(user, "Trade", "[COLOR#FFFFFF]Пример: /trade [COLOR#FF7433]Lavandos");
                return;
            }
            if (GetTrade(player.userID) != null)
            {
                rust.SendChatMessage(user, "Trade", $"[COLOR#FFFFFF]Игрок с ник-неймом [COLOR#FF7433]\"{args[0]}\" [COLOR#FFFFFF] уже обменивается!");
                rust.SendChatMessage(user, "Trade", "[COLOR#FFFFFF]/trade [COLOR#FF7433]\"ник\"");
                rust.SendChatMessage(user, "Trade", "[COLOR#FFFFFF]Пример: /trade [COLOR#FF7433]Lavandos");
                return;
            }

            tradeRequests.Add(user.userID, player.userID);

            rust.SendChatMessage(user, $"[color #FFFFFF]Вы отправили запрос на обмен игроку [color #05e0ff]{Users.GetUsername(player.userID)}.");

            rust.SendChatMessage(player.netUser, $"[color #FFFFFF]Вы получили запрос на обмен от [color #05e0ff]{Users.GetUsername(user.userID)}.");
            rust.SendChatMessage(player.netUser, "[color #FF7433]/tradeaccept [color #FFFFFF]- принять, [color #FF7433]/tradecancel [color #FFFFFF]- отклонить");
        }

        [ChatCommand("tradeaccept")]
        private void CMD_TradeAccept(NetUser user, string cmd, string[] args)
        {
            if (!tradeRequests.ContainsValue(user.userID))
            {
                rust.SendChatMessage(user, "Trade", $"[COLOR#FFFFFF]Вы не имеете запроса на трейд!");
                rust.SendChatMessage(user, "Trade", "[COLOR#FFFFFF]/trade [COLOR#FF7433]\"ник\"");
                rust.SendChatMessage(user, "Trade", "[COLOR#FFFFFF]Пример: /trade [COLOR#FF7433]Lavandos");
                return;
            }

            var request = tradeRequests.First(f => f.Value == user.userID);
            var player = Helper.GetPlayerClient(request.Key);
            if (player == null)
            {
                rust.SendChatMessage(user, "Trade", $"[COLOR#FFFFFF]Игрок покинул игру.");
                rust.SendChatMessage(user, "Trade", "[COLOR#FFFFFF]/trade [COLOR#FF7433]\"ник\"");
                rust.SendChatMessage(user, "Trade", "[COLOR#FFFFFF]Пример: /trade [COLOR#FF7433]Lavandos");

                tradeRequests.Remove(request.Key);
                return;
            }

            rust.SendChatMessage(user, string.Format($"[COLOR #FF7433]{player.userName} [COLOR#FFFFFF]принял ваш запрос на обмен."));
            rust.SendChatMessage(player.netUser, string.Format($"[COLOR #05e0ff]{user.displayName} [COLOR#FFFFFF]принял ваш запрос на обмен."));

            Trades.Add(new Trade
            {
                SelfID = user.userID,
                SelfItems = new Dictionary<string, int>(),
                PlayerID = player.userID,
                PlayerItems = new Dictionary<string, int>()
            });
            tradeRequests.Remove(request.Key);

            user.playerClient.networkView.RPC("StartTrade", user.playerClient.netPlayer);
            player.networkView.RPC("StartTrade", player.netPlayer);
        }

        [ChatCommand("tradecancel")]
        private void CMD_TradeCancel(NetUser user, string cmd, string[] args)
        {
            var trade = GetTrade(user.userID);
            if (trade != null)
            {
                Helper.GetPlayerClient(trade.SelfID)?.gameObject.GetComponent<TradeVM>()?.SendRPC("CancelTrade");
                Helper.GetPlayerClient(trade.PlayerID)?.gameObject.GetComponent<TradeVM>()?.SendRPC("CancelTrade");

                Trades.Remove(trade);
                return;
            }

            if (!tradeRequests.ContainsValue(user.userID))
            {
                rust.SendChatMessage(user, "Trade", $"[COLOR#FFFFFF]Вы не имеете запроса на трейд!");
                rust.SendChatMessage(user, "Trade", "[COLOR#FFFFFF]/trade [COLOR#FF7433]\"ник\"");
                rust.SendChatMessage(user, "Trade", "[COLOR#FFFFFF]Пример: /trade [COLOR#FF7433]Lavandos");
                return;
            }

            var request = tradeRequests.First(f => f.Value == user.userID);
            var player = Helper.GetPlayerClient(request.Key);
            if (player != null) rust.SendChatMessage(player.netUser, $"[COLOR #FF7433]{Users.GetUsername(user.userID)} [COLOR#FFFFFF]отклонил ваш запрос.");

            tradeRequests.Remove(request.Key);

            rust.SendChatMessage(user, $"[COLOR#FFFFFF]Вы отменили запрос на обмен от [COLOR #FF7433]{Users.GetUsername(player.userID)}");
        }

        public static List<ulong> playersWithPlugin = new List<ulong>();

        void AddPluginToPlayer(PlayerClient pc)
        {
            if (pc.gameObject.GetComponent<TradeVM>() == null)
            {
                if (playersWithPlugin.Contains(pc.userID)) playersWithPlugin.Remove(pc.userID);

                var receiver = pc.gameObject.AddComponent<TradeVM>();
                receiver.playerClient = pc;

                playersWithPlugin.Add(pc.userID);
            }
        }

        void OnPlayerConnected(NetUser netUser)
        {
            if (netUser.playerClient != null)
            {
                AddPluginToPlayer(netUser.playerClient);
            }
        }

        void OnPlayerDisconnected(uLink.NetworkPlayer networkPlayer)
        {
            foreach (ulong _steamID in playersWithPlugin)
            {
                PlayerClient pclient;
                PlayerClient.FindByUserID(_steamID, out pclient);
                if (pclient == null || pclient.netPlayer == networkPlayer)
                {
                    playersWithPlugin.Remove(_steamID);
                    break;
                }

                var trade = GetTrade(_steamID);
                if (trade != null)
                {
                    Helper.GetPlayerClient(trade.SelfID)?.gameObject.GetComponent<TradeVM>()?.SendRPC("CancelTrade");
                    Helper.GetPlayerClient(trade.PlayerID)?.gameObject.GetComponent<TradeVM>()?.SendRPC("CancelTrade");

                    Trades.Remove(trade);
                }
            }
        }

        void UnloadPlugin(GameObject obj, Type plugin)
        {
            if (obj.GetComponent(plugin) != null)
                UnityEngine.Object.Destroy(obj.GetComponent(plugin));
        }

        void Unload()
        {
            foreach (var _steamID in playersWithPlugin)
            {
                PlayerClient pclient;
                PlayerClient.FindByUserID(_steamID, out pclient);
                if (pclient != null)
                {
                    UnloadPlugin(pclient.gameObject, typeof(TradeVM));
                }
            }
        }

        public class TradeVM : MonoBehaviour
        {
            public PlayerClient playerClient;

            [RPC]
            public void ChangeSelfState(bool state)
            {
                var trade = GetTrade(playerClient.userID);
                var steamID = playerClient.userID == trade.SelfID ? trade.PlayerID : trade.SelfID;

                if (playerClient.userID == trade.PlayerID) trade.PlayerState = state;
                if (playerClient.userID == trade.SelfID) trade.SelfState = state;

                var player = Helper.GetPlayerClient(steamID);
                player.networkView.RPC("SetPlayerState", player.netPlayer, state);
            }

            [RPC]
            public void AddTradeItem(string item)
            {
                var trade = GetTrade(playerClient.userID);
                var steamID = playerClient.userID == trade.SelfID ? trade.PlayerID : trade.SelfID;

                if (playerClient.userID == trade.PlayerID)
                {
                    if (trade.PlayerItems.ContainsKey(item)) trade.PlayerItems[item]++;
                    else trade.PlayerItems.Add(item, 1);
                }
                if (playerClient.userID == trade.SelfID)
                {
                    if (trade.SelfItems.ContainsKey(item)) trade.SelfItems[item]++;
                    else trade.SelfItems.Add(item, 1);
                }

                var player = Helper.GetPlayerClient(steamID); 
                player.networkView.RPC("AddPlayerItem", player.netPlayer, item);
            }
            [RPC]
            public void RemoveTradeItem(string item)
            {
                var trade = GetTrade(playerClient.userID);
                var steamID = playerClient.userID == trade.SelfID ? trade.PlayerID : trade.SelfID;

                if (playerClient.userID == trade.PlayerID)
                {
                    trade.PlayerItems[item]--;
                    if (trade.PlayerItems[item] <= 0) trade.PlayerItems.Remove(item);
                }
                if (playerClient.userID == trade.SelfID)
                {
                    trade.SelfItems[item]--;
                    if (trade.SelfItems[item] <= 0) trade.SelfItems.Remove(item);
                }

                var player = Helper.GetPlayerClient(steamID);
                player.networkView.RPC("RemovePlayerItem", player.netPlayer, item);
            }

            [RPC]
            public void DoTrade()
            {
                var trade = GetTrade(playerClient.userID);
                if (trade.SelfState == false || trade.PlayerState == false)
                {
                    Broadcast.Message(playerClient.netUser, "Необходимо, чтобы все игроки были согласны на обмен!", "Trade");
                    return;
                }

                var inv1 = Helper.GetPlayerClient(trade.SelfID).rootControllable.idMain.GetComponent<Inventory>();
                if (inv1 == null) return;

                var inv2 = Helper.GetPlayerClient(trade.PlayerID).rootControllable.idMain.GetComponent<Inventory>();
                if (inv2 == null) return;

                foreach (var item in trade.SelfItems)
                {
                    if (Helper.InventoryItemCount(inv1, DatablockDictionary.GetByName(item.Key)) < item.Value)
                        return;
                }

                foreach (var item in trade.PlayerItems)
                {
                    if (Helper.InventoryItemCount(inv2, DatablockDictionary.GetByName(item.Key)) < item.Value)
                        return;
                }

                foreach (var item in trade.SelfItems)
                {
                    Helper.InventoryItemRemove(inv1, DatablockDictionary.GetByName(item.Key), item.Value);
                    Helper.GiveItem(Helper.GetPlayerClient(trade.PlayerID), item.Key, item.Value);
                }

                foreach (var item in trade.PlayerItems)
                {
                    Helper.InventoryItemRemove(inv2, DatablockDictionary.GetByName(item.Key), item.Value);
                    Helper.GiveItem(Helper.GetPlayerClient(trade.SelfID), item.Key, item.Value);
                }

                Broadcast.Message(Helper.GetPlayerClient(trade.SelfID).netUser, "Трейд успешно прошёл!", "Trade");
                Broadcast.Message(Helper.GetPlayerClient(trade.PlayerID).netUser, "Трейд успешно прошёл!", "Trade");

                Trades.Remove(trade);

                Helper.GetPlayerClient(trade.SelfID).networkView.RPC("CancelTrade", Helper.GetPlayerClient(trade.SelfID).netPlayer);
                Helper.GetPlayerClient(trade.PlayerID).networkView.RPC("CancelTrade", Helper.GetPlayerClient(trade.PlayerID).netPlayer);
            }

            public void SendRPC(string name, params object[] args)
            {
                playerClient.networkView.RPC(name, playerClient.netPlayer, args);
            }
        }
    }
}
