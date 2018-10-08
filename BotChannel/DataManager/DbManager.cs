﻿using BotChannel.Model;
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

		public long GetAvalablePostForGroup(Group group)
		{
			using (var db = new LiteDatabase(Dbfile))
			{
				var postsCount = db.GetCollection<Content>().Count(cnt =>
						(!cnt.Posted)
						&& cnt.GroupId.Equals(group.GroupId, StringComparison.InvariantCultureIgnoreCase));

				return postsCount;
			}
		}
	}
}
