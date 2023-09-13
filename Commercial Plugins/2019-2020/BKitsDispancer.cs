
using UnityEngine;
using RustExtended;
using System.Collections.Generic;

using static RustExtended.Core;

namespace Oxide.Plugins
{
    [Info("BKitDispancer", "systemXcrackedZ", "4.3.2")]
    internal class BKitsDispancer : RustLegacyPlugin
    {
        internal class PlayerKit
        {
            public PlayerKit(NetUser sender, PlayerClient playerClient, string kitName)
            {
                Sender = sender;
                PlayerClient = playerClient; 
                KitName = kitName; 
            }
            public NetUser Sender;
            public PlayerClient PlayerClient;
            public string KitName;
        }

        [ChatCommand("zkit")]
        private void OnCMD_ZoneKit(NetUser user, string cmd, string[] args)
        {
            if (!user.CanAdmin() || Users.GetBySteamID(user.userID).Rank < 41)
            {
                PlayAudio(user, "JugBottle");
                rust.SendChatMessage(user, "Жаль не взял собой рундук. Хе-хе. Сундук для рун - рундук.");
                return;
            }
            if (args.Length < 2)
            {
                rust.SendChatMessage(user, "Использование команды: /zkit <zonename> <kitname>.");
                return;
            }

            string zoneName = args[0];
            string kitName = args[1];

            if (Zones.Find(zoneName) == null)
            {
                rust.SendChatMessage(user, "Зона не найдена.");
                return;
            }

            for (int i = 0; i < PlayerClient.All.Count; i++)
            {
                PlayerClient playerClient = PlayerClient.All[i];
                if (playerClient != null && playerClient.netPlayer != null)
                {
                    WorldZone zone = Zones.Find(zoneName);
                    if (Users.GetBySteamID(playerClient.userID).Zone != null && Users.GetBySteamID(playerClient.userID).Zone.Name == zone.Name) GivePlayerKit(new PlayerKit(user, playerClient, kitName));
                }
            }
            rust.Notice(user, $"Вы выдали кит \"{kitName}\" всем игрокам в зоне \"{zoneName}\"!");
        }
        [ChatCommand("rakit")]
        private void OnCMD_RadiusKit(NetUser user, string cmd, string[] args)
        {
            if (!user.CanAdmin() || Users.GetBySteamID(user.userID).Rank < 41)
            {
                PlayAudio(user, "JugSven");
                rust.SendChatMessage(user, "Главное, Sven, не размер меча, а как ты с ним управляешься.");
                return;
            }
            if (args.Length < 2)
            {
                rust.SendChatMessage(user, "Использование команды: /rakit <radius> <kitname>.");
                return;
            }

            float radius = float.Parse(args[0]);
            string kitName = args[1];

            Collider[] colliders = Physics.OverlapSphere(user.playerClient.lastKnownPosition, radius);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];

                Player player = collider.GetComponent<Player>();
                if (player != null) GivePlayerKit(new PlayerKit(user, player.playerClient, kitName));
            }
            rust.Notice(user, $"Вы выдали кит \"{kitName}\" всем игрокам в радиусе \"{radius}\"!");
        }

        [ChatCommand("zclear")]
        private void OnCMD_ZoneClear(NetUser user, string cmd, string[] args)
        {
            if (!user.CanAdmin() || Users.GetBySteamID(user.userID).Rank < 41)
            {
                PlayAudio(user, "JugBottle");
                rust.SendChatMessage(user, "Жаль не взял собой рундук. Хе-хе. Сундук для рун - рундук.");
                return;
            }
            if (args.Length < 2)
            {
                rust.SendChatMessage(user, "Использование команды: /zclear <radius>.");
                return;
            }

            string zoneName = args[0];

            if (Zones.Find(zoneName) == null)
            {
                rust.SendChatMessage(user, "Зона не найдена.");
                return;
            }

            for (int i = 0; i < PlayerClient.All.Count; i++)
            {
                PlayerClient playerClient = PlayerClient.All[i];
                if (playerClient != null && playerClient.netPlayer != null)
                {
                    WorldZone zone = Zones.Find(zoneName);
                    if (Users.GetBySteamID(playerClient.userID).Zone != null && Users.GetBySteamID(playerClient.userID).Zone.Name == zone.Name) rust.GetInventory(playerClient.netUser).Clear();
                }
            }
            rust.Notice(user, $"Вы очистили инвентарь всем игрокам в зоне \"{zoneName}\"!");
        }
        [ChatCommand("raclear")]
        private void OnCMD_RadiusClear(NetUser user, string cmd, string[] args)
        {
            if (!user.CanAdmin() || Users.GetBySteamID(user.userID).Rank < 41)
            {
                PlayAudio(user, "JugSven");
                rust.SendChatMessage(user, "Главное, Sven, не размер меча, а как ты с ним управляешься.");
                return;
            }
            if (args.Length < 2)
            {
                rust.SendChatMessage(user, "Использование команды: /raclear <radius>.");
                return;
            }

            float radius = float.Parse(args[0]);

            Collider[] colliders = Physics.OverlapSphere(user.playerClient.lastKnownPosition, radius);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];

                Player player = collider.GetComponent<Player>();
                if (player != null) rust.GetInventory(player.playerClient.netUser).Clear();
            }
            rust.Notice(user, $"Вы очистили инвентарь всем игрокам в радиусе \"{radius}\"!");
        }

        private void PlayAudio(NetUser user, string soundTag)
        {
            
        }

        private void GivePlayerKit(PlayerKit playerKit)
        {
            List<string> KitList = (List<string>)Kits[playerKit.KitName];

            if (KitList == null || KitList.Count == 0)
            {
                rust.SendChatMessage(playerKit.Sender, "Кит не найден.");
                return;
            }

            foreach (string VAR in KitList)
            {
                if (VAR.ToLower().StartsWith("item") && VAR.Contains("="))
                {
                    string[] Item = VAR.Split('='); if (Item.Length < 2) continue;
                    string[] KitItem = Item[1].Split(','); string ItemName = KitItem[0].Trim();
                    int Amount; if (KitItem.Length > 1) { if (!int.TryParse(KitItem[1].Trim(), out Amount)) Amount = 1; } else Amount = 1;
                    int Slots; if (KitItem.Length > 2) { if (!int.TryParse(KitItem[2].Trim(), out Slots)) Slots = -1; } else Slots = -1;

                    rust.Notice(playerKit.PlayerClient.netUser, $"Вам был выдан кит \"{playerKit.KitName}\"!");

                    Helper.GiveItem(playerKit.PlayerClient, ItemName, Amount, Slots);
                }
            }
        }
    }
}
