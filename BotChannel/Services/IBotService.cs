using System.Collections.Generic;
using BotChannel.BotCommand;
using Telegram.Bot;

namespace BotChannel.Services
{
	public interface IBotService
	{
		TelegramBotClient Client { get; }
		List<long> UserAccess { get; set; }
		List<UserAction> UserActions { get; set; }
	}
}