using BotChannel.DataManager;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using System;
using BotChannel.Model;
using Telegram.Bot.Types;
using BotChannel.Services;
using System.Linq;
using Telegram.Bot.Types.InputFiles;

namespace BotChannel
{
	/// <summary>
	/// Backgroud worker for post to all grops in db
	/// Just create tasks for each group wich will be posting by infinity loop
	/// </summary>
	public class Worker
	{
		private IBotService _botService { get; set; }

		public Worker(IBotService botService)
		{
			_botService = botService;
		}

		public void StartPosting()
		{
			DbManager db = new DbManager();
			var groups = db.GetGroupsWithAvaliableContent();

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
			var bot = _botService.Client;
			var mainUser = _botService.UserAccess.FirstOrDefault();	 // the first user in config will be used for send inforamtion
			try
			{
				//delay will be first, coz groups may be have diff interval, so just for stop spaming
				await Task.Delay(group.Interval);
				while (true)
				{
					var postList = new List<InputMediaBase>();
					DbManager db = new DbManager();
					//for randomly interval
					var postsCount = db.GetAvalablePostForGroup(group);
					if (postsCount == 0)
					{
						await bot.SendTextMessageAsync(mainUser, $"All content for {group.GroupId}:{group.Title} was posted !");
						return;
					}
					var rnd = new Random().Next(postsCount);
					var post = db.GetContentByNumber(rnd, group.GroupId);

					if (post != null)
					{
						foreach (var photoLink in post.PhotoList)
						{
							var photo = new InputMediaPhoto();
							photo.Media = new InputMedia(photoLink);
							postList.Add(photo);
						}

						await bot.SendMediaGroupAsync(group.GroupId, postList);
					}
					//make post as "posted"
				}
			}
			catch (Exception err)
			{
				await bot.SendTextMessageAsync(mainUser, err.Message);
			}
		}
	}
}