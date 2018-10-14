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
	public class Worker
	{
		private IBotService _botService { get; set; }
		//to manage of worker tasks
		private static Dictionary<int, Task> taskManager { get; set; }

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

					var adverts = db.GetAdvertsByGroup(group);
					//coz group may content multiple adverts
					//and it doesn't make sense	to post advert without content
					foreach (var advert in adverts)
					{
						Task.Run(() => PosterWork(group));
					}
				}
			}
		}

		private async Task PosterWork(Group group)
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

		private async Task AdvertWork(Advert advert)
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