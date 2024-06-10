using UIKit;

namespace Microsoft.Maui;

internal static class LayoutFactory
{
	public static UICollectionViewLayout CreateList(IVirtualListView virtualListView)
		=> virtualListView.Orientation == ListOrientation.Vertical
			? CreateVerticalList(virtualListView)
			: CreateHorizontalList(virtualListView);
	
	public static UICollectionViewLayout CreateGrid(IVirtualListView virtualListView)
		=> virtualListView.Orientation == ListOrientation.Vertical
			? CreateVerticalGrid(virtualListView)
			: CreateHorizontalGrid(virtualListView);

	static NSCollectionLayoutBoundarySupplementaryItem[] CreateSupplementaryItems(IVirtualListView virtualListView,
		UICollectionViewScrollDirection scrollDirection, NSCollectionLayoutDimension width, NSCollectionLayoutDimension height)
	{
		if (virtualListView.Adapter.GetNumberOfSections() > 0)
		{
			var items = new List<NSCollectionLayoutBoundarySupplementaryItem>();
			
			// if (groupingInfo.HasHeader)
			// {
				items.Add(NSCollectionLayoutBoundarySupplementaryItem.Create(
					NSCollectionLayoutSize.Create(width, height),
					UICollectionElementKindSectionKey.Header.ToString(),
					scrollDirection == UICollectionViewScrollDirection.Vertical
						? NSRectAlignment.Top
						: NSRectAlignment.Leading));
			//}

			//if (virtualListView.Footer is not null)
			//{
				items.Add(NSCollectionLayoutBoundarySupplementaryItem.Create(
					NSCollectionLayoutSize.Create(width, height),
					UICollectionElementKindSectionKey.Footer.ToString(),
					scrollDirection == UICollectionViewScrollDirection.Vertical
						? NSRectAlignment.Bottom
						: NSRectAlignment.Trailing));
			//}

			return items.ToArray();
		}

		return [];
	}

	static UICollectionViewLayout CreateLayout(UICollectionViewScrollDirection scrollDirection, IVirtualListView virtualListView, NSCollectionLayoutDimension itemWidth, NSCollectionLayoutDimension itemHeight, NSCollectionLayoutDimension groupWidth, NSCollectionLayoutDimension groupHeight, int columns = 1)
	{
		var layoutConfiguration = new UICollectionViewCompositionalLayoutConfiguration();
		layoutConfiguration.ScrollDirection = scrollDirection;
		
		// Setup the header/footer views
		// The header/footer will use the same size as the group itself so it spans the size of it
		layoutConfiguration.BoundarySupplementaryItems = CreateSupplementaryItems(
			virtualListView,
			scrollDirection,
			groupWidth,
			groupHeight);

		var layout = new UICollectionViewCompositionalLayout((sectionIndex, environment) =>
		{
			// Each item has a size
			var itemSize = NSCollectionLayoutSize.Create(itemWidth, itemHeight);
			// Create the item itself from the size
			var item = NSCollectionLayoutItem.Create(layoutSize: itemSize);
			
			// Each group of items (for grouped collections) has a size
			var groupSize = NSCollectionLayoutSize.Create(groupWidth, groupHeight);

			// Create the group
			// If vertical list, we want the group to layout horizontally (eg: grid columns go left to right)
			// for horizontal list, we want to lay grid rows out vertically
			// For simple lists it doesn't matter so much since the items span the entire width or height
			var group = scrollDirection == UICollectionViewScrollDirection.Vertical
				? NSCollectionLayoutGroup.CreateHorizontal(groupSize, item, columns)
				: NSCollectionLayoutGroup.CreateVertical(groupSize, item, columns);
			
			// Create our section layout
			return NSCollectionLayoutSection.Create(group: group);
		}, layoutConfiguration);

		return layout;
	}

	public static UICollectionViewLayout CreateVerticalList(IVirtualListView virtualListView)
		=> CreateLayout(UICollectionViewScrollDirection.Vertical,
			virtualListView,
			// Fill the width
			NSCollectionLayoutDimension.CreateFractionalWidth(1f),
			// Dynamic (estimate required)
			NSCollectionLayoutDimension.CreateEstimated(30f),
			NSCollectionLayoutDimension.CreateFractionalWidth(1f),
			NSCollectionLayoutDimension.CreateEstimated(30f));


	public static UICollectionViewLayout CreateHorizontalList(IVirtualListView virtualListView)
		=> CreateLayout(UICollectionViewScrollDirection.Horizontal,
			virtualListView,
			// Dynamic, estimated width
			NSCollectionLayoutDimension.CreateEstimated(30f),
			// Fill the height for horizontal
			NSCollectionLayoutDimension.CreateFractionalHeight(1f),
			NSCollectionLayoutDimension.CreateEstimated(30f),
			NSCollectionLayoutDimension.CreateFractionalHeight(1f));
	
	public static UICollectionViewLayout CreateVerticalGrid(IVirtualListView virtualListView)
		=> CreateLayout(UICollectionViewScrollDirection.Vertical,
			virtualListView,
			// Width is the number of columns
			NSCollectionLayoutDimension.CreateFractionalWidth(1f / virtualListView.Columns),
			// Height is dynamic, estimated
			NSCollectionLayoutDimension.CreateEstimated(30f),
			// Group spans all columns, full width for vertical
			NSCollectionLayoutDimension.CreateFractionalWidth(1f),
			// Group is dynamic height for vertical
			NSCollectionLayoutDimension.CreateEstimated(30f),
			virtualListView.Columns);
	
	
	public static UICollectionViewLayout CreateHorizontalGrid(IVirtualListView virtualListView)
		=> CreateLayout(UICollectionViewScrollDirection.Horizontal,
			virtualListView,
			// Item width is estimated
			NSCollectionLayoutDimension.CreateEstimated(30f),
			// Item height is number of rows
			NSCollectionLayoutDimension.CreateFractionalHeight(1f / virtualListView.Columns),
			// Group width is dynamic for horizontal
			NSCollectionLayoutDimension.CreateEstimated(30f),
			// Group spans all rows, full height for horizontal
			NSCollectionLayoutDimension.CreateFractionalHeight(1f),
			virtualListView.Columns);
	
}