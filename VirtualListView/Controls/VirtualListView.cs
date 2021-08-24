﻿using Microsoft.Maui.Controls.Xaml.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.Maui.Controls
{

	public partial class VirtualListView : View, IVirtualListView, IVirtualListViewSelector, IVisualTreeElement
	{
		static VirtualListView()
		{

		}

		public static readonly BindableProperty PositionInfoProperty = BindableProperty.CreateAttached(
			"PositionInfo",
			typeof(PositionInfo),
			typeof(View),
			default);

		public IVirtualListViewAdapter Adapter
		{
			get => (IVirtualListViewAdapter)GetValue(AdapterProperty);
			set => SetValue(AdapterProperty, value);
		}

		public static readonly BindableProperty AdapterProperty =
			BindableProperty.Create(nameof(Adapter), typeof(IVirtualListViewAdapter), typeof(VirtualListView), default);


		public IView GlobalHeader
		{
			get => (IView)GetValue(GlobalHeaderProperty);
			set => SetValue(GlobalHeaderProperty, value);
		}

		public static readonly BindableProperty GlobalHeaderProperty =
			BindableProperty.Create(nameof(GlobalHeader), typeof(IView), typeof(VirtualListView), default);

		public IView GlobalFooter
		{
			get => (IView)GetValue(GlobalFooterProperty);
			set => SetValue(GlobalFooterProperty, value);
		}

		public static readonly BindableProperty GlobalFooterProperty =
			BindableProperty.Create(nameof(GlobalFooter), typeof(IView), typeof(VirtualListView), default);



		public DataTemplate ItemTemplate
		{
			get => (DataTemplate)GetValue(ItemTemplateProperty);
			set => SetValue(ItemTemplateProperty, value);
		}

		public static readonly BindableProperty ItemTemplateProperty =
			BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(VirtualListView), default);

		public VirtualListViewItemTemplateSelector ItemTemplateSelector
		{
			get => (VirtualListViewItemTemplateSelector)GetValue(ItemTemplateSelectorProperty);
			set => SetValue(ItemTemplateSelectorProperty, value);
		}

		public static readonly BindableProperty ItemTemplateSelectorProperty =
			BindableProperty.Create(nameof(ItemTemplateSelector), typeof(VirtualListViewItemTemplateSelector), typeof(VirtualListView), default);



		public DataTemplate SectionHeaderTemplate
		{
			get => (DataTemplate)GetValue(SectionHeaderTemplateProperty);
			set => SetValue(SectionHeaderTemplateProperty, value);
		}

		public static readonly BindableProperty SectionHeaderTemplateProperty =
			BindableProperty.Create(nameof(SectionHeaderTemplate), typeof(DataTemplate), typeof(VirtualListView), default);

		public VirtualListViewSectionTemplateSelector SectionHeaderTemplateSelector
		{
			get => (VirtualListViewSectionTemplateSelector)GetValue(SectionHeaderTemplateSelectorProperty);
			set => SetValue(SectionHeaderTemplateSelectorProperty, value);
		}

		public static readonly BindableProperty SectionHeaderTemplateSelectorProperty =
			BindableProperty.Create(nameof(SectionHeaderTemplateSelector), typeof(VirtualListViewSectionTemplateSelector), typeof(VirtualListView), default);



		public DataTemplate SectionFooterTemplate
		{
			get => (DataTemplate)GetValue(SectionFooterTemplateProperty);
			set => SetValue(SectionFooterTemplateProperty, value);
		}

		public static readonly BindableProperty SectionFooterTemplateProperty =
			BindableProperty.Create(nameof(SectionFooterTemplate), typeof(DataTemplate), typeof(VirtualListView), default);

		public VirtualListViewSectionTemplateSelector SectionFooterTemplateSelector
		{
			get => (VirtualListViewSectionTemplateSelector)GetValue(SectionFooterTemplateSelectorProperty);
			set => SetValue(SectionFooterTemplateSelectorProperty, value);
		}

		public static readonly BindableProperty SectionFooterTemplateSelectorProperty =
			BindableProperty.Create(nameof(SectionFooterTemplateSelector), typeof(VirtualListViewSectionTemplateSelector), typeof(VirtualListView), default);


		public Maui.SelectionMode SelectionMode
		{
			get => (Maui.SelectionMode)GetValue(SelectionModeProperty);
			set => SetValue(SelectionModeProperty, value);
		}

		public static readonly BindableProperty SelectionModeProperty =
			BindableProperty.Create(nameof(SelectionMode), typeof(Maui.SelectionMode), typeof(VirtualListView), Maui.SelectionMode.None);

		public event EventHandler<SelectedItemsChangedEventArgs> SelectedItemsChanged;

		readonly object selectedItemsLocker = new object();
		readonly List<ItemPosition> selectedItems = new List<ItemPosition>();

		public IReadOnlyList<ItemPosition> SelectedItems
		{
			get
			{
				if (SelectionMode == Maui.SelectionMode.None)
					return new List<ItemPosition>();

				lock (selectedItemsLocker)
					return selectedItems.AsReadOnly();
			}
		}

		public VirtualListViewLayout Layout
		{
			get => (VirtualListViewLayout)GetValue(LayoutProperty);
			set => SetValue(LayoutProperty, value);
		}

		public static readonly BindableProperty LayoutProperty =
			BindableProperty.Create(nameof(VirtualListViewLayout), typeof(VirtualListViewLayout), typeof(VirtualListView), new VirtualListViewStackLayout());



		public IVirtualListViewSelector ViewSelector => this;

		public IView Header => GlobalHeader;
		public IView Footer => GlobalFooter;

		public bool IsItemSelected(int sectionIndex, int itemIndex)
		{
			if (SelectionMode == Maui.SelectionMode.None)
				return false;

			lock (selectedItemsLocker)
				return selectedItems.Contains(new ItemPosition(sectionIndex, itemIndex));
		}



		public void SetSelected(params ItemPosition[] paths)
		{
			if (SelectionMode == Maui.SelectionMode.None)
				return;

			var prev = new List<ItemPosition>(SelectedItems);

			IReadOnlyList<ItemPosition> current;

			lock (selectedItemsLocker)
			{
				foreach (var path in paths)
				{
					if (!selectedItems.Contains(path))
						selectedItems.Add(path);
				}

				current = SelectedItems ?? new List<ItemPosition>();
			}

			(Handler as VirtualListViewHandler)?.Invoke(nameof(SetSelected), paths);

			// Raise event
			SelectedItemsChanged?.Invoke(this, new SelectedItemsChangedEventArgs(prev, current));
		}

		public void SetDeselected(params ItemPosition[] paths)
		{
			if (SelectionMode == Maui.SelectionMode.None)
				return;

			var prev = new List<ItemPosition>(SelectedItems);

			IReadOnlyList<ItemPosition> current;

			lock (selectedItemsLocker)
			{
				foreach (var path in paths)
				{
					if (selectedItems.Contains(path))
						selectedItems.Remove(path);
				}

				current = SelectedItems ?? new List<ItemPosition>();
			}

			(Handler as VirtualListViewHandler)?.Invoke(nameof(SetDeselected), paths);

			// Raise event
			SelectedItemsChanged?.Invoke(this, new SelectedItemsChangedEventArgs(prev, current));
		}

		public event EventHandler<ScrolledEventArgs> Scrolled;
		public event EventHandler DataInvalidated;

		internal void RaiseScrolled(ScrolledEventArgs args)
			=> Scrolled?.Invoke(this, args);

		public void InvalidateData()
		{
			(Handler as VirtualListViewHandler)?.InvalidateData();

			DataInvalidated?.Invoke(this, new EventArgs());
		}

		public bool SectionHasHeader(int sectionIndex)
			=> SectionHeaderTemplateSelector != null || SectionHeaderTemplate != null;

		public bool SectionHasFooter(int sectionIndex)
			=> SectionFooterTemplateSelector != null || SectionFooterTemplate != null;

		public IView CreateView(PositionInfo position, object data)
			=> position.Kind switch {
				PositionKind.Item => 
					ItemTemplateSelector?.SelectTemplate(data, position.SectionIndex, position.ItemIndex)?.CreateContent() as View
						?? ItemTemplate?.CreateContent() as View,
				PositionKind.SectionHeader =>
					SectionHeaderTemplateSelector?.SelectTemplate(data, position.SectionIndex)?.CreateContent() as View
						?? SectionHeaderTemplate?.CreateContent() as View,
				PositionKind.SectionFooter =>
					SectionFooterTemplateSelector?.SelectTemplate(data, position.SectionIndex)?.CreateContent() as View
						?? SectionFooterTemplate?.CreateContent() as View,
				PositionKind.Header =>
					GlobalHeader,
				PositionKind.Footer =>
					GlobalFooter,
				_ => default
			};

		public void RecycleView(PositionInfo position, object data, IView view)
		{
			if (view is View controlsView)
			{
				if (controlsView.Resources.TryGetValue("VirtualViewCellPositionInfo", out var v) && v is BindablePositionInfo bindablePositionInfo)
					bindablePositionInfo.SetPositionInfo(position);
				else
					controlsView.Resources["VirtualViewCellPositionInfo"] = new BindablePositionInfo(position);

				controlsView.BindingContext = data;
			}
		}

		public string GetReuseId(PositionInfo position, object data)
			=> position.Kind switch
			{
				PositionKind.Item =>
					"ITEM_" + (ItemTemplateSelector?.SelectTemplate(data, position.SectionIndex, position.ItemIndex)
						?? ItemTemplate).GetHashCode().ToString(),
				PositionKind.SectionHeader =>
					"SECTION_HEADER_" + (SectionHeaderTemplateSelector?.SelectTemplate(data, position.SectionIndex)
						?? SectionHeaderTemplate).GetHashCode().ToString(),
				PositionKind.SectionFooter =>
					"SECTION_FOOTER_" + (SectionFooterTemplateSelector?.SelectTemplate(data, position.SectionIndex)
						?? SectionFooterTemplate).GetHashCode().ToString(),
				PositionKind.Header =>
					"GLOBLA_HEADER_" + (Header?.GetContentTypeHashCode().ToString() ?? "NIL"),
				PositionKind.Footer =>
					"GLOBAL_FOOTER_" + (Footer?.GetContentTypeHashCode().ToString() ?? "NIL"),
				_ => "UNKNOWN"
			};

		public IReadOnlyList<IVisualTreeElement> GetVisualChildren()
		{
			var results = new List<IVisualTreeElement>();

			foreach (var c in logicalChildren)
			{
				if (c is IVisualTreeElement vte)
					results.Add(vte);
			}

			return results;
		}

		static MethodInfo onChildAddedMethod;
		static MethodInfo onChildRemovedMethod;

		readonly object lockLogicalChildren = new();
		readonly List<IView> logicalChildren = new();

		public void ViewDetached(PositionInfo position, IView view)
		{
			var oldLogicalIndex = -1;
			lock (lockLogicalChildren)
			{
				oldLogicalIndex = logicalChildren.IndexOf(view);
				logicalChildren.Remove(view);
			}

			if (onChildRemovedMethod == null)
				onChildRemovedMethod = typeof(VisualDiagnostics).GetMethod("OnChildRemoved", BindingFlags.Static | BindingFlags.NonPublic);

			onChildRemovedMethod.Invoke(null, new object[] { this, view as Element, oldLogicalIndex });
		}

		public void ViewAttached(PositionInfo position, IView view)
		{
			lock (lockLogicalChildren)
				logicalChildren.Add(view);

			if (onChildAddedMethod == null)
				onChildAddedMethod = typeof(VisualDiagnostics).GetMethod("OnChildAdded", BindingFlags.Static | BindingFlags.NonPublic);

			onChildAddedMethod.Invoke(null, new object[] { this, view as Element });
		}
	}
}
