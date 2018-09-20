using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VkNet;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;

namespace BotChannel.Parsers
{
	public class VkParser
	{
		public static string AccessToken {get;set;}

		private VkApi vkApi { get; set; }

		public VkParser()
		{
			vkApi = new VkApi();

			vkApi.Authorize(new ApiAuthParams
			{
				AccessToken = AccessToken
			});
		}

		public List<string> GetLinksFromPost(string linkPost)
		{
			var listLinks = new List<string>();
			try
			{
				var id = ParseId(linkPost);

				var post = vkApi.Wall.GetById(new[] { id });
				
				if (post.WallPosts.Any())
				{
					foreach (var attach in post.WallPosts[0].Attachments)
					{
						if (attach.Instance is Photo photo)
						{
							var maxSize = photo.Sizes.Max(w => w.Width);
							var valid = photo.Sizes.First(w => w.Width == maxSize);
							listLinks.Add(valid.Url.AbsoluteUri);
						}
					}
				}
			}
			catch (Exception err)
			{
				// ignored
			}

			return listLinks;
		}

		public async Task<List<string>> GetLinksFromAlbum(string linkAlbum)
		{
			var listLinks = new List<string>();

			var id = ParseId(linkAlbum);
			var requestParam = new PhotoGetParams
			{
				Count = 50,
				OwnerId = Convert.ToInt64(id.Split("_")[0]),
				AlbumId = PhotoAlbumType.Id(Convert.ToInt64(id.Split("_")[1])),
				Offset = 0
			};
			int total = 1;
			while (total > 0)
			{
				var photos = await vkApi.Photo.GetAsync(requestParam);
				foreach (var photo in photos)
				{
					var maxSize = photo.Sizes.Max(w => w.Width);
					var valid = photo.Sizes.First(w => w.Width == maxSize);
					listLinks.Add(valid.Url.AbsoluteUri);
				}
				requestParam.Offset += 50;
				total = photos.Count;
				Thread.Sleep(100); //prevent spaming vk API
			}

			return listLinks;
		}

		private string ParseId(string linkId)
		{
			var splitter = linkId.Split("/");
			return splitter.Last();
		}
	}
}
