using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SQLite;
using Xamarin.CommunityToolkit.UI.Views;

namespace VirtualListViewSample
{
	public class SqliteGroupedAdapter<TGroup, TItem> : IVirtualListViewAdapter
		where TGroup : IList<TItem>
	{

		public class DbPersonGroup
		{
			public string GroupName { get; set; }

			public int GroupCount { get; set; }
		}

		public SqliteGroupedAdapter()
		{
			var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "sampledata.db");
			
			if (!File.Exists(path))
			{
				// note that the prefix includes the trailing period '.' that is required
				var assembly = IntrospectionExtensions.GetTypeInfo(typeof(App)).Assembly;
				using (var stream = assembly.GetManifestResourceStream(nameof(VirtualListViewSample) + ".sampledata.db"))
				using (var outStream = File.Create(path))
					stream.CopyTo(outStream);
			}

			Db = new SQLiteConnection(path);
			Db.CreateTable<Person>();
		}

		public SQLiteConnection Db { get; }

		List<DbPersonGroup> sections;

		List<DbPersonGroup> GetSections()
		{
			if (sections == null)
				sections = Db.Query<DbPersonGroup>("select IndexCharacter as GroupName, count(IndexCharacter) as GroupCount from Person group by IndexCharacter ORDER BY IndexCharacter");

			return sections;
		}

		public int Sections
			=> GetSections().Count;

		public object Item(int sectionIndex, int itemIndex)
			=> Db.Query<Person>(
				"select * from Person Where IndexCharacter = ? ORDER BY LastName, FirstName LIMIT 1 OFFSET " + itemIndex,
				GetSections()[sectionIndex].GroupName)
				?.FirstOrDefault();

		public int ItemsForSection(int sectionIndex)
 			=> GetSections()[sectionIndex].GroupCount;

		public object Section(int sectionIndex)
			=> GetSections()[sectionIndex];
	}

}
