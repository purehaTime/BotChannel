using BotChannel.DataManager;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace BotChannel.BotCommand.Commands
{
	public abstract class BaseCommand
	{
		public Func<Task<bool>> NextState { get; set; }

		protected ITelegramBotClient bot;
		protected Message message;
		private object dbManager;

		public virtual async Task<bool> Action(Message updateMessage)
		{
			try
			{
				if (updateMessage.Text.Equals("/cancel"))
				{
					return true;
				}

				message = updateMessage;
				return await NextState();
			}
			catch (Exception err)
			{
				await bot.SendTextMessageAsync(message.From.Id, "Something wrong! " + err.Message);
			}
			return true;
		}

		public BaseCommand(ITelegramBotClient clientBot)
		{
			bot = clientBot;
			NextState = () => { return null; }; //default state
		}

		/// <summary>
		/// always false
		/// </summary>
		/// <param name="dbManager">//actually bad way by OOP concept</param>
		/// <param name="textToSend"></param>
		/// <returns></returns>
		protected async Task<bool> ChooseGroupStep(DbManager dbManager, string textToSend)
		{
			var groupList = dbManager.GetGroups();

			var columnButton = new List<InlineKeyboardButton>();
			var gridButtons = new List<List<InlineKeyboardButton>>();

			for (var i = 1; i <= groupList.Count; i++) //it is needs for two colums button builds
			{
				var text = groupList[i-1].Title ?? groupList[i-1].GroupId;

				columnButton.Add(InlineKeyboardButton.WithCallbackData(text));
				if (i % 2 == 0)
				{
					gridButtons.Add(columnButton);
					columnButton = new List<InlineKeyboardButton>();
				}
			}

			var buttons = new InlineKeyboardMarkup(gridButtons);

			await bot.SendTextMessageAsync(message.From.Id, textToSend, replyMarkup: buttons);
			return false;
		}
	}

}
