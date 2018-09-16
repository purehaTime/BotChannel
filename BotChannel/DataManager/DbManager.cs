using BotChannel.Model;
using LiteDB;
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

		public void AddData(Content content)
		{
			using (var db = new LiteDatabase(Dbfile))
			{
				// Get customer collection
				var contents = db.GetCollection<Content>("Contents");

				// Insert new customer document (Id will be auto-incremented)
				contents.Insert(content);

				// Index document using a document property
				contents.EnsureIndex(x => x.PhotoList);
			}
		}

		public void AddGroup(Group model)
		{
			using (var db = new LiteDatabase(Dbfile))
			{
				// Get customer collection
				var groups = db.GetCollection<Group>("Groups");

				// Insert new customer document (Id will be auto-incremented)
				groups.Insert(model);

				// Index document using a document property
				groups.EnsureIndex(x => x.GroupId);
			}
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
	}
}
