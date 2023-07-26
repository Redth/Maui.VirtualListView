using LiteDB;
using Microsoft.Maui.Adapters;
using System.Collections.ObjectModel;

namespace VirtualListViewSample;

public static class MusicDatabase
{
	public static Lazy<LiteDatabase> Instance { get; }
		= new Lazy<LiteDatabase>(() => Open());

	static LiteDatabase Open()
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
		database ??= MusicDatabase.Instance.Value;

		albums = database.GetCollection<AlbumInfo>("albums").FindAll().OrderBy(a => a.AlbumId).ToList();
		tracks = database.GetCollection<TrackInfo>("tracks");
	}

	LiteDatabase database;
	readonly List<AlbumInfo> albums;
	readonly ILiteCollection<TrackInfo> tracks;

	public override int GetNumberOfSections()
		=> albums.Count;

	public override TrackInfo GetItem(int sectionIndex, int itemIndex)
	{
		var album = GetSection(sectionIndex);
		var t = tracks.Query().Where(t => t.AlbumId == album.AlbumId).OrderBy(t => t.TrackId).Skip(itemIndex).Limit(1).First();

		t.SectionIndex = sectionIndex;
		t.ItemIndex = itemIndex;

		return t;
	}

	Dictionary<int, int> cachedSectionCounts = new();

	public override int GetNumberOfItemsInSection(int sectionIndex)
	{
		if (cachedSectionCounts.TryGetValue(sectionIndex, out var count))
			return count;

		var albumId = GetSection(sectionIndex).AlbumId;

		count = tracks.Count(t => t.AlbumId == albumId);

		cachedSectionCounts.TryAdd(sectionIndex, count);
		return count;
	}

	public override AlbumInfo GetSection(int sectionIndex)
		=> albums[sectionIndex];

	public override void InvalidateData()
	{
		cachedSectionCounts.Clear();
		base.InvalidateData();
	}
}



