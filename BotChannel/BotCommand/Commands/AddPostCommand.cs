using BotChannel.DataManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace BotChannel.BotCommand.AddVkPost
{
	public class AddPostCommand : ICommand
	{
		public Func<Task<bool>> NextState { get; set; }

		private ITelegramBotClient bot;
		private Message message;
		private DbManager dbManager;

		public AddPostCommand()
		{
			dbManager = new DbManager();
		}

		public async Task<bool> Action(Message updateMessage)
		{
			if (updateMessage.Text.Equals("/cancel"))
			{
				return true;
			}

			message = updateMessage;
			return await NextState();
		}

		public AddPostCommand(ITelegramBotClient clientBot)
		{
			bot = clientBot;
			NextState = FirstStep;
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
			var request = await bot.SendTextMessageAsync(message.From.Id, "Choose group to add:", replyMarkup: replyButtons);
			
			return false;
		}

		private async Task<bool> SecondStep()
		{
			var p = await bot.SendTextMessageAsync(message.From.Id, "Send links (separate by ',') direct or vk post/album");
			return false;
		}
	}

}
