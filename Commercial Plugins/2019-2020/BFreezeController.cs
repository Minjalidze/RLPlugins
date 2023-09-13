using RustExtended;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("BFreezeController", "systemXcrackedZ", "4.1.2")]
    internal class BFreezeController : RustLegacyPlugin
    {
        private object OnUserCommand(IPlayer player, string command, string[] args)
        {
            if (command == "freeze")
            {
                OnCMD_Freeze(NetUser.FindByUserID(ulong.Parse(player.Id)), command, args);
                return false;
            }
            return null;
        }

        private void OnKilled(TakeDamage takeDamage, DamageEvent evt)
        {
            try
            {
                if (takeDamage is HumanBodyTakeDamage || evt.victim.idMain is DeployableObject)
                {
                    Inventory inventory = evt.attacker.client?.controllable?.GetComponent<Inventory>();
                    if (inventory != null && (inventory.activeItem?.datablock?.name?.Contains("Uber Hatchet") ?? false))
                    {
                        inventory.RemoveItem(inventory.activeItem.slot);
                        rust.Notice(evt.attacker.client?.netUser, "Вы получили фриз на 20 минут за нарушение правил сервера.");

                        FreezePlayer(evt.attacker.client?.netUser, 20 * 60);
                    }
                }
            }
            catch { }
        }

        [ChatCommand("unfreeze")]
        private void OnCMD_UnFreeze(NetUser user, string cmd, string[] args)
        {
            if (!user.CanAdmin() || Users.GetBySteamID(user.userID).Rank < 41)
            {
                rust.SendChatMessage(user, "Главное, Sven, не размер меча, а как ты с ним управляешься.");
                return;
            }
            if (args.Length < 1)
            {
                rust.SendChatMessage(user, "Список доступных команд: ");
                rust.SendChatMessage(user, "/freeze <nick> <time(s)>");
                rust.SendChatMessage(user, "/unfreeze <nick>");
                return;
            }

            UserData userData = Users.GetBySteamID(user.userID);
            UserData victimData = Users.Find(args[0]);
            if (victimData == null)
            {
                rust.SendChatMessage(user, "Список доступных команд: ");
                rust.SendChatMessage(user, "/freeze <nick> <time(s)>");
                rust.SendChatMessage(user, "/unfreeze <nick>");
                return;
            }

            if (!victimData.HasFlag(UserFlags.freezed))
            {
                rust.Notice(user, $"Игрок \"{victimData.Username}\" не имеет фриза!");
                return;
            }

            victimData.SetFlag(UserFlags.freezed, false);
            rust.Notice(NetUser.FindByUserID(victimData.SteamID), $"С вас снял фриз администратор \"{userData.Username}\"!");
            rust.Notice(user, $"Вы сняли фриз с игрока \"{victimData.Username}\"!");
        }

        private void OnCMD_Freeze(NetUser user, string cmd, string[] args)
        {
            if (!user.CanAdmin() || Users.GetBySteamID(user.userID).Rank < 41)
            {
                rust.SendChatMessage(user, "Жаль не взял собой рундук. Хе-хе. Сундук для рун - рундук.");
                return;
            }

            UserData userData = Users.GetBySteamID(user.userID);
            UserData victimData = Users.Find(args[0]);

            if (args.Length < 2 || victimData == null)
            {
                rust.SendChatMessage(user, "Список доступных команд: ");
                rust.SendChatMessage(user, "/freeze <nick> <time(s)>");
                rust.SendChatMessage(user, "/unfreeze <nick>");
                return;
            }

            if (victimData.HasFlag(UserFlags.freezed))
            {
                rust.Notice(user, $"Игрок \"{victimData.Username}\" уже имеет фриз!");
                return;
            }

            int time;
            try { time = int.Parse(args[1]); }
            catch
            {
                rust.SendChatMessage(user, "Список доступных команд: ");
                rust.SendChatMessage(user, "/freeze <nick> <time(s)>");
                rust.SendChatMessage(user, "/unfreeze <nick>"); return; 
            }

            NetUser victimUser = NetUser.FindByUserID(victimData.SteamID);
            FreezePlayer(victimUser, time);

            rust.Notice(user, $"Вы успешно зафризили игрока \"{victimData.Username}\" на \"{time}\" секунд!");
            rust.Notice(victimUser, $"Вас зафризил администратор \"{userData.Username}\" на \"{time}\" секунд!");
        }
        private void FreezePlayer(NetUser user, int time)
        {
            UserData userData = Users.GetBySteamID(user.userID);
            userData.SetFlag(UserFlags.freezed, true);

            timer.Once(time, () =>
            {
                if (userData.HasFlag(UserFlags.freezed))
                {
                    userData.SetFlag(UserFlags.freezed, false);

                    user = NetUser.FindByUserID(userData.SteamID);
                    if (user == null) return;

                    rust.Notice(user, $"Ваш фриз прошёл, можете бегать дальше!");
                }
            });
        }
    }
}
