using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Xamarin.Forms.Platform.Android.AppCompat;

[assembly: Xamarin.Forms.ExportRenderer(typeof(Xamarin.CommunityToolkit.UI.Views.VirtualListView), typeof(Xamarin.CommunityToolkit.UI.Views.VirtualListViewRenderer))]

namespace Xamarin.CommunityToolkit.UI.Views
{
	public class VirtualListViewRenderer : ViewRenderer<VirtualListView, RecyclerView>
	{
		public VirtualListViewRenderer(Context context)
			: base(context)
		{
		}

		RvAdapter adapter;
		RecyclerView recyclerView;
		LinearLayoutManager layoutManager;

		public override Xamarin.Forms.SizeRequest GetDesiredSize(int widthConstraint, int heightConstraint)
		{
			return new Xamarin.Forms.SizeRequest(new Xamarin.Forms.Size(int.MaxValue - 1000, int.MaxValue - 1000),
				new Xamarin.Forms.Size(0, 0));
		}

		protected override void OnElementChanged(Xamarin.Forms.Platform.Android.ElementChangedEventArgs<VirtualListView> e)
		{
			base.OnElementChanged(e);

			// Clean up old
			if (e.OldElement != null)
			{
				// Unsubscribe from event handlers and cleanup any resources
				if (recyclerView?.Parent is ViewGroup vg)
					vg.RemoveView(recyclerView);

				recyclerView.SetAdapter(null);
				recyclerView.Dispose();
				recyclerView = null;
				adapter.Dispose();
				adapter = null;
				layoutManager.Dispose();
				layoutManager = null;
			}

			// Setup new
			if (e.NewElement != null)
			{
				// Create the native control
				if (Control == null)
				{
					recyclerView = new RecyclerView(Context);
					layoutManager = new LinearLayoutManager(Context);
					//layoutManager.Orientation = LinearLayoutManager.Horizontal;

					var templateSelector = CreateTemplateSelector();

					adapter = new RvAdapter(Context, e.NewElement.Adapter);
					adapter.TemplateSelector = templateSelector;
					adapter.Element = e.NewElement;

					recyclerView.SetLayoutManager(layoutManager);
					recyclerView.SetAdapter(adapter);
					recyclerView.LayoutParameters = new LayoutParams(
						LayoutParams.MatchParent, LayoutParams.MatchParent);

					SetNativeControl(recyclerView);
				}
			}
		}

		PositionTemplateSelector CreateTemplateSelector()
			=> new PositionTemplateSelector
			{
				HeaderTemplate = Element.HeaderTemplate,
				FooterTemplate = Element.FooterTemplate,
				ItemTemplate = Element.ItemTemplate,
				ItemTemplateSelector = Element.ItemTemplateSelector,
				SectionFooterTemplate = Element.SectionFooterTemplate,
				SectionFooterTemplateSelector = Element.SectionFooterTemplateSelector,
				SectionHeaderTemplate = Element.SectionHeaderTemplate,
				SectionHeaderTemplateSelector = Element.SectionHeaderTemplateSelector
			};

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == VirtualListView.AdapterProperty.PropertyName)
			{
				adapter?.NotifyDataSetChanged();
			}
			else if (e.PropertyName == VirtualListView.HeaderTemplateProperty.PropertyName
				|| e.PropertyName == VirtualListView.HeaderTemplateProperty.PropertyName
				|| e.PropertyName == VirtualListView.FooterTemplateProperty.PropertyName
				|| e.PropertyName == VirtualListView.ItemTemplateProperty.PropertyName
				|| e.PropertyName == VirtualListView.ItemTemplateSelectorProperty.PropertyName
				|| e.PropertyName == VirtualListView.SectionFooterTemplateProperty.PropertyName
				|| e.PropertyName == VirtualListView.SectionFooterTemplateSelectorProperty.PropertyName
				|| e.PropertyName == VirtualListView.SectionHeaderTemplateProperty.PropertyName
				|| e.PropertyName == VirtualListView.SectionHeaderTemplateSelectorProperty.PropertyName)
			{
				var templateSelector = CreateTemplateSelector();
				adapter.TemplateSelector = templateSelector;
				adapter?.NotifyDataSetChanged();
			}
		}
	}

	internal class RvItemHolder : RecyclerView.ViewHolder
	{
		public VirtualViewCell ViewCell { get; }
		public PositionInfo PositionInfo { get; set; }

		public RvItemHolder(VirtualViewCell viewCell, View itemView)
			: base(itemView)
		{
			ViewCell = viewCell;
		}
	}

	internal class RvAdapter : RecyclerView.Adapter
	{
		internal PositionTemplateSelector TemplateSelector { get; set; }
		
		readonly List<Xamarin.Forms.DataTemplate> templates;

		readonly object lockObj = new object();

		readonly IVirtualListViewAdapter adapter;

		RvViewHolderClickListener clickListener;

		public Context Context { get; }

		public object BindingContext { get; set; }

		public VirtualListView Element { get; set; }

		public override int ItemCount
		{
			get
			{
				var sum = 0;

				if (TemplateSelector?.HeaderTemplate != null)
					sum += 1;

				if (adapter != null)
				{
					for (int i = 0; i < adapter.Sections; i++)
					{
						if (TemplateSelector?.SectionHeaderTemplate != null || TemplateSelector?.SectionHeaderTemplateSelector != null)
							sum += 1;

						sum += adapter.ItemsForSection(i);

						if (TemplateSelector?.SectionFooterTemplate != null || TemplateSelector?.SectionFooterTemplateSelector != null)
							sum += 1;
					}
				}

				if (TemplateSelector?.FooterTemplate != null)
					sum += 1;

				return sum;
			}
		}

		internal RvAdapter(Context context, IVirtualListViewAdapter adapter)
		{
			Context = context;
			this.adapter = adapter;

			templates = new List<Xamarin.Forms.DataTemplate>();
		}

		public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
		{
			var info = TemplateSelector.GetInfo(adapter, position);

			if (info == null)
				return;

			// The template selector doesn't infer selected properly
			// so we need to ask the listview which tracks selections about the state
			info.IsSelected = info.Kind == PositionKind.Item
				&& (Element?.IsItemSelected(info.SectionIndex, info.ItemIndex) ?? false);

			var item = info.BindingContext ?? BindingContext;

			if (item != null && holder is RvItemHolder itemHolder && itemHolder.ViewCell != null)
			{
				itemHolder.PositionInfo = info;
				itemHolder.ViewCell.BindingContext = item;
				itemHolder.ViewCell.Update(info);
				itemHolder.ViewCell.View.InvalidateMeasureNonVirtual(Xamarin.Forms.Internals.InvalidationTrigger.MeasureChanged);
			}
		}

		public override int GetItemViewType(int position)
		{
			int viewType = base.GetItemViewType(position);

			var template = TemplateSelector.GetTemplate(adapter, position);

			lock (lockObj)
			{
				viewType = templates.IndexOf(template);

				if (viewType < 0)
				{
					templates.Add(template);
					viewType = templates.Count - 1;
				}
			}

			return viewType;
		}

		public override long GetItemId(int position)
			=> position;

		View CreateViewContainer(Context context, Xamarin.Forms.View formsView)
			=> new Xamarin.Forms.Platform.Android.ContainerView(context, formsView)
			{
				MatchWidth = true,
				LayoutParameters = new ViewGroup.LayoutParams(
				ViewGroup.LayoutParams.MatchParent,
				ViewGroup.LayoutParams.WrapContent)
			};

		public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
		{
			var template = templates.ElementAtOrDefault(viewType);

			var templateContent = template.CreateContent();

			if (templateContent is VirtualViewCell viewCell)
			{
				var viewHolder = new RvItemHolder(viewCell, CreateViewContainer(parent.Context, viewCell.View));

				clickListener = new RvViewHolderClickListener(viewHolder, rvh =>
				{
					if (rvh.PositionInfo == null || rvh.PositionInfo.Kind != PositionKind.Item)
						return;

					var p = new ItemPosition(rvh.PositionInfo.SectionIndex, rvh.PositionInfo.ItemIndex);

					var selected = !rvh.ViewCell.IsSelected;

					rvh.ViewCell.IsSelected = selected;

					if (selected)
						Element.SetSelected(p);
					else
						Element.SetDeselected(p);
				});

				viewHolder.ItemView.SetOnClickListener(clickListener);

				return viewHolder;
			}

			VirtualViewCell.ThrowInvalidDataTemplateException();
			return null;
		}

		class RvViewHolderClickListener : Java.Lang.Object, View.IOnClickListener
		{
			public RvViewHolderClickListener(RvItemHolder viewHolder, Action<RvItemHolder> clickHandler)
			{
				ViewHolder = viewHolder;
				ClickHandler = clickHandler;
			}

			public RvItemHolder ViewHolder { get; }

			public Action<RvItemHolder> ClickHandler { get; }

			public void OnClick(View v)
			{
				if (ViewHolder?.PositionInfo != null && ViewHolder.PositionInfo.Kind == PositionKind.Item)
					ClickHandler?.Invoke(ViewHolder);
			}
		}
	}
}