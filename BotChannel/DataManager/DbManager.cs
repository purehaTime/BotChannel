using BotChannel.Model;
using LiteDB;
using System;
using System.Collections.Generic;

namespace BotChannel.DataManager
{
	public class DbManager
	{
		public static string Dbfile { get; set; }

		public DbManager()
		{
			Dbfile = Dbfile ?? "db.db";
		}

		public void AddNewPost(Content content)
		{
			using (var db = new LiteDatabase(Dbfile))
			{
				var contents = db.GetCollection<Content>("Contents");
				contents.Insert(content);
				contents.EnsureIndex(x => x.PhotoList);
			}
		}

		public bool AddGroup(Group model)
		{
			using (var db = new LiteDatabase(Dbfile))
			{
				var groups = db.GetCollection<Group>("Groups");
				if (!groups.Exists(e => e.GroupId == model.GroupId))
				{
					groups.Insert(model);
					groups.EnsureIndex(x => x.GroupId);
					return true;
				}
			}
			return false;
		}

		public List<Group> GetGroups()
		{
			List<Group> groups = new List<Group>();
			using (var db = new LiteDatabase(Dbfile))
			{
				var groupList = db.GetCollection<Group>("Groups").FindAll();
				groups.AddRange(groupList);
			}
			return groups;
		}

		public Group GetGroupByName(string name)
		{
			Group group = null;
			using (var db = new LiteDatabase(Dbfile))
			{
				 group = db.GetCollection<Group>("Groups").FindOne(f => f.Title.Equals(name, StringComparison.InvariantCultureIgnoreCase));
			}
			return group;
		}

		public void UpdateGroup(Group updatingGroup)
		{
			using (var db = new LiteDatabase(Dbfile))
			{
				var update = db.GetCollection<Group>("Groups");
				update.Update(updatingGroup);
			}
		}

		public List<Group> GetGroupsWithAvaliableContent()
		{
			List<Group> groups = new List<Group>();
			using (var db = new LiteDatabase(Dbfile))
			{
				var groupList = db.GetCollection<Group>("Groups").FindAll();
				foreach (var group in groupList)
				{
					var postsCount = db.GetCollection<Content>("Contents").Count(cnt =>
						!cnt.Posted
						&& cnt.GroupId.Equals(group.GroupId));

					if (postsCount > 0)
					{
						groups.Add(group);
					}
				}
				
			}
			return groups;
		}

		public void AddAdvert(Advert advert)
		{
			using (var db = new LiteDatabase(Dbfile))
			{
				var adverts = db.GetCollection<Advert>("Adverts");
				adverts.Insert(advert);
				adverts.EnsureIndex(x => x.Id);
			}
		}

		/// <summary>
		/// Make content as posted, used UTC time
		/// </summary>
		/// <param name="post"></param>
		public void MakePosted(Content post)
		{
			using (var db = new LiteDatabase(Dbfile))
			{
				post.Posted = true;
				post.TimePosted = DateTime.UtcNow;
				var contents = db.GetCollection<Content>("Contents");
				contents.Update(post);
			}
		}

		/// <summary>
		/// Get numbers of avaliable content by group
		/// </summary>
		/// <param name="group"></param>
		/// <returns></returns>
		public int GetAvalablePostForGroup(Group group)
		{
			using (var db = new LiteDatabase(Dbfile))
			{
				var postsCount = db.GetCollection<Content>("Contents").Count(cnt =>
						!cnt.Posted
						&& cnt.GroupId.Equals(group.GroupId));

				return postsCount;
			}
		}

		public Content GetContentByNumber(int number, string groupId)
		{
			using (var db = new LiteDatabase(Dbfile))
			{
				var allPosts = db.GetCollection<Content>("Contents").Find(f => 
							!f.Posted
							&& f.GroupId.Equals(groupId));
				var list = new List<Content>(allPosts);
				//prevent OutOfRange exception
				var result = (list.Count >= number) ? list[number] : null;
				return result;
			}
		}

		public List<Advert> GetAdvertsByGroup(Group group)
		{
			using (var db = new LiteDatabase(Dbfile))
			{
				var results = new List<Advert>();
				var adverts = db.GetCollection<Advert>("Adverts").Find(f => f.GroupId.Equals(group.GroupId));
				results.AddRange(adverts);

				return results;
			}

		}

		public void DeleteAdvert(Advert advert)
		{
			using (var db = new LiteDatabase(Dbfile))
			{
				var result = db.GetCollection<Advert>("Adverts").Delete(d => d.Id == advert.Id);
			}
		}
	}
}
