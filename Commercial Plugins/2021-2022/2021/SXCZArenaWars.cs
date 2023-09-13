using RustExtended;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    using Rand = Core.Random;
    [Info("SXCZArenaWars", "systemXcrackedZ", "1.0.0")]
    internal class SXCZArenaWars : RustLegacyPlugin
    {
        #region [LIST] -> [Списки]
        private List<ulong> Join = new List<ulong>(); // Игроки на арене
        private List<string> Points = new List<string>(); // Точки
        #endregion
        #region [STRING] -> [Строки]
        private string cName = RustExtended.Core.ServerName; // Название плагина в чате (выдаёт название сервера)
        private string[] AdminCommands = // Список админ-команд
        {
            "Список доступных команд администратора:",
            "/arena point - создать поинт респавна."
        };
        private string[] AvailableCommands = // Список всех команд
        {
            "Список доступных команд:",
            "/arena join - присоединиться к арене.",
            "/arena leave - выйти с арены."
        };
        #endregion
        #region [DICTIONARY] -> [Словари]
        private static Dictionary<ulong, Inventory.Transfer[]> KeepInventory = new Dictionary<ulong, Inventory.Transfer[]>(); // Сохранение инвентаря игроков
        private static Dictionary<ulong, Vector3> KeepPosition = new Dictionary<ulong, Vector3>(); // Сохранение позиции игроков
        private static Dictionary<int, Loot> ArenaLoot = new Dictionary<int, Loot>() // Лут для арены
        {           // Название оружия  // Название патронов  // Количество патронов
            { 0, new Loot { Gun = "M4", AmmoType = "556 Ammo", Ammo = 250 } },
            { 1, new Loot { Gun = "Bolt Action Rifle", AmmoType = "556 Ammo", Ammo = 100 } },
            //
            { 2, new Loot { Gun = "P250", AmmoType = "9mm Ammo", Ammo = 250 } },
            { 3, new Loot { Gun = "MP5A4", AmmoType = "9mm Ammo", Ammo = 250 } },
            //
            { 4, new Loot { Gun = "Pipe Shotgun", AmmoType = "Handmade Shell", Ammo = 10 } }
        }; // Лут для арены
        #endregion

        // by systemXcrackedZ

        #region [CLASS] Loot -> [Класс] Лут
        class Loot
        {
            public string Gun;
            public string AmmoType;
            public int Ammo;
        }
        #endregion

        // by systemXcrackedZ

        #region [CMD] Arena -> [Команды] Арена
        [ChatCommand("a")] // Сокращение /a для команды /arena
        void ON_CMD_AliasArena(NetUser user, string cmd, string[] args)
        {
            ON_CMD_Arena(user, cmd, args);
        }
        [ChatCommand("arena")] // Команда /arena
        void ON_CMD_Arena(NetUser user, string cmd, string[] args)
        {
            if (args.Length == 0)
            {
                if (user.CanAdmin())
                {
                    foreach (string adminmsg in AdminCommands)
                        SCM(user, adminmsg);
                }
                AvailableSend(user);
                return;
            }
            switch(args[0])
            {
                case "point":
                    if (!user.CanAdmin()) { AvailableSend(user); }
                    AddPoint(user.playerClient.lastKnownPosition);
                    SCM(user, "Вы успешно установили точку для арены.");
                    return;
                case "join":
                    JoinPlayer(user);
                    return;
                case "leave":
                    LeavePlayer(user);
                    return;
            }
        }
        #endregion

        // by systemXcrackedZ

        #region [VOID] Player Arena Actions -> [Методы] Действия игрока с ареной
        void AddPoint(Vector3 pos) // Создание точки арены
        {
            Points.Add(pos.ToString());
            SaveData();
        }
        void JoinPlayer(NetUser user) // Вход на арену
        {
            if (Joined(user.userID))
            {
                SCM(user, "Вы уже находитесь на арене.");
                return;
            }
            Inventory inv = user.playerClient.rootControllable.idMain.GetComponent<Inventory>();

            Join.Add(user.userID);
            KeepInventory.Add(user.userID, inv.GenerateOptimizedInventoryListing(Inventory.Slot.KindFlags.Armor | Inventory.Slot.KindFlags.Belt | Inventory.Slot.KindFlags.Default));
            KeepPosition.Add(user.userID, user.playerClient.lastKnownPosition);

            TeleportArena(inv, user);

            SCM(user, "Вы зашли на арену.");
        }
        void LeavePlayer(NetUser user) // Выход с арены
        {
            if (!Joined(user.userID))
            {
                SCM(user, "Вы не находитесь на арене.");
                return;
            }

            BackPlayerLoot(user);
            Helper.TeleportTo(user, KeepPosition[user.userID]);

            Join.Remove(user.userID);
            KeepInventory.Remove(user.userID);
            KeepPosition.Remove(user.userID);

            SCM(user, "Вы покинули арену.");
        }
        void TeleportArena(Inventory inv, NetUser user)
        {
            Helper.TeleportTo(user, StringToVector3(Points.ElementAt(Rand.Range(0, Points.Count))));
            LootAddPlayer(inv);
        }
        void OnPlayerConnected(NetUser user)
        {
            if (Join.Contains(user.userID))
            {
                timer.Once(20f, () =>
                {
                    BackPlayerLoot(user);
                    Helper.TeleportTo(user, KeepPosition[user.userID]);

                    Join.Remove(user.userID);
                    KeepInventory.Remove(user.userID);
                    KeepPosition.Remove(user.userID);
                });
            }
        }
        #endregion
        #region [VOID] Player Loot Actions -> [Методы] Действия с лутом игрока
        void BackPlayerLoot(NetUser user) // Возвращение лута при выходе с арены
        {
            Inventory inv = user.playerClient.rootControllable.idMain.GetComponent<Inventory>();
            inv.Clear();

            for (int i = 0; i < KeepInventory[user.userID].Length; i++)
            {
                IInventoryItem IItem = inv.AddItem(ref KeepInventory[user.userID][i].addition);
                inv.MoveItemAtSlotToEmptySlot(inv, IItem.slot, KeepInventory[user.userID][i].item.slot);
            }

            KeepInventory.Remove(user.userID);
        }
        void LootAddPlayer(Inventory inventory) // Выдача лута при вход на арену
        {
            int result = Rand.Range(1, ArenaLoot.Count);

            inventory.Clear();

            inventory.AddItemSomehow(DatablockDictionary.GetByName("Leather Helmet"), Inventory.Slot.Kind.Armor, 35, 1);
            inventory.AddItemSomehow(DatablockDictionary.GetByName("Leather Vest"), Inventory.Slot.Kind.Armor, 36, 1);
            inventory.AddItemSomehow(DatablockDictionary.GetByName("Leather Pants"), Inventory.Slot.Kind.Armor, 37, 1);
            inventory.AddItemSomehow(DatablockDictionary.GetByName("Leather Boots"), Inventory.Slot.Kind.Armor, 38, 1);
            //
            inventory.AddItemSomehow(DatablockDictionary.GetByName(ArenaLoot[result].Gun), Inventory.Slot.Kind.Belt, 30, 1);
            inventory.AddItemSomehow(DatablockDictionary.GetByName(ArenaLoot[result].AmmoType), Inventory.Slot.Kind.Default, 0, ArenaLoot[result].Ammo);
            //
            inventory.AddItemSomehow(DatablockDictionary.GetByName("Large Medkit"), Inventory.Slot.Kind.Belt, 34, 5);
            inventory.AddItemSomehow(DatablockDictionary.GetByName("Large Medkit"), Inventory.Slot.Kind.Belt, 35, 5);
            //
            inventory.AddItemSomehow(DatablockDictionary.GetByName("Silencer"), Inventory.Slot.Kind.Default, 24, 1);
            inventory.AddItemSomehow(DatablockDictionary.GetByName("Holo sight"), Inventory.Slot.Kind.Default, 25, 1);
            inventory.AddItemSomehow(DatablockDictionary.GetByName("Flashlight Mod"), Inventory.Slot.Kind.Default, 26, 1);
            inventory.AddItemSomehow(DatablockDictionary.GetByName("Laser Sight"), Inventory.Slot.Kind.Default, 27, 1);
        }
        #endregion
        #region [VOID] Simplification -> [Методы] Упрощения
        void SCM(NetUser user, string msg)
        {
            rust.SendChatMessage(user, cName, msg);
        }
        void AvailableSend(NetUser user)
        {
            foreach (string msg in AvailableCommands)
            {
                SCM(user, msg);
            }
        }
        #endregion
        #region [VOID] Plugin Actions -> [Методы] Действия плагина
        [HookMethod("OnPlayerSpawn")]
        void OnPlayerSpawn(PlayerClient player)
        {
            if (Join.Contains(player.userID))
            {
                Inventory inv = player.rootControllable.idMain.GetComponent<Inventory>();
                TeleportArena(inv, player.netUser);
            }
        }
        void OnKilled(TakeDamage takeDamage, DamageEvent damage)
        {
            try
            {
                if (Join.Contains(damage.victim.userID))
                {
                    if (damage.victim.client.controllable.character.controller is HumanController)
                    {
                        Inventory inventory;
                        Inventory inv = damage.victim.client.rootControllable.idMain.GetComponent<Inventory>();
                        if (DropHelper.DropInventoryContents(inv, out inventory))
                        {
                            LootableObject lootable = inventory.GetComponent<LootableObject>();
                            if (lootable != null) lootable.lifeTime = 1f;
                        }
                    }
                }
            }
            catch { }
        }
        void Loaded()
        {
            Points = Core.Interface.GetMod().DataFileSystem.ReadObject<List<string>>("ArenaData");
        }
        void OnServerSave()
        {
            SaveData();
        }
        void SaveData()
        {
            Core.Interface.GetMod().DataFileSystem.WriteObject("ArenaData", Points);
        }
        #endregion

        // by systemXcrackedZ

        #region [BOOL] Check -> [Логика] Проверки
        private bool Joined(ulong uID)
        {
            if (Join.Contains(uID))
            {
                return true;
            }
            return false;
        }
        #endregion

        #region [CONVERT] -> [Конвертация]
        public static Vector3 StringToVector3(string sVector)
        {
            if (sVector.StartsWith("(") && sVector.EndsWith(")")) sVector = sVector.Substring(1, sVector.Length - 2);
            var sArray = sVector.Split(',');
            var result = new Vector3(
                float.Parse(sArray[0]),
                float.Parse(sArray[1]),
                float.Parse(sArray[2]));
            return result;
        }
        #endregion
    }
}