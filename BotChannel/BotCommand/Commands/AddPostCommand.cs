﻿using BotChannel.DataManager;
using BotChannel.Model;
using BotChannel.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace BotChannel.BotCommand.AddVkPost
{
	public class AddPostCommand : ICommand
	{
		public Func<Task<bool>> NextState { get; set; }

		private ITelegramBotClient bot;
		private Message message;
		private DbManager dbManager;
		private Content contentSave;

		public AddPostCommand()
		{
			dbManager = new DbManager();
		}

		public async Task<bool> Action(Message updateMessage)
		{
			if (updateMessage.Text.Equals("/cancel"))
			{
				return true;
			}

			message = updateMessage;
			return await NextState();
		}

		public AddPostCommand(ITelegramBotClient clientBot)
		{
			bot = clientBot;
			NextState = FirstStep;
		}

		private async Task<bool> FirstStep()
		{
			var groupList = dbManager.GetGroups();
			var buttons = new List<KeyboardButton>();

			foreach (var group in groupList)
			{
				buttons.Add(new KeyboardButton(group.Title));
			}

			var replyButtons = new ReplyKeyboardMarkup(buttons);
			var request = await bot.SendTextMessageAsync(message.From.Id, "Choose group to add:", replyMarkup: replyButtons);

			NextState = SecondStep;
			return false;
		}

		private async Task<bool> SecondStep()
		{
			var selectedGroup = message.Text;
			var groupId = dbManager.GetGroupIdByName(selectedGroup);
			if (groupId == null)
			{
				await bot.SendTextMessageAsync(message.From.Id, "It seems, chosed group was deleted");
				return true;
			}
			contentSave.GroupId = groupId;
			var request = await bot.SendTextMessageAsync(message.From.Id, "Send direct links " +
				"(separate by ',' for one post and ';' for array photo to one post). Or VK post/album (every wall-link for one post " +
				"and one photo from album well be save for one post)");

			NextState = ThridStep;
			return false;
		}

		private async Task<bool> ThridStep()
		{
			var linkList = message.Text.Split(",");
			foreach (var link in linkList)
			{
				if (IsValid(link))
				{
					if (link.Contains("vk.") && (link.Contains("album") || link.Contains("wall")))   //parse with VK api
					{
						var result = await ParseAsVK(link);
					}
				}

			}
			var request = await bot.SendTextMessageAsync(message.From.Id, "Complete !");
			return true;
		}

		private bool IsValid(string link)
		{
			Uri uri;
			return link.Contains(".") && Uri.TryCreate(link, UriKind.Absolute, out uri)
				&& (uri.Scheme == Uri.UriSchemeHttp
				 || uri.Scheme == Uri.UriSchemeHttps);
		}

		private async Task<List<string>> ParseAsVK(string link)
		{
			VkParser vk = new VkParser();
			if (link.Contains("wall"))
			{
				return await vk.GetLinksFromPost(link);
			}
			return await vk.GetLinksFromAlbum(link);
		}
	}

}
