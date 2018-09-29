using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;


namespace BotChannel.BotCommand.AddVkPost
{
	public abstract class BaseCommand
	{
		public Func<Task<bool>> NextState { get; set; }

		protected ITelegramBotClient bot;
		protected Message message;

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
	}

}
