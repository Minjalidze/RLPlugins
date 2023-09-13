using System.Collections.Generic;
using RustExtended;
using System;
using System.Linq;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("BlessTopAIO", "systemXcrackedZ (vk.com/sysxcrackz)", "1.0.0")]
    internal class BlessTopAIO : RustLegacyPlugin
    {
        #region [CLASS] -> [Классы]

        private class Top
        {
            public ulong SteamID { get; set; }
            public int TimePlayed { get; set; }
            public int OreMined { get; set; }
            public int ExtractedWood { get; set; }
        }

        #endregion

        #region [LIST] -> [Списки]

        private static List<Top> _tops = new List<Top>();
        private static readonly List<string> AvailableCommands = new List<string>
        {
            "/t(op) kill - топ убийств игроков",
            "/t(op) farm - топ фарма ресурсов",
            "/t(op) animal - топ убийств животных",
            "/t(op) time - топ онлайна"
        };
        private static readonly List<string> TopColors = new List<string>
        {
            "[COLOR # 00d200]",
            "[COLOR # 1d98ff]",
            "[COLOR # fe7900]",
            "[COLOR # fe7900]",
            "[COLOR # fe7900]"
        };

        #endregion

        #region [CMD] -> [Команды]

        [ChatCommand("t")]
        private void On_CMDTopAlias(NetUser user, string cmd, string[] args) => On_CMDTop(user, cmd, args);
        [ChatCommand("top")]
        private void On_CMDTop(NetUser user, string cmd, string[] args)
        {
            if (args.Length == 0)
            {
                SCM(user, "Список доступных команд: ");
                foreach (var availableCmd in AvailableCommands)
                {
                    SCM(user, availableCmd);
                }
                return;
            }

            switch (args[0])
            {
                case "kill":
                    ShowKillTop(user);
                    return;
                case "farm":
                    ShowFarmTop(user);
                    return;
                case "animal":
                    ShowAnimalTop(user);
                    return;
                case "time":
                    ShowTimeTop(user);
                    return;
            }
        }

        #endregion

        #region [VOIDS] Show Tops -> [Методы] Показ топов
       
        private void ShowKillTop(NetUser user)
        {
            SCM(user, "Топ игроков по киллам:");
            
            var validUsers = new List<UserData>();
            for (var i = 1; i <= 5; i++)
            {
                var killed = 0;
                var deaths = 0;
                UserData userData = null;

                foreach (var validUser in Users.All)
                {
                    if (validUsers.Contains(validUser))
                        continue;
                    
                    killed = Economy.Database[validUser.SteamID].PlayersKilled;
                    deaths = Economy.Database[validUser.SteamID].Deaths;
                    userData = validUser;
                }
                
                if (userData != null)
                {
                    validUsers.Add(userData);
                    SCM(user, $"{i}) Игрок: {userData.Username}. Убито: {killed} игроков, смертей: {deaths}.",
                        TopColors[i]);
                }
            }
        }
        private void ShowFarmTop(NetUser user)
        {
            SCM(user, "Топ игроков по фарму:");
            
            var validUsers = new List<UserData>();
            for (var i = 1; i <= 5; i++)
            {
                var ore = 0;
                var wood = 0;
                
                var average = 0;
                UserData userData = null;

                foreach (var validUser in Users.All)
                {
                    if (validUsers.Contains(validUser))
                        continue;

                    var top = GetUserTop(validUser.SteamID);
                    var sum = top.OreMined + top.ExtractedWood;
                    
                    if (sum < average)
                        continue;

                    ore = top.OreMined;
                    wood = top.ExtractedWood;
                    
                    average = sum;
                    userData = validUser;
                }

                if (userData != null)
                {
                    validUsers.Add(userData);
                    SCM(user, $"{i}) Игрок: {userData.Username}. Добыто камня: {ore}, дерева: {wood}.", 
                        TopColors[i]);
                }
            }
        }
        private void ShowAnimalTop(NetUser user)
        {
            SCM(user, "Топ игроков по животным:");

            var validUsers = new List<UserData>();
            for (var i = 1; i <= 5; i++)
            {
                var animals = 0;
                var mutants = 0;
                UserData userData = null;
                
                foreach (var validUser in Users.All)
                {
                    if (validUsers.Contains(validUser))
                        continue;
                    animals = Economy.Database[validUser.SteamID].AnimalsKilled;
                    mutants = Economy.Database[validUser.SteamID].MutantsKilled;
                    userData = validUser;
                }

                if (userData != null)
                {
                    validUsers.Add(userData);
                    SCM(user, $"{i}) Игрок: {userData.Username}. Убито животных: {animals} игроков, мутантов: {mutants}.", 
                        TopColors[i]);
                }
            }
        }
        private void ShowTimeTop(NetUser user)
        {
            SCM(user, "Топ игроков по времени:");

            var validUsers = new List<UserData>();
            for (var i = 1; i <= 5; i++)
            {
                var timePlayed = 0;
                var average = 0;
                UserData userData = null;

                foreach (var validUser in Users.All)
                {
                    if (validUsers.Contains(validUser))
                        continue;

                    var top = GetUserTop(validUser.SteamID);
                    if (top.TimePlayed < average)
                        continue;

                    timePlayed = top.TimePlayed;
                    average = timePlayed;
                    
                    userData = validUser;
                }

                if (userData != null)
                {
                    validUsers.Add(userData);
                    SCM(user, $"{i}) Игрок: {userData.Username}. Наиграно: {Math.Round((double)timePlayed/60/60)} часов.", TopColors[i]);
                }
            }
        }

        #endregion

        #region [SERVER ACTIONS] -> [Действия Сервера]

        private void Loaded()
        {
            _tops = Interface.GetMod().DataFileSystem.ReadObject<List<Top>>("TopAIOData");

            timer.Repeat(1f, 0, delegate
            {
                foreach (var player in PlayerClient.All)
                {
                    var top = GetUserTop(player.userID);
                    top.TimePlayed++;
                }
            });
        }
        private void OnServerSave()
        {
            SaveData();
        }

        private static void SaveData()
        {
            Interface.GetMod().DataFileSystem.WriteObject("TopAIOData", _tops);
        }
        
        private void OnGather(Inventory inv, ResourceTarget resourceTarget, ResourceGivePair resourceGivePair, int amount)
        {
            try
            {
                var user = NetUser.Find(inv.networkView.owner);
                var top = GetUserTop(user.userID);
                
                switch (resourceTarget.type)
                {
                    case ResourceTarget.ResourceTargetType.WoodPile:
                    case ResourceTarget.ResourceTargetType.StaticTree:
                    {
                        top.ExtractedWood++;
                        SaveData();
                        break;
                    }
                    
                    case ResourceTarget.ResourceTargetType.Rock1:
                    case ResourceTarget.ResourceTargetType.Rock2:
                    case ResourceTarget.ResourceTargetType.Rock3:
                    {
                        top.OreMined++;
                        SaveData();
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                //stfu fucking unity
            }
        }

        private void OnPlayerSpawn(PlayerClient player)
        {
            if (GetUserTop(player.userID) == null)
            {
                _tops.Add(new Top
                {
                    SteamID = player.userID
                });
                SaveData();
            }
        }

        private static Top GetUserTop(ulong userid) => _tops.Find(f => f.SteamID == userid);
        
        #endregion

        #region [SIMPLIFICATION] -> [Упрощения]

        private void SCM(NetUser user, string message, string color = "[COLOR #FFD700]") => rust.SendChatMessage(user, "INVR", $"{color}{message}");

        #endregion
    }
}
