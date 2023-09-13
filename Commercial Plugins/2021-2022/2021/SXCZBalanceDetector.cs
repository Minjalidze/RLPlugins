using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using RustExtended;
using System;

namespace Oxide.Plugins
{
    [Info("SXCZBalanceDetector", "systemXcrackedZ", "1.0.0")]
    internal class SXCZBalanceDetector : RustLegacyPlugin
    {
        private string MSGDetected = "";
        private string MSGCMD = "";

        private const int user_idA1 = 225435431;
        private const int user_idA2 = 555084949;
        private const string TOKEN = "9a55d1a9f071f24ea2fd27e1977216c0bc07563165b088b05230a3af8719fc96767626c993bdbf56c4f68";

        //private const int user_idA3 = 1;

        void Loaded()
        {
            timer.Repeat(5f, 0, () =>
            {
                foreach (var player in PlayerClient.All)
                    if (Economy.Database[player.userID].Balance >= 100000)
                    {
                        MSGDetected = string.Format("[ALARM]: Balance player {0} more 100.000$. Current balance: {1}.", player.userName, Economy.Database[player.userID].Balance);
                        SendMessageDetected();
                    }
            });
        }

        private object OnUserCommand(IPlayer player, string command, string[] args)
        {
            ulong uID = ulong.Parse(player.Id);
            if (Economy.Database[uID].Balance >= 100000)
            {
                MSGDetected = string.Format("[ALARM]: Balance player {0} more 100.000$. Current balance: {1}.", Users.GetBySteamID(uID).Username, Economy.Database[uID].Balance);
                MSGCMD = string.Format("[ALERT]: Last command used: /{0}", command);
                SendMessageDetected();
                SendMessageCommand();
            }
            return null;
        }

        [HookMethod("OnCommandRun")]
        private object RunCommand(NetUser user, string command, string[] args)
        {
            ulong uID = user.userID;
            if (Economy.Database[uID].Balance >= 100000)
            {
                MSGDetected = string.Format("[ALARM]: Balance player {0} more 100.000$. Current balance: {1}.", Users.GetBySteamID(uID).Username, Economy.Database[uID].Balance);
                MSGCMD = string.Format("[ALERT]: Last command used: /{0}", command);
                SendMessageDetected();
                SendMessageCommand();
            }
            return null;
        }

        void SendMessageDetected()
        {
            webrequest.EnqueueGet("https://api.vk.com/method/messages.send?user_id=" + user_idA1 + $"&message=\"{MSGDetected}\"&access_token={TOKEN}&v=5.73", (code, response) => GetCallback(code, response), this);
            webrequest.EnqueueGet("https://api.vk.com/method/messages.send?user_id=" + user_idA2 + $"&message=\"{MSGDetected}\"&access_token={TOKEN}&v=5.73", (code, response) => GetCallback(code, response), this);
            //webrequest.EnqueueGet("https://api.vk.com/method/messages.send?user_id=" + user_idA3 + $"&message=\"{MSGDetected}\"&access_token={TOKEN}&v=5.73", (code, response) => GetRegisterCallback(code, response), this);
        }
        void SendMessageCommand()
        {
            webrequest.EnqueueGet("https://api.vk.com/method/messages.send?user_id=" + user_idA1 + $"&message=\"{MSGCMD}\"&access_token={TOKEN}&v=5.73", (code, response) => GetCallback(code, response), this);
            webrequest.EnqueueGet("https://api.vk.com/method/messages.send?user_id=" + user_idA2 + $"&message=\"{MSGCMD}\"&access_token={TOKEN}&v=5.73", (code, response) => GetCallback(code, response), this);
            //webrequest.EnqueueGet("https://api.vk.com/method/messages.send?user_id=" + user_idA3 + $"&message=\"{MSGCMD}\"&access_token={TOKEN}&v=5.73", (code, response) => GetRegisterCallback(code, response), this);
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
