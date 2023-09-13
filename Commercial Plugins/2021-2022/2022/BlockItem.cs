using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using RustExtended;
using Random = Oxide.Core.Random;

namespace Oxide.Plugins
{
	[Info("BlockItem", "HostFun", "1.0.1")]
	internal class BlockItem : RustLegacyPlugin
	{
		private const string Prefix = "BLOCK";
		private const string MsgTimeLeft = "[color #FFFFFF]До разблокировки [color #FF7433]%ITEMNAME% [color #FFFFFF]осталось [color #FF7433]%TIME%";
		private List<BlockedItem> _allBlocked = new List<BlockedItem>();

		private List<BlockedItem> TryDefault()
		{
			var newlist = new List<BlockedItem>
			{
				new BlockedItem { Name = "M4", DateTime = DateTime.Now.ToString("g") },
				new BlockedItem { Name = "Explosive Charge", DateTime = DateTime.Now.ToString("g") }
			};
			return newlist;
		}

		public class BlockedItem
		{
			[JsonProperty("Название предмета")] public string Name;
			[JsonProperty("Время окончание блокировки (Месяц/День/Год Часы:Минуты)")]
			public string DateTime;
		}

		private void Loaded()
		{
			if (Interface.Oxide.DataFileSystem.ExistsDatafile("BlockConfig"))
			{
				_allBlocked = Interface.Oxide.DataFileSystem.ReadObject<List<BlockedItem>>("BlockConfig");
				return;
			}
			Interface.Oxide.DataFileSystem.WriteObject("BlockConfig", _allBlocked = TryDefault(), true);
		}

		[HookMethod("OnBeltUse")]
		public object BeltDetector(PlayerInventory inv, IInventoryItem inventoryItem)
		{
			var netuser = inventoryItem.controllable.netUser;
			// if (inventoryItem.datablock.name == "Uber Hatchet") rust.Notice(netuser, "Урон от Fire Hatchet наказывается фризом!!!");
			// if (inventoryItem.datablock.name == "Repair Bench") Broadcast.Message(netuser, "Вы взяли в руки шкаф, детальней /shkaf", "DarkRust");
			if (netuser == null || netuser.admin || Users.GetBySteamID(netuser.userID).Rank > 100) return null;
			if (_allBlocked == null || _allBlocked.Count <= 0) return null;
			var selectblock = _allBlocked.FirstOrDefault(item => item.Name == inventoryItem.datablock.name);
			if (selectblock == null) return null;
			DateTime datetime;
			var isParse = DateTime.TryParse(selectblock.DateTime, out datetime);
			if (!isParse) return null;
			if (DateTime.Now > datetime) return null;
			var kitTime = TimeSpan.FromSeconds((datetime - DateTime.Now).TotalSeconds);
			var msgTimeLeftnew = MsgTimeLeft.Replace("%ITEMNAME%", inventoryItem.datablock.name);
			if (kitTime.TotalDays >= 1) msgTimeLeftnew = msgTimeLeftnew.Replace("%TIME%", $"{kitTime.TotalDays:F0}д {kitTime.TotalHours:F0}ч {kitTime.Minutes:D2}м {kitTime.Seconds:D2} с");
			else if (kitTime.TotalHours >= 1) msgTimeLeftnew = msgTimeLeftnew.Replace("%TIME%", $"{kitTime.TotalHours:F0}ч {kitTime.Minutes:D2}м {kitTime.Seconds:D2} с");
			else if (kitTime.TotalMinutes >= 1) msgTimeLeftnew = msgTimeLeftnew.Replace("%TIME%", $"{kitTime.Minutes}м {kitTime.Seconds:D2}с");
			else msgTimeLeftnew = msgTimeLeftnew.Replace("%TIME%", $"{kitTime.Seconds:D2}с");
			Broadcast.Message(netuser, msgTimeLeftnew, Prefix);
			return true;
		}

		[ChatCommand("wipeblock")]
		void cmdPromo(NetUser netUser, string command, string[] args)
		{
			if (_allBlocked == null || _allBlocked.Count <= 0) { Broadcast.Message(netUser, "[color #FFFFFF]Блокировки на оружие и взрывчатку[color #FF7433] нет", Prefix); return; }
			var blocks = 0;
			foreach (var block in _allBlocked)
			{
				DateTime blockdate;
				var isParse = DateTime.TryParse(block.DateTime, out blockdate);
				if (isParse)
				{
					if (DateTime.Now < blockdate)
					{
						blocks++;
						var kitTime = TimeSpan.FromSeconds((blockdate - DateTime.Now).TotalSeconds);
						var msgTimeLeftnew = MsgTimeLeft.Replace("%ITEMNAME%", block.Name);
						if (kitTime.TotalDays >= 1) msgTimeLeftnew = msgTimeLeftnew.Replace("%TIME%", $"{kitTime.TotalDays:F0}д {kitTime.TotalHours:F0}ч {kitTime.Minutes:D2}м {kitTime.Seconds:D2} с");
						else if (kitTime.TotalHours >= 1) msgTimeLeftnew = msgTimeLeftnew.Replace("%TIME%", $"{kitTime.TotalHours:F0}ч {kitTime.Minutes:D2}м {kitTime.Seconds:D2} с");
						else if (kitTime.TotalMinutes >= 1) msgTimeLeftnew = msgTimeLeftnew.Replace("%TIME%", $"{kitTime.Minutes}м {kitTime.Seconds:D2}с");
						else msgTimeLeftnew = msgTimeLeftnew.Replace("%TIME%", $"{kitTime.Seconds:D2}с");
						Broadcast.Message(netUser, msgTimeLeftnew, Prefix);
					}
				}
			}
			if (blocks <= 0) { Broadcast.Message(netUser, "[color #FFFFFF]Блокировки на оружие, броню и взрывчатку[color #FF7433] нет.", Prefix); return; }
		}
		private object OnItemMoved(PlayerClient playerClient, Inventory fromInventory, int fromSlot, Inventory moveInventory, int moveSlot, Inventory.SlotOperationsInfo info)
		{
			IInventoryItem inventoryItem;
			if (!fromInventory.GetItem(fromSlot, out inventoryItem)) return false;

			if (inventoryItem.datablock.category == ItemDataBlock.ItemCategory.Armor && moveSlot > 35 && moveSlot < 40)
			{
				if (_allBlocked == null || _allBlocked.Count <= 0) return null;

				var selectblock = _allBlocked.FirstOrDefault(item => item.Name == inventoryItem.datablock.name);
				if (selectblock == null) return null;

				DateTime datetime;
				var isParse = DateTime.TryParse(selectblock.DateTime, out datetime);
				if (!isParse) return null;
				if (DateTime.Now > datetime) return null;

				var kitTime = TimeSpan.FromSeconds((datetime - DateTime.Now).TotalSeconds);
				var msgTimeLeftnew = MsgTimeLeft.Replace("%ITEMNAME%", inventoryItem.datablock.name);

				if (kitTime.TotalDays >= 1) msgTimeLeftnew = msgTimeLeftnew.Replace("%TIME%", $"{kitTime.TotalDays:F0}д {kitTime.TotalHours:F0}ч {kitTime.Minutes:D2}м {kitTime.Seconds:D2} с");
				else if (kitTime.TotalHours >= 1) msgTimeLeftnew = msgTimeLeftnew.Replace("%TIME%", $"{kitTime.TotalHours:F0}ч {kitTime.Minutes:D2}м {kitTime.Seconds:D2} с");
				else if (kitTime.TotalMinutes >= 1) msgTimeLeftnew = msgTimeLeftnew.Replace("%TIME%", $"{kitTime.Minutes}м {kitTime.Seconds:D2}с");
				else msgTimeLeftnew = msgTimeLeftnew.Replace("%TIME%", $"{kitTime.Seconds:D2}с");

				Broadcast.Message(playerClient.netUser, msgTimeLeftnew, Prefix);

				timer.Once(0.1f, () =>
				{
					IInventoryItem inventoryItem2;
					if (moveInventory.GetItem(moveSlot, out inventoryItem2) && inventoryItem2.datablock.name == inventoryItem.datablock.name)
					{
						moveInventory.RemoveItem(moveSlot);
						fromInventory.AddItem(inventoryItem2.datablock, fromSlot, inventoryItem2.uses);
					}
				});
			}
			return null;
		}
		// [ChatCommand("shkaf")]
		// void cmdRecycle(NetUser netuser, string command, string[] args)
		// {
		// rust.SendChatMessage(netuser, "DarkRust", "[color #ffbf00]Теперь можно поставить [color #F5DEB3]\"Шкаф\" в дом[color #ffbf00] и к тебе никто не подстроится!");
		// rust.SendChatMessage(netuser, "DarkRust", "[color #ffbf00]Шкафом является [color #F5DEB3]\"Repair Bench\"");
		// rust.SendChatMessage(netuser, "DarkRust", "[color #ffbf00]Его можно [color #F5DEB3]скрафтить [color #ffbf00]или получить в [color #F5DEB3]/kit home");
		// rust.SendChatMessage(netuser, "DarkRust", "[color #ffbf00]Поставив в доме шкаф, никто кроме вас и ваших друзей [color #F5DEB3]/share[color #ffbf00] не сможет строится в радиусе [color #F5DEB3]40 метров");
		// }
	}
}