﻿using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Microsoft.Maui
{
	public partial class VirtualListViewHandler : ViewHandler<IVirtualListView, ItemsRepeaterScrollHost>
	{
		ItemsRepeaterScrollHost itemsRepeaterScrollHost;
		ScrollViewer scrollViewer;
		ItemsRepeater itemsRepeater;
		IrSource irSource;
		IrDataTemplateSelector templateSelector;

		internal PositionalViewSelector PositionalViewSelector { get; private set; }

		protected override ItemsRepeaterScrollHost CreateNativeView()
		{
			itemsRepeaterScrollHost = new ItemsRepeaterScrollHost();
			scrollViewer = new ScrollViewer();
			itemsRepeater = new ItemsRepeater();

			scrollViewer.Content = itemsRepeater;
			itemsRepeaterScrollHost.ScrollViewer = scrollViewer;

			return itemsRepeaterScrollHost;
		}

		protected override void ConnectHandler(ItemsRepeaterScrollHost nativeView)
		{
			base.ConnectHandler(nativeView);

			templateSelector = new IrDataTemplateSelector(VirtualView);
			itemsRepeater.ItemTemplate = templateSelector;

			PositionalViewSelector = new PositionalViewSelector(VirtualView);
			irSource = new IrSource(MauiContext, PositionalViewSelector, VirtualView);

			itemsRepeater.ItemsSource = irSource;
		}

		protected override void DisconnectHandler(ItemsRepeaterScrollHost nativeView)
		{
			itemsRepeater.ItemTemplate = null;
			itemsRepeater.ItemsSource = null;

			PositionalViewSelector.Reset();
			PositionalViewSelector = null;
			templateSelector = null;
			irSource = null;

			base.DisconnectHandler(nativeView);
		}

		public void InvalidateData()
		{
			PositionalViewSelector.Reset();
			irSource?.Reset();
		}

		public static void MapAdapter(VirtualListViewHandler handler, IVirtualListView virtualListView)
			=> handler?.InvalidateData();

		public static void MapHeader(VirtualListViewHandler handler, IVirtualListView virtualListView)
			=> handler?.InvalidateData();

		public static void MapFooter(VirtualListViewHandler handler, IVirtualListView virtualListView)
			=> handler?.InvalidateData();

		public static void MapViewSelector(VirtualListViewHandler handler, IVirtualListView virtualListView)
			=> handler?.InvalidateData();

		public static void MapSelectionMode(VirtualListViewHandler handler, IVirtualListView virtualListView)
		{ }

		public static void MapInvalidateData(VirtualListViewHandler handler, IVirtualListView virtualListView)
			=> handler?.InvalidateData();

		public static void MapSetSelected(VirtualListViewHandler handler, IVirtualListView virtualListView, object? parameter)
		{
			if (parameter is ItemPosition[] items)
			{
				//
			}
		}

		public static void MapSetDeselected(VirtualListViewHandler handler, IVirtualListView virtualListView, object? parameter)
		{
			if (parameter is ItemPosition[] items)
			{
				//
			}
		}

		public static void MapLayout(VirtualListViewHandler handler, IVirtualListView virtualListView)
		{
			handler.itemsRepeater.Layout = (virtualListView.Layout ?? new VirtualListViewStackLayout()).CreateNativeLayout();
			handler.InvalidateData();
		}

		internal static void AddLibraryResources(string key, string uri)
		{
			var resources = UI.Xaml.Application.Current?.Resources;
			if (resources == null)
				return;

			var dictionaries = resources.MergedDictionaries;
			if (dictionaries == null)
				return;

			if (!resources.ContainsKey(key))
			{
				dictionaries.Add(new UI.Xaml.ResourceDictionary
				{
					Source = new Uri(uri)
				});
			}
		}
	}
}
