using System;
using System.Collections.Generic;
using uLink;
using Oxide.Core;
using Oxide.Core.Plugins;
using RageMods;
using RustExtended;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SRVEventsGUI", "systemXcrackedZ", "1.0.0")]
    class SRVEventsGUI : RustLegacyPlugin
    {
    	RustServerManagement management;

        string cDefault = "[COLOR #DC143C]";
        string chatName = "SRVEvent";

        Vector3 pos = new Vector3();

        bool eventstarted = false;
        bool grenadeevent = false;
        bool parkourevent = false;
        bool p250event = false;
        bool pubgevent = false;
        bool godmode = false;

        int RankAdminka = 25;

        void OnServerInitialized()
        {
            management = RustServerManagement.Get();
        }

        void execCMD(string Command)
        {
            rust.RunServerCommand(Command);
        }

        void DoTeleportToPos(NetUser source, Vector3 position)
        {
	        if (source == null || source.playerClient == null)
	            return;
	        management.TeleportPlayerToWorld(source.playerClient.netPlayer, position);
        }

        [ChatCommand("eventmenu")]
        void onCommand(NetUser netuser, string command, string[] args)
        {

        	UserData userData = Users.GetBySteamID(netuser.userID);
            string username = userData.Username;

        	if (userData.Rank < RankAdminka)
            {
                rust.SendChatMessage(netuser, chatName, cDefault + "У вас нет прав для использования данной комманды!");
                return;
            }

            PlayerMods playerGUI = PlayerMods.GetPlayerMods(netuser);

            playerGUI.SetCursorState(true);

            playerGUI.AddButton(new Rect(5f, 330f, 350f, 30f), "bStartEventP250", "Начать ивент \"1vs1 P250\"", delegate
            {
                eventstarted = true;
                p250event = true;
                grenadeevent = false;
				parkourevent = false;
				pubgevent = false;

				pos = netuser.playerClient.lastKnownPosition;

				timer.Once(1, () => { 
	                Broadcast.NoticeAll("❖", "Начался телепорт на ивент \"1vs1 P250\"!", null, 8f);
	                timer.Once(2, () => {
	                    Broadcast.NoticeAll("❖", "После телепортации инвентарь будет автоматически отчищен!", null, 8f);
	                    timer.Once(3, () => {
	                        Broadcast.NoticeAll("❖", "Телепортация на ивент будет длиться 120 секунд!", null, 8f);
	                        timer.Once(2, () => {
	                            Broadcast.NoticeAll("❖", "Чтобы попасть на ивент нажмите на кнопку, в левом верхнем углу экрана ПКМ, предватильно зайдя в инвентарь.", null, 8f);
	                            eventstarted = true;

	                            foreach (var player in PlayerClient.All)
	                            {
	                            	PlayerMods playersGUI = PlayerMods.GetPlayerMods(player.netUser);

	                            	UserData userDatas = Users.GetBySteamID(player.netUser.userID);
           							string usernamed = userDatas.Username;

	                            	playersGUI.AddButton(new Rect(5f, 30f, 350f, 30f), "bTeleportToEvent", "Телепортироваться на ивент.", delegate
	                            	{
	                            		if (eventstarted)
	                            		{
		                            		DoTeleportToPos(player.netUser, pos);
		                            		playersGUI.DeleteGUI("bTeleportToEvent");
		                            		execCMD("serv.inv "+usernamed+" clear");
		                            		timer.Once(0.5f, () =>
					                        {
					                            execCMD("serv.give "+usernamed+" P250");
					                            execCMD("serv.give "+usernamed+" \"9mm Ammo\" 500");
					                            execCMD("serv.give "+usernamed+" \"Leather Helmet\"");
					                            execCMD("serv.give "+usernamed+" \"Leather Vest\"");
					                            execCMD("serv.give "+usernamed+" \"Leather Pants\"");
					                            execCMD("serv.give "+usernamed+" \"Leather Boots\"");
					                            execCMD("serv.give "+usernamed+" \"Large Medkit\" 20");
					                        });
					                        playersGUI.DeleteGUI("bTeleportToEvent");
					                    }
	                            	});
	                            	timer.Once(120f, () => {
	                            		if (eventstarted)
	                            		{
			                            	eventstarted = false;
			                            	Broadcast.NoticeAll("❖", "Телепортация на ивент закончилась!", null, 8f);
			                            	PlayerMods.GetPlayerMods(player.netUser).DeleteGUI("bTeleportToEvent");
			                            }
	                            	});
	                            }
	                        });
	                    });
	                });
            	});
            });

            playerGUI.AddButton(new Rect(5f, 365f, 350f, 30f), "bStartEventParkour", "Начать ивент \"Командное пвп на болтах\"", delegate
            {
                eventstarted = true;
                grenadeevent = false;
				parkourevent = true;
				pubgevent = false;
        		p250event = false;

				pos = netuser.playerClient.lastKnownPosition;

				timer.Once(1, () => { 
	                Broadcast.NoticeAll("❖", "Начался телепорт на ивент \"Мясорубка\"!", null, 8f);
	                timer.Once(2, () => {
	                    Broadcast.NoticeAll("❖", "После телепортации инвентарь будет автоматически отчищен!", null, 8f);
	                    timer.Once(3, () => {
	                        Broadcast.NoticeAll("❖", "Телепортация на ивент будет длиться 120 секунд!", null, 8f);
	                        timer.Once(2, () => {
	                            Broadcast.NoticeAll("❖", "Чтобы попасть на ивент нажмите на кнопку, в левом верхнем углу экрана ПКМ, предватильно зайдя в инвентарь.", null, 8f);
	                            eventstarted = true;

	                            foreach (var player in PlayerClient.All)
	                            {
	                            	PlayerMods playersGUI = PlayerMods.GetPlayerMods(player.netUser);

	                            	UserData userDatas = Users.GetBySteamID(player.netUser.userID);
           							string usernamed = userDatas.Username;

	                            	playersGUI.AddButton(new Rect(5f, 30f, 350f, 30f), "bTeleportToEvent", "Телепортироваться на ивент.", delegate
	                            	{
	                            		if (eventstarted)
	                            		{
		                            		DoTeleportToPos(player.netUser, pos);
		                            		playersGUI.DeleteGUI("bTeleportToEvent");
		                            		rust.Notice(netuser, "Вы были телепортированы на ивент!", "❖" );
		                            		execCMD("serv.inv "+usernamed+" clear");
		                            		timer.Once(0.5f, () =>
					                        {
					                            execCMD("serv.give "+usernamed+" \"Large Medkit\" 10");
												execCMD("serv.give "+usernamed+" \"Bolt Action Rifle\" 1");
												execCMD("serv.give "+usernamed+" \"556 Ammo\" 100");
					                        });
					                        playersGUI.DeleteGUI("bTeleportToEvent");
					                    }
	                            	});
	                            	timer.Once(120f, () => {
	                            		if (eventstarted)
	                            		{
			                            	eventstarted = false;
			                            	Broadcast.NoticeAll("❖", "Телепортация на ивент закончилась!", null, 8f);
			                            	PlayerMods.GetPlayerMods(player.netUser).DeleteGUI("bTeleportToEvent");
			                            }
	                            	});
	                            }
	                        });
	                    });
	                });
            	});
            });

            playerGUI.AddButton(new Rect(5f, 400f, 350f, 30f), "bStartEventPUBG", "Начать ивент \"PUBG\"", delegate
            {
                eventstarted = true;
                pubgevent = true;
                grenadeevent = false;
				parkourevent = false;
        		p250event = false;

        		pos = netuser.playerClient.lastKnownPosition;

				timer.Once(1, () => { 
	                Broadcast.NoticeAll("❖", "Начался телепорт на ивент \"PUBG\"!", null, 8f);
	                timer.Once(2, () => {
	                    Broadcast.NoticeAll("❖", "После телепортации инвентарь будет автоматически отчищен!", null, 8f);
	                    timer.Once(3, () => {
	                        Broadcast.NoticeAll("❖", "Телепортация на ивент будет длиться 120 секунд!", null, 8f);
	                        timer.Once(2, () => {
	                            Broadcast.NoticeAll("❖", "Чтобы попасть на ивент нажмите на кнопку, в левом верхнем углу экрана ПКМ, предватильно зайдя в инвентарь.", null, 8f);
	                            eventstarted = true;

	                            foreach (var player in PlayerClient.All)
	                            {
	                            	PlayerMods playersGUI = PlayerMods.GetPlayerMods(player.netUser);

	                            	UserData userDatas = Users.GetBySteamID(player.netUser.userID);
           							string usernamed = userDatas.Username;

	                            	playersGUI.AddButton(new Rect(5f, 30f, 350f, 30f), "bTeleportToEvent", "Телепортироваться на ивент.", delegate
	                            	{
	                            		if (eventstarted)
	                            		{
		                            		DoTeleportToPos(player.netUser, pos);
		                            		playersGUI.DeleteGUI("bTeleportToEvent");
		                            		rust.Notice(netuser, "Вы были телепортированы на ивент!", "❖" );
		                            		execCMD("serv.inv "+usernamed+" clear");
					                    }
					                    playersGUI.DeleteGUI("bTeleportToEvent");
	                            	});
	                            	timer.Once(120f, () => {
	                            		if (eventstarted)
	                            		{
			                            	eventstarted = false;
			                            	Broadcast.NoticeAll("❖", "Телепортация на ивент закончилась!", null, 8f);
			                            	PlayerMods.GetPlayerMods(player.netUser).DeleteGUI("bTeleportToEvent");
			                            }
	                            	});
	                            }
	                        });
	                    });
	                });
            	});
            });

            playerGUI.AddButton(new Rect(5f, 435f, 350f, 30f), "bStartEventGrenade", "Начать ивент \"Взрывной Дождь\"", delegate
            {
                eventstarted = true;
                grenadeevent = true;
				parkourevent = false;
				pubgevent = false;
        		p250event = false;

        		pos = netuser.playerClient.lastKnownPosition;

				timer.Once(1, () => { 
	                Broadcast.NoticeAll("❖", "Начался телепорт на ивент \"Взрывной Дождь\"!", null, 8f);
	                timer.Once(2, () => {
	                    Broadcast.NoticeAll("❖", "После телепортации инвентарь будет автоматически отчищен!", null, 8f);
	                    timer.Once(3, () => {
	                        Broadcast.NoticeAll("❖", "Телепортация на ивент будет длиться 120 секунд!", null, 8f);
	                        timer.Once(2, () => {
	                            Broadcast.NoticeAll("❖", "Чтобы попасть на ивент нажмите на кнопку, в левом верхнем углу экрана ПКМ, предватильно зайдя в инвентарь.", null, 8f);
	                            eventstarted = true;

	                            foreach (var player in PlayerClient.All)
	                            {
	                            	PlayerMods playersGUI = PlayerMods.GetPlayerMods(player.netUser);

	                            	UserData userDatas = Users.GetBySteamID(player.netUser.userID);
           							string usernamed = userDatas.Username;

	                            	playersGUI.AddButton(new Rect(5f, 30f, 350f, 30f), "bTeleportToEvent", "Телепортироваться на ивент.", delegate
	                            	{
	                            		if (eventstarted)
	                            		{
		                            		DoTeleportToPos(player.netUser, pos);
		                            		playersGUI.DeleteGUI("bTeleportToEvent");
		                            		rust.Notice(netuser, "Вы были телепортированы на ивент!", "❖" );
		                            		execCMD("serv.inv "+usernamed+" clear");
					                    }
					                    playersGUI.DeleteGUI("bTeleportToEvent");
	                            	});
	                            	timer.Once(120f, () => {
	                            		if (eventstarted)
	                            		{
			                            	eventstarted = false;
			                            	Broadcast.NoticeAll("❖", "Телепортация на ивент закончилась!", null, 8f);
			                            	PlayerMods.GetPlayerMods(player.netUser).DeleteGUI("bTeleportToEvent");
			                            }
	                            	});
	                            }
	                        });
	                    });
	                });
            	});
            });

            playerGUI.AddButton(new Rect(5f, 470f, 350f, 30f), "bCancelEvent", "Закрыть телепорт", delegate
            {
                pubgevent = false;
                grenadeevent = false;
				parkourevent = false;
        		p250event = false;

	            if(eventstarted)
	            {
	                eventstarted = false;
	                Broadcast.NoticeAll("❖", "Телепортация на ивент остановлена!", null, 8f);
	                foreach (var player in PlayerClient.All)
	                {
	                	PlayerMods.GetPlayerMods(player.netUser).DeleteGUI("bTeleportToEvent");
	                }
	            }
	            else
	            {
	                rust.Notice(netuser, "Телепортация на ивент закрыта!", "❖" );
	            }
            });

            playerGUI.AddButton(new Rect(5f, 505f, 350f, 30f), "bGiveEvent", "Набор для ивентов", delegate
            {
	            execCMD("serv.give "+username+" \"Metal Foundation\" 250");
	            execCMD("serv.give "+username+" \"Metal Wall\" 250");
	            execCMD("serv.give "+username+" \"Metal Pillar\" 250");
	            execCMD("serv.give "+username+" \"Metal Window\" 250");
	            execCMD("serv.give "+username+" \"Metal Stairs\" 250");
	            execCMD("serv.give "+username+" \"Metal Ramp\" 250");
	            execCMD("serv.give "+username+" \"Metal Ceiling\" 250");
	            execCMD("serv.give "+username+" \"Metal Doorway\" 250");
	            execCMD("serv.give "+username+" \"Metal Window Bars\" 50");
	            execCMD("serv.give "+username+" \"Metal Door\" 10");
	            execCMD("serv.give "+username+" \"Large Wood Storage\" 10");
	            rust.Notice(netuser, "Вам был выдан набор для ивентов!", "❖" );
            });

            playerGUI.AddButton(new Rect(5f, 540f, 350f, 30f), "bClear", "Отчистить инвентарь себе.", delegate
            {
	        	UserData clearData = Users.GetBySteamID(netuser.userID);
	            string clearname = clearData.Username;

            	execCMD("serv.inv "+username+" clear");
            	rust.Notice(netuser, "Инвентарь отчищен!", "❖" );
            }); 

            playerGUI.AddButton(new Rect(5f, 575f, 350f, 30f), "bTruth", "Включить/Отключить Анти-Чит для себя.", delegate
            {

	        	UserData truthedData = Users.GetBySteamID(netuser.userID);
	            string truthed = truthedData.Username;

            	execCMD("serv.truth "+truthed);
            });         

            playerGUI.AddButton(new Rect(5f, 610f, 350f, 30f), "bGod", "Включить/Отключить GM для себя.", delegate
            {
            	if (godmode)
            	{
            		rust.Notice(netuser, "GM отключен!", "❖" );
            		godmode = false;
            		netuser.playerClient.rootControllable.rootCharacter.takeDamage.SetGodMode(false);
            	}
            	else
            	{	
            		rust.Notice(netuser, "GM включен!", "❖" );
            		godmode = true;
            		netuser.playerClient.rootControllable.rootCharacter.takeDamage.SetGodMode(true);
            	}
            }); 

            playerGUI.AddButton(new Rect(5f, 645f, 350f, 30f), "bClose", "Закрыть меню", delegate
            {

				playerGUI.SetCursorState(false);

                playerGUI.DeleteGUI("bStartEventParkour");

                playerGUI.DeleteGUI("bStartEventGrenade");

                playerGUI.DeleteGUI("bStartEventP250");

                playerGUI.DeleteGUI("bStartEventPUBG");

                playerGUI.DeleteGUI("bStartEventPUBG");

                playerGUI.DeleteGUI("bCancelEvent");

				playerGUI.DeleteGUI("bGiveEvent");

				playerGUI.DeleteGUI("bClose");

				playerGUI.DeleteGUI("bClear");

				playerGUI.DeleteGUI("bTruth");

				playerGUI.DeleteGUI("bGod");

            });

        }

        [ChatCommand("spawnevent")]
        void onSpawn(NetUser netuser, string command, string[] args)
        {
            UserData userData = Users.GetBySteamID(netuser.userID);
            string username = userData.Username;

            if (userData.Rank < RankAdminka)
            {
                rust.SendChatMessage(netuser, chatName, cDefault + "У вас нет прав для использования данной комманды!");
                return;
            }

            if (args.Length == 0 || args.Length > 1)
            {
                rust.SendChatMessage(netuser, chatName, cDefault + "Используйте: /spawnevent \"ник-нейм игрока\"");
                return;
            }

            string spawnplayer = args[0];

            execCMD("serv.tele "+spawnplayer+" "+pos.x+" "+pos.y+" "+pos.z);
        }

        [ChatCommand("summonevent")]
        void onSummon(NetUser netuser, string command, string[] args)
        {
            UserData userData = Users.GetBySteamID(netuser.userID);
            string username = userData.Username;

            if (userData.Rank < RankAdminka)
            {
                rust.SendChatMessage(netuser, chatName, cDefault + "У вас нет прав для использования данной комманды!");
                return;
            }

            if (args.Length == 0 || args.Length > 1)
            {
                rust.SendChatMessage(netuser, chatName, cDefault + "Используйте: /summonevent \"ник-нейм игрока\"");
                return;
            }

            string summonplayer = args[0];

			execCMD("serv.tele "+summonplayer+" "+pos.x+" "+pos.y+" "+pos.z);

            execCMD("serv.inv "+summonplayer+" clear");

	        if (p250event)
	        {
	        	timer.Once(0.5f, () => 
            	{ 
					execCMD("serv.give "+summonplayer+" P250");
					execCMD("serv.give "+summonplayer+" \"9mm Ammo\" 500");
					execCMD("serv.give "+summonplayer+" \"Leather Helmet\"");
					execCMD("serv.give "+summonplayer+" \"Leather Vest\"");
					execCMD("serv.give "+summonplayer+" \"Leather Pants\"");
					execCMD("serv.give "+summonplayer+" \"Leather Boots\"");
					execCMD("serv.give "+summonplayer+" \"Large Medkit\" 20");
					execCMD("serv.give "+summonplayer+" \"Wood Barricade\" 50");
				});     	
	        }

	        if (parkourevent)
	        {
	        	timer.Once(0.5f, () => 
            	{ 
					execCMD("serv.give "+summonplayer+" \"Large Medkit\" 10");
					execCMD("serv.give "+summonplayer+" \"Cooked Chicken Breast\" 30");
				});     	
	        }
        }
    }
}