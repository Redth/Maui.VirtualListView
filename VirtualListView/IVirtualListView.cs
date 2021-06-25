
namespace Microsoft.Maui
{
	public interface IVirtualListView : IView
	{
		IVirtualListView Adapter { get; }

		IView Header { get; }

		IView Footer { get; }

		IView SectionHeaderTemplate { get; }

		IView SectionFooterTemplate { get; }

		IView ItemTemplate { get; }
	}
}
