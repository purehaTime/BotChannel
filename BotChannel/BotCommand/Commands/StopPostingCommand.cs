using BotChannel.DataManager;
using System.Threading.Tasks;
using Telegram.Bot;

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
			var result = await ChooseGroupStep(dbManager, "Choose group to stop posting:");
			NextState = SecondStep;

			return result;
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
