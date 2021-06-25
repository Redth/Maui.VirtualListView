using System;

namespace Microsoft.Maui
{
	public class VirtualListViewCell : Maui.Controls.Layout2.GridLayout, IVirtualListViewCell
	{
		public int SectionIndex { get; private set; }
		public int ItemIndex { get; private set; }
		public bool IsGlobalHeader { get; private set; }
		public bool IsGlobalFooter { get; private set; }
		public bool IsSectionHeader { get; private set; }
		public bool IsSectionFooter { get; private set; }
		public bool IsItem { get; private set; }
		public bool IsLastItemInSection { get; private set; }
		public bool IsNotLastItemInSection { get; private set; }
		public bool IsFirstItemInSection { get; private set; }
		public bool IsNotFirstItemInSection { get; private set; }
		public PositionKind PositionKind { get; private set; }
		public bool IsSelected { get; private set; }

		internal void Update(PositionInfo info)
		{
			PositionKind = info.Kind;

			if (info.Kind == PositionKind.Item)
			{
				IsLastItemInSection = info.ItemIndex >= info.ItemsInSection - 1;
				IsNotLastItemInSection = !IsLastItemInSection;
				IsFirstItemInSection = info.ItemIndex == 0;
				IsNotFirstItemInSection = !IsFirstItemInSection;
				ItemIndex = info.ItemIndex;
				SectionIndex = info.SectionIndex;
				IsSelected = info.IsSelected;
			}
			else
			{
				IsLastItemInSection = false;
				IsNotLastItemInSection = false;
				IsFirstItemInSection = false;
				IsNotFirstItemInSection = false;
				ItemIndex = -1;
				SectionIndex = -1;
				IsSelected = false;
			}

			IsItem = info.Kind == PositionKind.Item;
			IsGlobalHeader = info.Kind == PositionKind.Header;
			IsGlobalFooter = info.Kind == PositionKind.Footer;
			IsSectionHeader = info.Kind == PositionKind.SectionHeader;
			IsSectionFooter = info.Kind == PositionKind.SectionFooter;
		}
		internal static void ThrowInvalidDataTemplateException()
			=> throw new NotSupportedException($"Item DataTemplate must contain a {nameof(VirtualListViewCell)}.");
	}
}
