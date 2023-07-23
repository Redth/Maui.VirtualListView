namespace VirtualListViewSample;

public class MauiProgram
{
	public static MauiApp Create()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseVirtualListView();

		_ = Task.Run(() => MusicDatabase.Instance.Value);

		return builder.Build();
	}
}