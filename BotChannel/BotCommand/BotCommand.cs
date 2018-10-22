using System;
using System.Collections.Generic;
using System.Linq;
using BotChannel.BotCommand.Commands;
using Telegram.Bot;

namespace BotChannel.BotCommand
{
	public static class BotCommands
	{
		private static Dictionary<string, Func<ITelegramBotClient, ICommand>> CommandStore { get; set; }

		// init storage of commands for factory command
		static BotCommands()
		{
			CommandStore = new Dictionary<string, Func<ITelegramBotClient, ICommand>>
			{
				{"/addpost", (bot) => new AddPostCommand(bot)},
				{"/stats", (bot) => new StatisticsCommand(bot)},
				{"/addgroup", (bot) => new AddGroupCommand(bot)},
				{"/editgroup", (bot) => new EditGroupCommand(bot)},
				{"/addadvert", (bot) => new AddAdvertCommand(bot)},
				{"/stopposting", (bot) => new StopPostingCommand(bot)},
				{"/deleteadvert", (bot) => new DeleteAdvertCommand(bot)}
			};
		}

		public static ICommand GetCommandAction(ITelegramBotClient bot, string command)
		{
			var result = CommandStore.FirstOrDefault(f => f.Key.Equals(command, StringComparison.InvariantCultureIgnoreCase));
			return result.Value?.Invoke(bot);
		}

		public static List<string> GetCommands()
		{
			return new List<string>(CommandStore.Keys);
		}
	}
}
