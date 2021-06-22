
namespace Microsoft.Maui
{
	public interface IVirtualListView : IView
	{
		IVirtualListView Adapter { get; }

		IReplaceableView Header { get; }

		IReplaceableView Footer { get; }


		IReplaceableView SectionHeaderTemplate { get; }

		IReplaceableView SectionFooterTemplate { get; }

		IReplaceableView ItemTemplate { get; }

	}
}
