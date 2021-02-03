using System;
using System.Collections.Generic;
using Foundation;
using UIKit;
using Xamarin.Forms;

namespace Xamarin.CommunityToolkit.UI.Views
{
	internal class ReusableIdManager
	{
		public ReusableIdManager(string uniquePrefix, NSString supplementaryKind = null)
		{
			UniquePrefix = uniquePrefix;
			SupplementaryKind = supplementaryKind;
			templates = new List<DataTemplate>();
			lockObj = new object();
		}

		public string UniquePrefix { get; }
		public NSString SupplementaryKind { get; }

		readonly List<DataTemplate> templates;
		readonly object lockObj;

		NSString GetReuseId(int i, string idModifier = null)
			=> new NSString($"_{UniquePrefix}_{nameof(VirtualListView)}_{i}");

		public NSString GetReuseId(UICollectionView collectionView, DataTemplate template)
		{
			var viewType = 0;

			lock (lockObj)
			{
				viewType = templates.IndexOf(template);

				if (viewType < 0)
				{
					templates.Add(template);
					viewType = templates.Count - 1;

					collectionView.RegisterClassForCell(
						typeof(CvCell),
						GetReuseId(viewType));
				}
			}

			return GetReuseId(viewType);
		}

		public void ResetTemplates(UICollectionView collectionView)
		{
			lock (lockObj)
			{
				for (int i = 0; i < templates.Count; i++)
					collectionView.RegisterClassForCell(null, GetReuseId(i));

				templates.Clear();
			}
		}
	}
}
