using Microsoft.Maui;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace VirtualListViewSample
{
    public class JsonGroupedAdapter : IVirtualListViewAdapter
    {
        public JsonGroupedAdapter()
        {
            var files = new[] { "albums.json", "tracks.json" };

            foreach (var file in files)
            {
                var path = Path.Combine(Microsoft.Maui.Essentials.FileSystem.CacheDirectory, file);

                if (!File.Exists(path))
                {
                    var assembly = IntrospectionExtensions.GetTypeInfo(typeof(App)).Assembly;
                    using (var stream = assembly.GetManifestResourceStream(nameof(VirtualListViewSample) + file))
                    using (var outStream = File.Create(path))
                        stream.CopyTo(outStream);
                }

                var json = JObject.Parse(File.ReadAllText(path));

                foreach (var row in json["rows"])
                {
                    var ci = 0;
                    var cols = row.Values().ToArray();

                    if (path.EndsWith("tracks.json"))
                    {
                        Tracks.Add(new TrackInfo
                        {
                            TrackId = cols[ci++].Value<int>(),
                            TrackName = cols[ci++].Value<string>(),
                            AlbumId = cols[ci++].Value<int>(),
                            AlbumTitle = cols[ci++].Value<string>(),
                            ArtistId = cols[ci++].Value<int>(),
                            ArtistName = cols[ci++].Value<string>(),
                            GenreId = cols[ci++].Value<int>(),
                            GenreName = cols[ci++].Value<string>()
                        });
                    }
                    else
                    {
                        Albums.Add(new AlbumInfo
                        {
                            AlbumId = cols[ci++].Value<int>(),
                            AlbumTitle = cols[ci++].Value<string>(),
                            ArtistId = cols[ci++].Value<int>(),
                            ArtistName = cols[ci++].Value<string>()
                        });
                    }
                }

            }
        }

        public readonly List<AlbumInfo> Albums = new List<AlbumInfo>();
        public readonly List<TrackInfo> Tracks = new List<TrackInfo>();

        public int Sections
            => Albums.Count;

        public object Item(int sectionIndex, int itemIndex)
            => Tracks.Where(t => t.AlbumId == (Section(sectionIndex) as AlbumInfo).AlbumId)
            .Skip(itemIndex).FirstOrDefault();

        public int ItemsForSection(int sectionIndex)
        {
            var album = Section(sectionIndex) as AlbumInfo;

            return Tracks.Count(t => t.AlbumId == album.AlbumId);
        }
        
        public object Section(int sectionIndex)
            => Albums[sectionIndex];
    }
}
