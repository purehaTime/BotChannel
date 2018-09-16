using System;


namespace BotChannel.Model
{
	public class Content
	{
		public int Id { get; set; }
		public string GroupId { get; set; }
		public string[] PhotoList { get; set; }
		public bool Posted { get; set; }
		public DateTime TimePosted { get; set; }
		public string MessagerType { get; set; }
	}
}
