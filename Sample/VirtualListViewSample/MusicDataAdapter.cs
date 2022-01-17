using LiteDB;
using Microsoft.Maui;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace VirtualListViewSample
{
    public class MusicDataAdapter : IVirtualListViewAdapter, IDisposable
    {
        public MusicDataAdapter()
        {
            if (database == null)
            {
                var files = new[] { "chinook.litedb" };

                foreach (var file in files)
                {
                    var path = Path.Combine(Microsoft.Maui.Essentials.FileSystem.CacheDirectory, file);

                    var txt = string.Empty;

                    using (var stream = typeof(App).Assembly.GetManifestResourceStream("VirtualListViewSample." + file))
                    using (var sw = File.Create(path))
                        stream.CopyTo(sw);
                }

                var dbPath = Path.Combine(Microsoft.Maui.Essentials.FileSystem.CacheDirectory, files.First());

                database = new LiteDatabase(dbPath);
            }

            albums = database.GetCollection<AlbumInfo>("albums").FindAll().OrderBy(a => a.AlbumId).ToList();
            tracks = database.GetCollection<TrackInfo>("tracks");
        }

        static LiteDatabase database;
        readonly List<AlbumInfo> albums;
        readonly ILiteCollection<TrackInfo> tracks;
        
        public int Sections
            => albums.Count();

        public object Item(int sectionIndex, int itemIndex)
		{
            var section = Section(sectionIndex) as AlbumInfo;

            var t = tracks.Query().Where(t => t.AlbumId == section.AlbumId).OrderBy(t => t.TrackId).Skip(itemIndex).Limit(1).First();

            return t;
		}

        public int ItemsForSection(int sectionIndex)
            => (Section(sectionIndex) as AlbumInfo).TrackCount;

        public object Section(int sectionIndex)
        {
            var section = albums[sectionIndex] as AlbumInfo;
            if (section.TrackCount <= 0)
                section.TrackCount = tracks.Count(t => t.AlbumId == section.AlbumId);

            return section;
        }
    }
}
