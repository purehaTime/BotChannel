using BotChannel.DataManager;
using System.Threading.Tasks;
using Telegram.Bot;

namespace BotChannel.BotCommand.Commands
{
	public class StatisticsCommand : BaseCommand, ICommand
	{
		private DbManager dbManager;

		public StatisticsCommand(ITelegramBotClient clientBot) : base(clientBot)
		{
			NextState = FirstStep;
			dbManager = new DbManager();
		}
		
		private async Task<bool> FirstStep()
		{
			var result = await ChooseGroupStep(dbManager, "Choose group to show stats:");
			NextState = SecondStep;

			return false;
		}

		private async Task<bool> SecondStep()
		{
			var group = dbManager.GetGroupByName(message.Text);
			if (group != null)
			{
				var allAdvert = dbManager.GetAdvertsByGroup(group);
				var availableContent = dbManager.GetCountAvailablePostForGroup(group);
				var runAdverts = Worker.GetRunningAdverts(group);

				await bot.SendTextMessageAsync(message.From.Id, $"Adverts: Run - {runAdverts}, available - {allAdvert.Count}, Available content: {availableContent}");
				return true;
			}
			await bot.SendTextMessageAsync(message.From.Id, "It seems, chosed group was not found");
			return true;
		}
	}

}
