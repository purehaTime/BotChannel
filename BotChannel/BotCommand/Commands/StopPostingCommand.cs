using BotChannel.DataManager;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace BotChannel.BotCommand.Commands
{
	public class StopPostingCommand : BaseCommand, ICommand
	{
		private DbManager dbManager;

		public StopPostingCommand(ITelegramBotClient clientBot) : base(clientBot)
		{
			NextState = FirstStep;
			dbManager = new DbManager();
		}
		
		private async Task<bool> FirstStep()
		{
			//must popular command. will be move to base class
			var groupList = dbManager.GetGroups();	
			var buttons = new List<KeyboardButton>();

			foreach (var group in groupList)
			{
				buttons.Add(new KeyboardButton(group.Title));
			}

			var replyButtons = new ReplyKeyboardMarkup(buttons);
			await bot.SendTextMessageAsync(message.From.Id, "Choose group to stop posting:", replyMarkup: replyButtons);
			NextState = SecondStep;

			return false;
		}

		private async Task<bool> SecondStep()
		{
			var group = dbManager.GetGroupByName(message.Text);
			if (group != null)
			{
				Worker.StopPostingTask(group);
				await bot.SendTextMessageAsync(message.From.Id, $"posting for {group.Title} was stopped");
				return true;
			}
			await bot.SendTextMessageAsync(message.From.Id, "It seems, chosed group was not found");
			return true;
		}
	}

}
