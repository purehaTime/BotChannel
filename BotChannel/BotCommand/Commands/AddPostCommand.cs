using BotChannel.DataManager;
using BotChannel.Model;
using BotChannel.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;

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
			var result = await ChooseGroupStep(dbManager, "Choose group to add content:");

			NextState = SecondStep;
			return result;
		}

		private async Task<bool> SecondStep()
		{
			var selectedGroup = message.Text;
			var group = dbManager.GetGroupByName(selectedGroup);
			if (group == null)
			{
				await bot.SendTextMessageAsync(message.From.Id, "It seems, chosed group was not found");
				return true;
			}
			contentSave = new Content();
			contentSave.GroupId = group?.GroupId;
			var request = await bot.SendTextMessageAsync(message.From.Id, "Send direct links " +
				"(separate by ',' for one post and ';' for array photo to one post). Or VK post/album (every wall-link for one post " +
				"and one photo from album well be save for one post)");

			NextState = ThridStep;
			return false;
		}

		private async Task<bool> ThridStep()
		{
			var linkList = message.Text.Split(",");
			var counts = 0;
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

					if (SavePostToDb(parsedLinks))
					{
						counts++;
					}
				}
			}

			var fake = counts > 0
				? await bot.SendTextMessageAsync(message.From.Id, $"Complete ! Saved {counts} posts")
				: await bot.SendTextMessageAsync(message.From.Id, $"Nothing to save");

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
			VkParser vk = new VkParser(true);
			if (link.Contains("wall"))
			{
				return await vk.GetLinksFromPost(link);
			}
			return await vk.GetLinksFromAlbum(link);
		}

		private bool SavePostToDb(List<string> linkList)
		{
			contentSave.MessagerType = "telegram";
			contentSave.Posted = false;
			contentSave.PhotoList = linkList.ToArray();
			dbManager.AddNewPost(contentSave);
			return true;
		}
	}

}
