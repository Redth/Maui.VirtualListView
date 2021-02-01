using System;
using System.Collections.Generic;
using System.Text;

namespace Xamarin.CommunityToolkit.UI.Views
{
	internal class PositionInfo
	{
		public int Position { get; set; } = -1;

		public PositionKind Kind { get; set; } = PositionKind.Item;

		public object BindingContext { get; set; }

		public int SectionIndex { get; set; } = -1;

		public int NumberOfSections { get; set; } = 0;

		public int ItemIndex { get; set; } = -1;

		public int ItemsInSection { get; set; } = 0;

		public bool IsSelected { get; set; } = false;
	}
}
