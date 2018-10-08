using BotChannel.DataManager;
using BotChannel.Model;
using BotChannel.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace BotChannel.BotCommand.Commands
{
	public class AddPostCommand : BaseCommand, ICommand
	{
		private DbManager dbManager;
		private Content contentSave;

		public AddPostCommand(ITelegramBotClient clientBot): base(clientBot)
		{
			NextState = FirstStep;
			dbManager = new DbManager();
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
			await bot.SendTextMessageAsync(message.From.Id, "Choose group to add:", replyMarkup: replyButtons);

			NextState = SecondStep;
			return false;
		}

		private async Task<bool> SecondStep()
		{
			var selectedGroup = message.Text;
			var groupId = dbManager.GetGroupByName(selectedGroup);
			if (groupId == null)
			{
				await bot.SendTextMessageAsync(message.From.Id, "It seems, chosed group was not found");
				return true;
			}
			contentSave = new Content();
			contentSave.GroupId = groupId?.GroupId;
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
					List<string> parsedLinks = null;
					if (link.Contains("vk.") && (link.Contains("album") || link.Contains("wall")))   //parse with VK api
					{
						parsedLinks = await ParseAsVK(link);
					}
					else
					{
						parsedLinks = link.Split(";").ToList();
					}
					SavePostToDb(parsedLinks);
				}
			}

			await bot.SendTextMessageAsync(message.From.Id, $"Complete ! Saved {linkList.Count()} posts");
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

		private void SavePostToDb(List<string> linkList)
		{
			contentSave.MessagerType = "telegram";
			contentSave.Posted = false;
			contentSave.PhotoList = linkList.ToArray();
			dbManager.AddNewPost(contentSave);
		}
	}

}
