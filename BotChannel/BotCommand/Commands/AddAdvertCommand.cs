using BotChannel.DataManager;
using BotChannel.Model;
using System.Threading.Tasks;
using Telegram.Bot;

namespace BotChannel.BotCommand.Commands
{
	public class AddAdvertCommand : BaseCommand, ICommand
	{
		private DbManager dbManager;
		private Advert advert;
		private Group groupForAdvert;

		public AddAdvertCommand(ITelegramBotClient clientBot) : base(clientBot)
		{
			NextState = FirstStep;
			advert = new Advert();
			dbManager = new DbManager();
		}
		
		private async Task<bool> FirstStep()
		{
			var result = await ChooseGroupStep(dbManager, "Choose group to add advert:");
			NextState = SecondStep;

			return result;
		}

		private async Task<bool> SecondStep()
		{
			var selectedGroup = message.Text;
			groupForAdvert = dbManager.GetGroupByName(selectedGroup);
			if (groupForAdvert == null)
			{
				await bot.SendTextMessageAsync(message.From.Id, "It seems, group was deleted");
				return true;
			}
			await bot.SendTextMessageAsync(message.From.Id, "Enter a text for advert");
			NextState = ThridStep;

			return false;

		}

		private async Task<bool> ThridStep()
		{
			advert.Message = message.Text;
			await bot.SendTextMessageAsync(message.From.Id, "Enter a interval for this advert");
			NextState = FourthStep;

			return false;
		}

		private async Task<bool> FourthStep()
		{
			if (int.TryParse(message.Text, out int interval))
			{
				advert.Interval = interval;
				advert.GroupId = groupForAdvert.GroupId;
				dbManager.AddAdvert(advert);
				await bot.SendTextMessageAsync(message.From.Id, $"The advert was added for {groupForAdvert.Title} with {advert.Interval}");
				return true;
			}

			await bot.SendTextMessageAsync(message.From.Id, $"Incorrect interval, try again");
			return false;
		}
	}

}
