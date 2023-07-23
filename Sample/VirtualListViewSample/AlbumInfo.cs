using LiteDB;

namespace VirtualListViewSample
{
	public class AlbumInfo
	{
		[BsonId]
		public int AlbumId { get; set; }

		public string AlbumTitle { get; set; }

		public int ArtistId { get; set; }

		public string ArtistName { get; set; }

		public int TrackCount { get; set; }

		public string SemanticDescription
			=> $"Start of Album: {AlbumTitle} by {ArtistName}";

		public override string ToString()
			=> $"{AlbumTitle} - {ArtistName}";
	}

}
