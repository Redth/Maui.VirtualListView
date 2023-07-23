using LiteDB;

namespace VirtualListViewSample;

public class TrackInfo
{
	public int ItemIndex { get; set; }
	public int SectionIndex { get; set; }

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

	public override string ToString()
		=> $"{TrackName} - {ArtistName} - {AlbumTitle}";

	public string SemanticDescription
		=> $"Track: {TrackId} {TrackName}";

}
