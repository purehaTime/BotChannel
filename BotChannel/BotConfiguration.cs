using System.Collections.Generic;

namespace BotChannel
{
	public class BotConfiguration
	{
		public string BotToken { get; set; }
		public string Socks5Host { get; set; }
		public int? Socks5Port { get; set; }
		public string ProxyLogin { get; set; }
		public string ProxyPassword { get; set; }

		public List<long> UserAccess { get; set; } = new List<long>();
	}
}