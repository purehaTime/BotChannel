using System.Collections.Generic;
using System.Net;
using BotChannel.BotCommand;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace BotChannel.Services
{
	public class BotService : IBotService
	{
		private static TelegramBotClient _client;
		
		public TelegramBotClient Client {
			get => _client;
			set => _client = value;
		}

		public List<long> UserAccess { get; set; }
		//stack of current commands(dialogs) executing
		public List<UserAction> UserActions { get; set; }

		public BotService(IOptions<BotConfiguration> config)
		{
			if (_client != null) return;

			//list of valid users (telegram id)
			UserAccess = config.Value.UserAccess;
			UserActions = new List<UserAction>();
			var value = config.Value;
			// use proxy (with credential or not) if configured in appsettings.*.json
			Client = string.IsNullOrEmpty(value.Socks5Host)
				? new TelegramBotClient(value.BotToken)
				: new TelegramBotClient(value.BotToken,
						string.IsNullOrEmpty(value.ProxyLogin) 
						? new WebProxy(value.Socks5Host, value.Socks5Port ?? 8080)
						: new WebProxy($"{value.Socks5Host}:{value.Socks5Port ?? 8080}"
							, false, null, new NetworkCredential(value.ProxyLogin, value.ProxyPassword)));
			//initialise command factory for bot
			BotCommands.Initialize(Client);
		}
	}
}
