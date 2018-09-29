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
			// Add new command here
			// new command must be defined with base calss and ICommand interface
			// then defined a first step (in ctor)
			CommandFactory = new Dictionary<string, ICommand>
			{
				{ "/addpost", new AddPostCommand(telegramBot)},
				{ "/addgroup", new AddGroupCommand(telegramBot)}
			};
		}
	}
}
