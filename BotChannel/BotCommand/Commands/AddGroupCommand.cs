using BotChannel.DataManager;
using BotChannel.Model;
using System.Threading.Tasks;
using Telegram.Bot;

namespace BotChannel.BotCommand.Commands
{
	public class AddGroupCommand : BaseCommand, ICommand
	{
		private DbManager dbManager;
		private Group groupSave;

		public AddGroupCommand(ITelegramBotClient clientBot) : base(clientBot)
		{
			NextState = FirstStep;
			dbManager = new DbManager();
		}
		
		private async Task<bool> FirstStep()
		{
			await bot.SendTextMessageAsync(message.Chat.Id, "First of all, bot will be add to group as admin. Enter Id of group");

			NextState = SecondStep;
			return false;
		}

		private async Task<bool> SecondStep()
		{
			var chatId = message.Text.Trim();
			var group = await bot.GetChatAsync(chatId);
			if (group != null)
			{
				groupSave = new Group
				{
					GroupId = chatId,
					Title = group.Title,
					Link = group.InviteLink
				};
				await bot.SendTextMessageAsync(message.Chat.Id, "Enter a interval (in seconds) of time of post");
				NextState = ThridStep;
				return false;
			}

			await bot.SendTextMessageAsync(message.Chat.Id, "I can't find this group (may be i'm not admin in that group?) Try again");
			NextState = FirstStep;
			return await NextState();
		}

		private async Task<bool> ThridStep()
		{
			var interval = message.Text.Trim();
			if (int.TryParse(interval, out var valid))
			{
				groupSave.Interval = valid;
				dbManager.AddGroup(groupSave);
				await bot.SendTextMessageAsync(message.Chat.Id, "Complete save group");
				return true;
			}
			await bot.SendTextMessageAsync(message.Chat.Id, "Incorrect interval, try again: ");
			return false;
		}
	}

}
