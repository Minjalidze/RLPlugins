using System.Collections.Generic;
using System.Linq;
using RustExtended;

namespace Oxide.Plugins
{
    [Info("BSupports", "systemXcrackedZ", "1.0.0")]
    public class BSupports : RustLegacyPlugin
    {
        private class Support
        {
            public ulong SupportID { get; set; }
            public int Answers { get; set; }
        }
        private static List<Support> _supports = new List<Support>();

        private static Dictionary<ulong, string> _unanswered = new Dictionary<ulong, string>();

        private void Loaded()
        {
            _supports = Core.Interface.GetMod().DataFileSystem.ReadObject<List<Support>>("Supports");
        }
        private void OnServerSave()
        {
            SavePluginData();
        }
        private static void SavePluginData()
        {
            Core.Interface.GetMod().DataFileSystem.WriteObject("Supports", _supports);
        }

        [ChatCommand("setsupport")]
        private void OnCMD_SetSupport(NetUser user, string cmd, string[] args)
        {
            var enteredUserData = Users.Find(args[0]);
            if (enteredUserData == null)
            {
                SendChatMessage(user, $"Игрок с ником \"{args[0]}\" не найден!");
                return;
            }

            if (IsSupport(enteredUserData.SteamID))
            {
                SendChatMessage(user,$"Игрок \"{enteredUserData.Username}\" уже является игровым помощником!");
                return;
            }
            
            SendChatMessage(user, $"Вы назначили игрока \"{enteredUserData.Username}\" на пост игрового помощника!");
            SendChatMessage(NetUser.FindByUserID(enteredUserData.SteamID), $"Администратор \"{user.playerClient.userName}\" назначил Вас на пост игрового помощника!");
            
            _supports.Add(new Support
            {
                SupportID = enteredUserData.SteamID,
                Answers = 0
            });
            SavePluginData();
        }
        
        [ChatCommand("ask")]
        private void OnCMD_Ask(NetUser user, string cmd, string[] args)
        {
            if (args.Length < 1)
            {
                SendChatMessage(user, "Использование команды: /ask \"вопрос\".");
                return;
            }

            var ask = "";
            
            var n = args.Length;
            for (var i = 0; i < n; i++)
            {
                if (i != 0) ask += " ";
                ask += args[i];
            }
            
            foreach (var player in PlayerClient.All.Where(player => IsSupport(player.userID)))
            {
                SendChatMessage(player.netUser, $"Вопрос от игрока {user.playerClient.userName}:");
                SendChatMessage(player.netUser, ask);
                SendChatMessage(player.netUser, "Для ответа введите: /ans \"ник-нейм игрока\" \"ответ\"");
            }
            
            _unanswered.Add(user.userID, ask);
        }
        
        [ChatCommand("ans")]
        private void OnCMD_Ans(NetUser user, string cmd, string[] args)
        {
            if (!IsSupport(user.userID))
            {
                SendChatMessage(user, "Вам недоступна данная команда.");
                return;
            }
            if (args.Length < 2)
            {
                SendChatMessage(user, "Использование команды: /ans \"ник-нейм игрока\" \"ответ\".");
                return;
            }
            
            var askingUserData = Users.Find(args[0]);
            if (askingUserData == null)
            {
                SendChatMessage(user, $"Игрок с ником \"{args[0]}\" не найден!");
                return;
            }
            var askingUser = NetUser.FindByUserID(askingUserData.SteamID);

            if (!_unanswered.ContainsKey(askingUserData.SteamID))
            {
                SendChatMessage(user, "Указанный игрок не задавал вопрос, или уже получил ответ.");
                return;
            }
            
            var answer = "";
            
            var n = args.Length;
            for (var i = 1; i < n; i++)
            {
                if (i != 0) answer += " ";
                answer += args[i];
            }
            
            SendChatMessage(askingUser, $"Ответ игрового помощника({user.playerClient.userName}):");
            SendChatMessage(askingUser, answer);
            
            _unanswered.Remove(user.userID);
            _supports.Find(f => f.SupportID == user.userID).Answers += 1;
            SavePluginData();
        }

        private static bool IsSupport(ulong userID) => _supports.Find(f => f.SupportID == userID) != null;

        private void SendChatMessage(NetUser user, string message) =>
            rust.SendChatMessage(user, "Support System", $"[COLOR #DC143C]{message}");
    }
}