using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using BotChannel.BotCommand;


namespace BotChannel.Services
{
	public class UpdateService : IUpdateService
	{
		private readonly IBotService _botService;

		private static Message _previosMessage = null;

		public UpdateService(IBotService botService)
		{
			_botService = botService;
			_previosMessage = _previosMessage ?? new Message();

		}

		public async Task EchoAsync(Update update)
		{
			if (update.Type != UpdateType.Message)
			{
				return;
			}

			var message = update.Message;
			var idUser = message.From.Id;

			//only for valid users
			if (!_botService.UserAccess.Any(u => u == idUser))
			{
				return;
			}

			//if it is has command - execute next state	of command
			var action = _botService.UserActions.FirstOrDefault(user => user.IdUser == idUser);

			if (action?.Command != null)
			{
				var isComplete = await action.Command.Action(message);
				if (isComplete)
				{
					_botService.UserActions.Remove(action);
				}
				return;
			}

			if (message.Type == MessageType.Text)
			{
				var command = BotCommands.CommandFactory.FirstOrDefault(key => key.Key == message.Text);
				if (command.Value != null)
				{
					_botService.UserActions.Add(new UserAction
					{
						Command = command.Value,
						IdUser = message.From.Id
					});
				}
			}
		}
	}
}
