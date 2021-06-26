using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SQLite;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace VirtualListViewSample
{
	public class TrackInfo
	{
		public int TrackId { get; set; }

		public string TrackName { get; set; }

		public int AlbumId { get; set; }

		public string AlbumTitle { get; set; }

		public int ArtistId { get; set; }

		public string ArtistName { get; set; }

		public int GenreId { get; set; }

		public string GenreName { get; set; }

		public int TrackLength { get; set; }
	}

	public class AlbumInfo
	{
		public int AlbumId { get; set; }

		public string AlbumTitle { get; set; }

		public int ArtistId { get; set; }

		public string ArtistName { get; set; }

		public int TrackCount { get; set; }
	}

	public class SqliteGroupedAdapter<TGroup, TItem> : IVirtualListViewAdapter
	{
		public SqliteGroupedAdapter()
		{
			var path = Path.Combine(Microsoft.Maui.Essentials.FileSystem.CacheDirectory, "chinook.db");

			if (!File.Exists(path))
			{
				// note that the prefix includes the trailing period '.' that is required
				var assembly = IntrospectionExtensions.GetTypeInfo(typeof(App)).Assembly;
				using (var stream = assembly.GetManifestResourceStream(nameof(VirtualListViewSample) + ".chinook.db"))
				using (var outStream = File.Create(path))
					stream.CopyTo(outStream);
			}

			Db = new SQLiteConnection(path);
		}

		public SQLiteConnection Db { get; }

		public void ReloadData()
		{
			cachedAlbumInfo.Clear();
		}

		Dictionary<int, AlbumInfo> cachedAlbumInfo = new Dictionary<int, AlbumInfo>();

		const string AlbumSectionSQL = @"
SELECT 
	album.AlbumId AS AlbumId,
	album.Title AS AlbumTitle,
	album.ArtistId AS ArtistId,
	artist.Name AS ArtistName,
	count(*) as TrackCount
FROM Album album
	INNER JOIN Artist artist ON artist.ArtistId = album.ArtistId
	INNER JOIN Track track ON track.AlbumId = album.AlbumId
GROUP BY album.AlbumId
ORDER BY AlbumId
LIMIT 1 OFFSET ?
";

		int? sectionsCount = null;

		public int Sections
			=> sectionsCount ??= Db.ExecuteScalar<int>("SELECT COUNT(AlbumId) FROM Album");

		const string TrackSQL = @"
SELECT
	track.TrackId AS TrackId,
	track.Name as TrackName,
	track.AlbumId as AlbumId,
	album.Title AS AlbumTitle,
	album.ArtistId AS ArtistId,
	artist.Name AS ArtistName,
	track.GenreId AS GenreId,
	genre.Name AS GenreName,
	track.Milliseconds AS TrackLength
FROM Track track
	INNER JOIN Album album ON album.AlbumId = track.AlbumId
	INNER JOIN Artist artist ON artist.ArtistId = album.ArtistId
	INNER JOIN Genre genre ON genre.GenreId = track.GenreId
WHERE
	album.AlbumId = ?
	AND artist.ArtistId = ?
	LIMIT 1 OFFSET ?
";


		public object Item(int sectionIndex, int itemIndex)
		{
			var albumInfo = Section(sectionIndex) as AlbumInfo;

			return Db.FindWithQuery<TrackInfo>(
				TrackSQL,
				albumInfo.AlbumId,
				albumInfo.ArtistId,
				itemIndex);
		}

		public int ItemsForSection(int sectionIndex)
			=> (Section(sectionIndex) as AlbumInfo)?.TrackCount ?? 0;

		public object Section(int sectionIndex)
		{
			if (cachedAlbumInfo.ContainsKey(sectionIndex))
				return cachedAlbumInfo[sectionIndex];

			var albumInfo = Db.FindWithQuery<AlbumInfo>(AlbumSectionSQL, sectionIndex);

			if (albumInfo != null)
				cachedAlbumInfo.Add(sectionIndex, albumInfo);

			return albumInfo;
		}
	}

}
