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
			if (updateMessage.Text.Equals("/cancel"))
			{
				return true;
			}

			message = updateMessage;
			return await NextState();
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

			var oddRow = new List<InlineKeyboardButton>();
			var evenRow = new List<InlineKeyboardButton>();
			for (var i = 0; i < groupList.Count; i++) //it is needs for two colums button builds
			{
				var text = groupList[i].Title ?? groupList[i].GroupId;
				if (i % 2 == 0)
				{
					evenRow.Add(InlineKeyboardButton.WithCallbackData(text));
					continue;
				}
				oddRow.Add(InlineKeyboardButton.WithCallbackData(text));
			}

			var buttons = new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>> {
				 oddRow,
				 evenRow
			});

			await bot.SendTextMessageAsync(message.From.Id, textToSend, replyMarkup: buttons);
			return false;
		}
	}

}
