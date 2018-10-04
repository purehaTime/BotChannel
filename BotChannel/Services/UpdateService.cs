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

		private static int? _previosMessageId { get; set; }

		public UpdateService(IBotService botService)
		{
			_botService = botService;
			_previosMessageId = _previosMessageId ?? 0;

		}
		/// <summary>
		/// Main. check valid user? then seeing users states command. 
		/// If nothing found - search cammands to start new state for user
		/// </summary>
		/// <param name="update">telegram message</param>
		/// <returns></returns>
		public async Task EchoAsync(Update update)
		{
			if (update.Type != UpdateType.Message)
			{
				return;
			}

			var message = update.Message;

			if (_previosMessageId == message.MessageId)
			{
				return;	//prevent "spaming" from telegram server
			}
			_previosMessageId = message.MessageId;

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
				var command = BotCommands.GetCommand(_botService.Client, message.Text);
				if (command != null)
				{
					_botService.UserActions.Add(new UserAction
					{
						Command = command,
						IdUser = message.From.Id
					});
					await command.Action(message);
				}
			}
		}
	}
}
