using BotChannel.DataManager;
using BotChannel.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

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
			var groupList = dbManager.GetGroups();
			var buttons = new List<KeyboardButton>();

			foreach (var group in groupList)
			{
				buttons.Add(new KeyboardButton(group.Title));
			}

			var replyButtons = new ReplyKeyboardMarkup(buttons);
			var request = await bot.SendTextMessageAsync(message.From.Id, "Choose group to edit:", replyMarkup: replyButtons);

			NextState = SecondStep;
			return false;
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

			await bot.SendTextMessageAsync(message.From.Id, $"The old interval of posting is - {groupEdit.Interval} seconds. Enter new interval:");
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
