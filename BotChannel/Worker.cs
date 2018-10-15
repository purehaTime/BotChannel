using BotChannel.DataManager;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using System;
using BotChannel.Model;
using Telegram.Bot.Types;
using BotChannel.Services;
using System.Linq;

namespace BotChannel
{
	/// <summary>
	/// Backgroud worker for post to all grops in db
	/// Just create tasks for each group wich will be posting by infinity loop
	/// </summary>
	public static class Worker
	{
		//to manage of worker tasks
		private static Dictionary<int, Task> _taskManager { get; set; }

		//bad way to use this variant
		private static IBotService _botService { get; set; }

		private static List<TaskWorker> _tasksWorkerPost { get; set; }
		private static List<TaskWorker> _tasksWorkerAdvert { get; set; }

		public static void StartPosting(IBotService botService)
		{
			DbManager db = new DbManager();
			_tasksWorkerPost = new List<TaskWorker>();
			_tasksWorkerAdvert = new List<TaskWorker>();
			_botService = botService;

			var groups = db.GetGroupsWithAvaliableContent();

			if (groups.Count > 0)
			{
				foreach (var group in groups)
				{
					var postTask = Task.Run(() => PosterWork(group));
					//need for manage tasks
					_tasksWorkerPost.Add(new TaskWorker
					{
						Id = group.GroupId,
						TaskInWork = postTask
					});

					var adverts = db.GetAdvertsByGroup(group);
					//coz group may content multiple adverts
					//and it doesn't make sense	to post advert without content
					foreach (var advert in adverts)
					{
						var advertTask = Task.Run(() => AdvertWork(advert));
						_tasksWorkerPost.Add(new TaskWorker
						{
							Id = advert.Id.ToString(),
							TaskInWork = advertTask
						});
					}
				}
			}
		}

		private static async Task PosterWork(Group group)
		{
			var bot = _botService.Client;
			var mainUser = _botService.UserAccess.FirstOrDefault();	 // the first user in config will be used for send inforamtion
			try
			{
				while (true)
				{
					//delay will be first, coz groups may be have diff interval, so just for stop spaming
					await Task.Delay(group.Interval);
					
					DbManager db = new DbManager();
					//for randomly post
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
						var postList = new List<InputMediaBase>();

						foreach (var photoLink in post.PhotoList)
						{
							var photo = new InputMediaPhoto();
							photo.Media = new InputMedia(photoLink);
							postList.Add(photo);
						}

						await bot.SendMediaGroupAsync(group.GroupId, postList);
						db.MakePosted(post);
					}
					//make post as "posted"
				}
			}
			catch (Exception err)
			{
				await bot.SendTextMessageAsync(mainUser, err.Message);
			}
		}

		private static async Task AdvertWork(Advert advert)
		{
			var bot = _botService.Client;
			var mainUser = _botService.UserAccess.FirstOrDefault();  // the first user in config will be used for send inforamtion
			try
			{
				while (true)
				{
					await Task.Delay(advert.Interval);
					await bot.SendTextMessageAsync(advert.GroupId, advert.Message);
				}

			}
			catch (Exception err)
			{
				await bot.SendTextMessageAsync(mainUser, err.Message);
			}
		}
	}
}