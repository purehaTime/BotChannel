using System;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace BotChannel.BotCommand
{
	public interface ICommand
	{
		Task<bool> Action(Message message);
		Func<Task<bool>> NextState { get; set; }
	}
}
