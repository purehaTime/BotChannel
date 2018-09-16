using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace BotChannel.Services
{
	public interface IUpdateService
	{
		Task EchoAsync(Update update);
	}
}
