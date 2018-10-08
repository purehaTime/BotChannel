using BotChannel.DataManager;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using System;
using BotChannel.Model;
using Telegram.Bot.Types;

namespace BotChannel
{
	/// <summary>
	/// Backgroud worker for post to all grops in db
	/// Just create tasks for each group witsh will posting by infinity loop
	/// </summary>
	public class Worker
	{
		private ITelegramBotClient _bot { get; set; }

		public Worker(ITelegramBotClient telegramBot)
		{
			_bot = telegramBot;
		}

		public void StartPosting()
		{
			DbManager db = new DbManager();
			var groups = db.GetGroups();

			if (groups.Count > 0)
			{
				foreach (var group in groups)
				{
					Task.Run(() => PosterWork(group));
				}
			}
		}

		private async Task PosterWork(Group group)
		{
			try
			{
				//delay will be first, coz groups may be have diff interval, so just for stop spaming
				await Task.Delay(group.Interval); 
				while (true)
				{
					var post = new List<InputMediaBase>();
					DbManager db = new DbManager();
					//for get randomly interval
					var postsCount = db.GetAvalablePostForGroup(group);
					//get random avaliable post
					// make post for bot

					await _bot.SendMediaGroupAsync(group.GroupId, post);
					//make post as "posted"
				}
			}
			catch (Exception err)
			{
				await _bot.SendTextMessageAsync("", err.Message);
			}
		}
	}
}