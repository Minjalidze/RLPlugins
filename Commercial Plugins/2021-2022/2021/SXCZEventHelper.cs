using System.Collections.Generic;
using RustExtended;
using UnityEngine;
using RageMods;
using System;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    using Rand = Oxide.Core.Random;
    [Info("SXCZEventHelper", "systemXcrackedZ", "1.0.0")]
    internal class SXCZEventHelper : RustLegacyPlugin
    {
        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~MAIN~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//

        #region [Vector3]
        private Vector3 teleportPosition = new Vector3();
        #endregion
        //
        #region [BOOL] -> [Логика]
        private bool teleportStarted = false;
        private bool duel = false;
        private bool myaso = false;
        #endregion
        //
        #region [INT] -> [Целые числа]
        private int iventType;
        #endregion

        //---------------------------------------------------------------------//

        #region [CLASS] Event Items -> [Класс] Вещи для Ивентов
        public class EventItems
        {
            public string helmet;
            public string vest;
            public string pants;
            public string boots;

            public string gunname;
            public string ammotype;
            public int ammovalue;

            public string medkit;
            public int medkitvalue;

            public string eat;
            public int eatvalue;
        }
        #endregion

        //---------------------------------------------------------------------//

        #region [DICTIONARYs] -> [Словари]
        private static Dictionary<ulong, Inventory.Transfer[]> KeepInventory = new Dictionary<ulong, Inventory.Transfer[]>(); // Сохранение инвентаря игроков
        private static Dictionary<ulong, Vector3> KeepPosition = new Dictionary<ulong, Vector3>(); // Сохранение позиции игроков
        private static Dictionary<int, ulong> Winners = new Dictionary<int, ulong>(); // Сохранения списка выигравших на ивенте
        public static Dictionary<int, EventItems> Sets = new Dictionary<int, EventItems>() // Список вещей на ивенте
        {
            {1, new EventItems 
            { 
                helmet = "", vest = "", pants = "", boots = "", 
                gunname = "", ammotype = "", ammovalue = 0,
                medkit = "", medkitvalue = 0, 
                eat = "", eatvalue = 0 
            } }
        };
        #endregion
        //
        #region [LISTs] -> [Списки]
        public List<ulong> Teleported = new List<ulong>(); // Сохранение списка телепортировавшихся на ивент
        public List<ulong> StepTwo = new List<ulong>(); // Сохранение списка телепортировавшихся на ивент
        public List<ulong> StepThree = new List<ulong>(); // Сохранение списка телепортировавшихся на ивент
        public List<ulong> StepFour = new List<ulong>(); // Сохранение списка телепортировавшихся на ивент
        public List<ulong> StepFive = new List<ulong>(); // Сохранение списка телепортировавшихся на ивент
        public List<ulong> StepSix = new List<ulong>(); // Сохранение списка телепортировавшихся на ивент
        public List<ulong> StepSeven = new List<ulong>(); // Сохранение списка телепортировавшихся на ивент

        public List<ulong> Losers = new List<ulong>(); // Сохранение списка телепортировавшихся на ивент

        public List<Vector3> Points = new List<Vector3>(); // Поинты для телепорта
        #endregion

        //---------------------------------------------------------------------//

        #region [VOIDs] Event Custom Voids -> [Методы] Кастомные Методы Ивентов
        #region [VOIDs] Save User Actions -> [Методы] Действия с сохранением Игрока
        private void SaveUser(ulong uID, Inventory inventory, Vector3 position)
        {
            KeepInventory.Add(uID, inventory.GenerateOptimizedInventoryListing(Inventory.Slot.KindFlags.Armor | Inventory.Slot.KindFlags.Belt | Inventory.Slot.KindFlags.Default));
            KeepPosition.Add(uID, position);
        }
        private void RestoreUser(ulong uID)
        {
            ClearUserInventory(NetUser.FindByUserID(uID));

            Inventory inv = NetUser.FindByUserID(uID).playerClient.rootControllable.idMain.GetComponent<Inventory>();

            timer.Once(0.5f, ()=>
            {
                for (int i = 0; i < KeepInventory[uID].Length; i++)
                {
                    IInventoryItem IItem = inv.AddItem(ref KeepInventory[uID][i].addition);
                    inv.MoveItemAtSlotToEmptySlot(inv, IItem.slot, KeepInventory[uID][i].item.slot);
                }
                Helper.TeleportTo(NetUser.FindByUserID(uID), KeepPosition[uID]);
                KeepPosition.Remove(uID);
                KeepInventory.Remove(uID);
            });
        }
        #endregion

        #region [VOIDs] Event Actions -> Действия Ивента
        private void StartEvent(string eventname, float teleporttime, int settype)
        {
            foreach (PlayerClient pClients in PlayerClient.All)
            {
                NetUser users =
                    pClients.netUser;

                teleportStarted = true;
                iventType = settype;

                rust.Notice(users, $"Начался ивент \"{eventname}\", телепорт будет длиться \"{teleporttime}\" секунд.");
                rust.Notice(users, "Для телепорта нажмите ПКМ по кнопке сверху экрана, открыв инвентарь.");

                timer.Once(teleporttime, () =>
                {
                    StopEventTeleport();
                    if (duel)
                    {
                        StartDuel();
                    }
                });
            }
        }
        private void StopEventTeleport()
        {
            foreach (PlayerClient pClients in PlayerClient.All)
            {
                NetUser users =
                    pClients.netUser;

                PlayerMods pMod = PlayerMods.GetPlayerMods(users);

                teleportStarted = false;

                pMod.DeleteGUI("Button_1");

                rust.Notice(users, $"Телепорт на ивент закончился.");
                if (myaso)
                {
                    GiveEventItems(pClients);
                }
            }
        }
        private void StopEvent()
        {
            teleportPosition = new Vector3(0f, 0f);

            Teleported.Clear();
            StepTwo.Clear();
            StepThree.Clear();
            StepFour.Clear();
            StepFive.Clear();
            StepSix.Clear();
            StepSeven.Clear();

            Points.Clear();

            KeepInventory.Clear();
            KeepPosition.Clear();

            Winners.Clear();
        }
        #endregion

        #region [VOID] Draw Button -> [Метод] Показ Кнопки
        private void DrawTeleportButton(NetUser user)
        {
            PlayerMods pMod = PlayerMods.GetPlayerMods(user);

            float sWidth = pMod.ScreenWidth;
            float sHeigth = pMod.ScreenHeight;

            Rect buttonPosition = new Rect(sWidth/2.5f, 0f, sHeigth/18f, sWidth/3.56f);

            pMod.AddButton(buttonPosition, "Button_1", "Телепортироваться на ивент!", delegate { TeleportUserToEvent(user); });
        }
        #endregion

        #region [VOIDs] Inventory Actions -> [Методы] Действия с Инвентарём
        private void GiveEventItems(PlayerClient pClient)
        {
            Inventory inv = pClient.controllable.GetComponent<Inventory>();
            var Set = Sets[iventType];
            AddItem(inv, Set.helmet, 1);
            AddItem(inv, Set.vest, 1);
            AddItem(inv, Set.pants, 1);
            AddItem(inv, Set.boots, 1);
            AddItem(inv, Set.gunname, 1);
            AddItem(inv, Set.ammotype, Set.ammovalue);
            AddItem(inv, Set.medkit, Set.medkitvalue);
            AddItem(inv, Set.eat, Set.eatvalue);
        }
        private void ClearUserInventory(NetUser user)
        {
            Inventory inv = user.playerClient.controllable.GetComponent<Inventory>();

            inv.DeactivateItem();
            inv.Clear();
        }
        #endregion

        #region [VOIDs] Teleports -> [Методы] Телепорты
        private void TeleportUserToEvent(NetUser user)
        {
            SaveUser(user.userID, user.playerClient.GetComponent<Inventory>(), user.playerClient.lastKnownPosition);
            timer.Once(0.5f, () =>
            {
                Helper.TeleportTo(user, teleportPosition);
                rust.Notice(user, "Вы были телепортированы на ивент!");
                Teleported.Add(user.userID);
                ClearUserInventory(user);
                timer.Once(0.5f, () =>
                {
                    if (!duel && !myaso)
                    {
                        GiveEventItems(user.playerClient);
                    }
                });
            });
        }
        private void TeleportToPoint(NetUser user, int point)
        {
            Helper.TeleportTo(user, Points[point]);
            GiveEventItems(user.playerClient);
        }
        #endregion

        #region [VOID] Duel -> [Метод] Дуэль
        private void StartDuel()
        {
            int randomOne;
            int randomTwo;
            if (Teleported.Count <= 1)
            {
                if (StepTwo.Count <= 1)
                {
                    if (StepThree.Count <= 1)
                    {
                        if (StepFour.Count <= 1)
                        {
                            if (StepFive.Count <= 1)
                            {
                                if (StepSix.Count <= 1)
                                {
                                    if (StepSeven.Count <= 1)
                                    {
                                        randomOne = Rand.Range(0, StepSeven.Count + 1);
                                        TeleportToPoint(NetUser.FindByUserID(StepSeven[randomOne]), 0);
                                        randomTwo = Rand.Range(0, StepSeven.Count + 1);
                                        if (randomOne == randomTwo)
                                        {
                                            randomTwo = Rand.Range(0, StepSeven.Count + 1);
                                        }
                                        TeleportToPoint(NetUser.FindByUserID(StepSeven[randomTwo]), 1);
                                    }
                                }
                                randomOne = Rand.Range(0, StepSix.Count + 1);
                                TeleportToPoint(NetUser.FindByUserID(StepSix[randomOne]), 0);
                                randomTwo = Rand.Range(0, StepSix.Count + 1);
                                if (randomOne == randomTwo)
                                {
                                    randomTwo = Rand.Range(0, StepSix.Count + 1);
                                }
                                TeleportToPoint(NetUser.FindByUserID(StepSix[randomTwo]), 1);
                            }
                            randomOne = Rand.Range(0, StepFive.Count + 1);
                            TeleportToPoint(NetUser.FindByUserID(StepFive[randomOne]), 0);
                            randomTwo = Rand.Range(0, StepFive.Count + 1);
                            if (randomOne == randomTwo)
                            {
                                randomTwo = Rand.Range(0, StepFive.Count + 1);
                            }
                            TeleportToPoint(NetUser.FindByUserID(StepFive[randomTwo]), 1);
                        }
                        randomOne = Rand.Range(0, StepFour.Count + 1);
                        TeleportToPoint(NetUser.FindByUserID(StepFour[randomOne]), 0);
                        randomTwo = Rand.Range(0, StepFour.Count + 1);
                        if (randomOne == randomTwo)
                        {
                            randomTwo = Rand.Range(0, StepFour.Count + 1);
                        }
                        TeleportToPoint(NetUser.FindByUserID(StepFour[randomTwo]), 1);
                    }
                    randomOne = Rand.Range(0, StepThree.Count + 1);
                    TeleportToPoint(NetUser.FindByUserID(StepThree[randomOne]), 0);
                    randomTwo = Rand.Range(0, StepThree.Count + 1);
                    if (randomOne == randomTwo)
                    {
                        randomTwo = Rand.Range(0, StepThree.Count + 1);
                    }
                    TeleportToPoint(NetUser.FindByUserID(StepThree[randomTwo]), 1);
                }
                randomOne = Rand.Range(0, StepTwo.Count + 1);
                TeleportToPoint(NetUser.FindByUserID(StepTwo[randomOne]), 0);
                randomTwo = Rand.Range(0, StepTwo.Count + 1);
                if (randomOne == randomTwo)
                {
                    randomTwo = Rand.Range(0, StepTwo.Count + 1);
                }
                TeleportToPoint(NetUser.FindByUserID(StepTwo[randomTwo]), 1);
            }
            randomOne = Rand.Range(0, Teleported.Count + 1);
            TeleportToPoint(NetUser.FindByUserID(Teleported[randomOne]), 0);
            randomTwo = Rand.Range(0, Teleported.Count + 1);
            if (randomOne == randomTwo)
            {
                randomTwo = Rand.Range(0, Teleported.Count + 1);
            }
            TeleportToPoint(NetUser.FindByUserID(Teleported[randomTwo]), 1);
        }
        #endregion
        #endregion
        //
        #region [VOIDs] Plugin Actions -> [Методы] Действия плагинов
        void OnKilled(TakeDamage take, DamageEvent damage)
        {
            try
            {
                NetUser userAttack =
                    damage.attacker.client.netUser;
                NetUser userVictim =
                    damage.victim.client.netUser;

                if (userAttack == null || userVictim == null)
                    return;

                ulong uIDAttack =
                    userAttack.userID;
                ulong uIDVictim =
                    userVictim.userID;

                if (Teleported.Contains(uIDAttack) && Teleported.Contains(uIDVictim))
                {
                    Teleported.Remove(uIDAttack);
                    Teleported.Remove(uIDVictim);
                    StepTwo.Add(uIDAttack);
                    Losers.Add(uIDVictim);
                    Helper.TeleportTo(NetUser.FindByUserID(uIDAttack), teleportPosition);
                    ClearUserInventory(NetUser.FindByUserID(uIDAttack));
                    StartDuel();
                }
                if (StepTwo.Contains(uIDAttack) && StepTwo.Contains(uIDVictim))
                {
                    StepTwo.Remove(uIDAttack);
                    StepTwo.Remove(uIDVictim);
                    StepThree.Add(uIDAttack);
                    Losers.Add(uIDVictim);
                    Helper.TeleportTo(NetUser.FindByUserID(uIDAttack), teleportPosition);
                    ClearUserInventory(NetUser.FindByUserID(uIDAttack));
                    StartDuel();
                }
                if (StepThree.Contains(uIDAttack) && StepThree.Contains(uIDVictim))
                {
                    StepThree.Remove(uIDAttack);
                    StepThree.Remove(uIDVictim);
                    StepFour.Add(uIDAttack);
                    Losers.Add(uIDVictim);
                    Helper.TeleportTo(NetUser.FindByUserID(uIDAttack), teleportPosition);
                    ClearUserInventory(NetUser.FindByUserID(uIDAttack));
                    StartDuel();
                }
                if (StepFour.Contains(uIDAttack) && StepFour.Contains(uIDVictim))
                {
                    StepFour.Remove(uIDAttack);
                    StepFour.Remove(uIDVictim);
                    StepFive.Add(uIDAttack);
                    Losers.Add(uIDVictim);
                    Helper.TeleportTo(NetUser.FindByUserID(uIDAttack), teleportPosition);
                    ClearUserInventory(NetUser.FindByUserID(uIDAttack));
                    StartDuel();
                }
                if (StepFive.Contains(uIDAttack) && StepFive.Contains(uIDVictim))
                {
                    StepFive.Remove(uIDAttack);
                    StepFive.Remove(uIDVictim);
                    StepSix.Add(uIDAttack);
                    Losers.Add(uIDVictim);
                    Helper.TeleportTo(NetUser.FindByUserID(uIDAttack), teleportPosition);
                    ClearUserInventory(NetUser.FindByUserID(uIDAttack));
                    StartDuel();
                }
                if (StepSix.Contains(uIDAttack) && StepSix.Contains(uIDVictim))
                {
                    StepSix.Remove(uIDAttack);
                    StepSix.Remove(uIDVictim);
                    StepSeven.Add(uIDAttack);
                    Losers.Add(uIDVictim);
                    Helper.TeleportTo(NetUser.FindByUserID(uIDAttack), teleportPosition);
                    ClearUserInventory(NetUser.FindByUserID(uIDAttack));
                    StartDuel();
                }
                if (StepSeven.Contains(uIDAttack) && StepSeven.Contains(uIDVictim))
                {
                    if (StepSeven.Count == 1)
                    {
                        Winners.Add(2, uIDVictim);
                        Winners.Add(1, uIDAttack);
                        foreach (var player in PlayerClient.All)
                        {
                            rust.Notice(player.netUser, $"Ивент окончен. 3) {Users.GetBySteamID(Winners[3]).Username}, 2) {Users.GetBySteamID(Winners[2]).Username}, 1) {Users.GetBySteamID(Winners[1]).Username}");
                        }
                        StopEvent();
                        return;
                    }
                    if (StepSeven.Count == 2)
                    {
                        Winners.Add(3, uIDVictim);
                    }
                    StepSeven.Remove(uIDAttack);
                    StepSeven.Remove(uIDVictim);
                    Losers.Add(uIDVictim);
                    Helper.TeleportTo(NetUser.FindByUserID(uIDAttack), teleportPosition);
                    ClearUserInventory(NetUser.FindByUserID(uIDAttack));
                    StartDuel();
                }
            }
            catch { }
        }

        void OnPlayerConnected(NetUser user)
        {
            ulong uID =
                user.userID;

            if (teleportStarted)
                if (!Teleported.Contains(uID))
                {
                    DrawTeleportButton(user);
                }
        }

        [HookMethod("OnPlayerSpawn")]
        void OnPlayerSpawn(PlayerClient player)
        {
            if (Losers.Contains(player.userID))
            {
                Losers.Remove(player.userID);
                RestoreUser(player.userID);
            }
        }
        #endregion

        //---------------------------------------------------------------------//

        #region [CMDs] Event Manage -> [Команды] Управление Ивентами
        [ChatCommand("startevent")]
        void CMD_StartEvent(NetUser user, string cmd, string[] args)
        {
            if (!user.CanAdmin()) return;
            StartEvent(args[0], float.Parse(args[1]), int.Parse(args[2]));
            SCM(user, "Ивент начат!");
        }
        [ChatCommand("setpoint")]
        void CMD_SetPoint(NetUser user, string cmd, string[] args)
        {
            if (!user.CanAdmin()) return;
            Points.Add(user.playerClient.lastKnownPosition);
            SCM(user, "Точка установлена!");
        }
        [ChatCommand("setduel")]
        void CMD_SetDuel(NetUser user, string cmd, string[] args)
        {
            if (!user.CanAdmin()) return;
            if (Points.Count < 2)
            {
                SCM(user, "Нельзя сделать режим дуэлей, с точками меньше двух.");
                return;
            }
            if (myaso) myaso = !myaso;
            duel = !duel;
            if (duel)
            {
                SCM(user, "Вы включили режим дуэлей.");
            }
            if (!duel)
            {
                SCM(user, "Вы отключили режим дуэлей.");
            }
        }
        [ChatCommand("setmyaso")]
        void CMD_SetMyaso(NetUser user, string cmd, string[] args)
        {
            if (!user.CanAdmin()) return;
            if (duel) duel = !duel;
            myaso = !myaso;
            if (myaso)
            {
                SCM(user, "Вы включили режим мяса.");
            }
            if (!myaso)
            {
                SCM(user, "Вы отключили режим мяса.");
            }
        }
        #endregion

        //---------------------------------------------------------------------//

        #region [VOIDs] Simplification -> [Методы] Упрощение
        private void SCM(NetUser user, string MSG)
        {
            rust.SendChatMessage(user, "Event Helper", MSG);
        }
        private static void AddItem(Inventory inv, string name, int value)
        {
            inv.AddItemAmount(DatablockDictionary.GetByName(name), value);
        }
        #endregion

        /////////////////////////////////////////////////////////////////////////
        // Developed by: systemXcrackedZ                                       //
        // Server: Volchara Rust                                               //
        // VK: https://vk.com/sysxcrackz                                       //
        // Price: 150 rub.                                                     //
        // Использование данного плагина на других серверах без разрешения     //
        // кодера - запрещено.                                                 //
        /////////////////////////////////////////////////////////////////////////

        /////////////////////////////////////////////////////////////////////////
    }
}