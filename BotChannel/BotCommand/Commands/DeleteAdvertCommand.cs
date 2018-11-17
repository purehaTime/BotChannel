using BotChannel.DataManager;
using BotChannel.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace BotChannel.BotCommand.Commands
{
	public class DeleteAdvertCommand : BaseCommand, ICommand
	{
		private DbManager dbManager;
		private Advert advert;
		private List<Advert> advertList;

		private InlineKeyboardMarkup buttons;

		public DeleteAdvertCommand(ITelegramBotClient clientBot) : base(clientBot)
		{
			NextState = FirstStep;
			dbManager = new DbManager();

			var switcherButtons = new List<InlineKeyboardButton>()
			{
				InlineKeyboardButton.WithCallbackData("Next advert"),
				InlineKeyboardButton.WithCallbackData("Delete this"),
			};
			buttons = new InlineKeyboardMarkup(switcherButtons);
		}
		
		private async Task<bool> FirstStep()
		{
			var result = await ChooseGroupStep(dbManager, "Choose group to delete advert from:");
			NextState = SecondStep;

			return result;
		}

		private async Task<bool> SecondStep()
		{
			var group = dbManager.GetGroupByName(message.Text);
			if (group != null)
			{
				advertList = dbManager.GetAdvertsByGroup(group);
				if (advertList.Count > 0)
				{
					await bot.SendTextMessageAsync(message.From.Id, $"Found {advertList.Count} adverts for {group.Title}");
					await bot.SendTextMessageAsync(message.From.Id, advertList.FirstOrDefault()?.Message, replyMarkup: buttons);

					NextState = ThridStep;
					return false;
				}
				await bot.SendTextMessageAsync(message.From.Id, $"No adverts for group: {group.Title}");
				return true;
			}

			await bot.SendTextMessageAsync(message.From.Id, "It seems, chosed group was not found");
			return true;
		}

		private async Task<bool> ThridStep()
		{
			if (advertList.Count > 0)
			{
				var text = message.Text;
				if (text.Equals("Delete this"))
				{
					var deleting = advertList.FirstOrDefault();
					dbManager.DeleteAdvert(deleting);
					Worker.StopAdvertTask(deleting);
					await bot.SendTextMessageAsync(message.From.Id, "Advert was removed");
					return true;
				}
				advertList.RemoveAt(0);	
				if (advertList.Count > 0)
				{
					await bot.SendTextMessageAsync(message.From.Id, advertList.FirstOrDefault()?.Message, replyMarkup: buttons);
					return false;
				}
			}
			await bot.SendTextMessageAsync(message.From.Id, "No more adverts at list");
			return false;
		}

	}

}
