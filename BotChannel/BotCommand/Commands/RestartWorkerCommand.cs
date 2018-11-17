using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace BotChannel.BotCommand.Commands
{
	public class RestartWorkerCommand : BaseCommand, ICommand
	{
		private InlineKeyboardMarkup buttons;

		public RestartWorkerCommand(ITelegramBotClient clientBot) : base(clientBot)
		{
			NextState = FirstStep;
			var yesNoButtons = new List<InlineKeyboardButton>()
			{
				InlineKeyboardButton.WithCallbackData("Yes"),
				InlineKeyboardButton.WithCallbackData("No"),
			};
			buttons = new InlineKeyboardMarkup(yesNoButtons);
		}

		private async Task<bool> FirstStep()
		{
			await bot.SendTextMessageAsync(message.From.Id, "Are you sure?", replyMarkup: buttons);
			NextState = SecondStep;
			return false;
		}

		private async Task<bool> SecondStep()
		{
			var answer = message.Text;
			if (answer.Equals("Yes"))
			{
				Worker.ReastartWorker();
				await bot.SendTextMessageAsync(message.From.Id, "Worker was restarted");
			}
			return true;
		}
	}

}
