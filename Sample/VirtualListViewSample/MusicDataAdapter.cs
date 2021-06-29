using LiteDB;
using Microsoft.Maui;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace VirtualListViewSample
{
    public class MusicDataAdapter : IVirtualListViewAdapter
    {
        public MusicDataAdapter()
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

            albums = database.GetCollection<AlbumInfo>("albums").FindAll().ToList();
            tracks = database.GetCollection<TrackInfo>("tracks");
        }

        readonly LiteDatabase database;
        readonly List<AlbumInfo> albums;
        readonly ILiteCollection<TrackInfo> tracks;
        
        public int Sections
            => albums.Count();

        public object Item(int sectionIndex, int itemIndex)
            => tracks.FindOne(t => t.AlbumId == (Section(sectionIndex) as AlbumInfo).AlbumId);

        public int ItemsForSection(int sectionIndex)
        {
            var album = Section(sectionIndex) as AlbumInfo;

            return tracks.Count(t => t.AlbumId == album.AlbumId);
        }

        public object Section(int sectionIndex)
            => albums[sectionIndex];
    }
}
