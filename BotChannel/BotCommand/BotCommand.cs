using System.Collections.Generic;
using BotChannel.BotCommand.AddVkPost;
using Telegram.Bot;

namespace BotChannel.BotCommand
{
	public static class BotCommands
	{
		public static Dictionary<string, ICommand> CommandFactory { get; set; }

		public static void Initialize(ITelegramBotClient telegramBot)
		{
			CommandFactory = new Dictionary<string, ICommand>
			{
				{ "/addpost", new AddPostCommand(telegramBot)}
			};
		}
	}
}
