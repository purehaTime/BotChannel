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
		/// Main.validation then seeing users states command and added new stack if command correct. 
		/// If nothing found - search cammands to start new state for user
		/// </summary>
		/// <param name="update">telegram message</param>
		/// <returns></returns>
		public async Task EchoAsync(Update update)
		{
			 // check is anything correct
			if (!Validation(update))
				return;

			if (update.Message == null)//button click
			{
				update.Message = update.CallbackQuery.Message;
				update.Message.From = update.CallbackQuery.From;
				update.Message.Text = update.CallbackQuery.Data;
			}

			var message = update.Message;
			var idUser =  message.From.Id;
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
				await AddNewCommandStack(message, idUser);
			}
		}

		private async Task AddNewCommandStack(Message message, long idUser)
		{
			var command = BotCommands.GetCommandAction(_botService.Client, message.Text);
			var isExists = _botService.UserActions.Any(user => user.IdUser == idUser);
			if (command != null && !isExists)
			{
				_botService.UserActions.Add(new UserAction
				{
					Command = command,
					IdUser = message.From.Id
				});
				await command.Action(message);
				return;
			}

			await DefaultCommand(message);
		}
		
		/// <summary>
		/// Validation on: messageType is text, access for valid user, and new messages
		/// </summary>
		/// <param name="update"></param>
		/// <returns></returns>
		private bool Validation(Update update)
		{
			if (update.Type != UpdateType.Message && update.Type != UpdateType.CallbackQuery)
			{
				return false;
			}

			var message = update.Message ?? update.CallbackQuery.Message;

			if (_previosMessageId == message?.MessageId && update.CallbackQuery == null)
			{
				return false; //prevent "spaming" from telegram server
			}
			_previosMessageId = message.MessageId;

			var idUser = update.CallbackQuery == null ? message.From.Id : update.CallbackQuery.From.Id;

			//only for valid users
			if (!_botService.UserAccess.Any(u => u == idUser))
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Send a list of available commands
		/// </summary>
		/// <returns></returns>
		private async Task DefaultCommand(Message message)
		{
			var list = BotCommands.GetCommands();

			var result = "Available commands: \r\n";
			result += string.Join("\r\n", list);

			await _botService.Client.SendTextMessageAsync(message.From.Id, result);
		}
	}
}
