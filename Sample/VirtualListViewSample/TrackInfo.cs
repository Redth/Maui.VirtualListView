using LiteDB;

namespace VirtualListViewSample
{
    public class TrackInfo
    {
        [BsonId]
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

}
