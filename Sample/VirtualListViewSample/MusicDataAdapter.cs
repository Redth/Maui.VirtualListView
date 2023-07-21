using LiteDB;
using Microsoft.Maui.Adapters;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace VirtualListViewSample
{
	public static class MusicDatabase
	{
		public static LiteDatabase Open()
		{
		var files = new[] { "chinook.litedb" };

			foreach (var file in files)
			{
				var path = Path.Combine(FileSystem.CacheDirectory, file);

				var txt = string.Empty;

				using (var stream = typeof(App).Assembly.GetManifestResourceStream("VirtualListViewSample." + file))
				using (var sw = File.Create(path))
					stream.CopyTo(sw);
			}

			var dbPath = Path.Combine(FileSystem.CacheDirectory, files.First());

			return new LiteDatabase(dbPath);
		}
	}

	public class MusicDataAdapter : VirtualListViewAdapterBase<AlbumInfo, TrackInfo>
	{
		public MusicDataAdapter()
		{
			database ??= MusicDatabase.Open();

			albums = database.GetCollection<AlbumInfo>("albums").FindAll().OrderBy(a => a.AlbumId).ToList();
			tracks = database.GetCollection<TrackInfo>("tracks");
		}

		LiteDatabase database;
		readonly List<AlbumInfo> albums;
		readonly ILiteCollection<TrackInfo> tracks;
		
		public override int Sections
			=> albums.Count();

		public override TrackInfo Item(int sectionIndex, int itemIndex)
		{
			var section = Section(sectionIndex) as AlbumInfo;

			var t = tracks.Query().Where(t => t.AlbumId == section.AlbumId).OrderBy(t => t.TrackId).Skip(itemIndex).Limit(1).First();

			t.SectionIndex = sectionIndex;
			t.ItemIndex = itemIndex;

			return t;
		}

		public override int ItemsForSection(int sectionIndex)
			=> (Section(sectionIndex) as AlbumInfo).TrackCount;

		public override AlbumInfo Section(int sectionIndex)
		{
			var section = albums[sectionIndex] as AlbumInfo;
			if (section.TrackCount <= 0)
				section.TrackCount = tracks.Count(t => t.AlbumId == section.AlbumId);

			return section;
		}
	}


	public class AlbumSection : ObservableCollection<TrackInfo>
	{
		public AlbumSection(AlbumInfo album, IEnumerable<TrackInfo> tracks)
			: base(tracks)
		{
			Album = album;
		}

		public AlbumInfo Album { get; private set; }
	}

	
}
