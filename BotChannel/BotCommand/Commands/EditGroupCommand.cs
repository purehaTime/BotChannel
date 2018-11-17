using BotChannel.DataManager;
using BotChannel.Model;
using System.Threading.Tasks;
using Telegram.Bot;

namespace BotChannel.BotCommand.Commands
{
	public class EditGroupCommand : BaseCommand, ICommand
	{
		private DbManager dbManager;
		private Group groupEdit;

		public EditGroupCommand(ITelegramBotClient clientBot) : base(clientBot)
		{
			NextState = FirstStep;
			dbManager = new DbManager();
		}
		
		private async Task<bool> FirstStep()
		{
			var result = await ChooseGroupStep(dbManager, "Choose group to edit:");
			NextState = SecondStep;

			return result;
		}

		private async Task<bool> SecondStep()
		{
			var selectedGroup = message.Text;
			groupEdit = dbManager.GetGroupByName(selectedGroup);

			if (groupEdit == null)
			{
				await bot.SendTextMessageAsync(message.From.Id, "It seems, chosed group was not found");
				return true;
			}

			await bot.SendTextMessageAsync(message.From.Id, $"The old interval of posting is - {groupEdit.Interval} minutes. Enter new interval:");
			NextState = ThridStep;
			return false;

		}

		private async Task<bool> ThridStep()
		{
			var interval = message.Text.Trim();
			if (int.TryParse(interval, out var valid))
			{
				groupEdit.Interval = valid;
				dbManager.UpdateGroup(groupEdit);
				await bot.SendTextMessageAsync(message.Chat.Id, "Complete update group");
				return true;
			}

			await bot.SendTextMessageAsync(message.Chat.Id, "Incorrect interval, try again: ");
			return false;
		}
	}

}
