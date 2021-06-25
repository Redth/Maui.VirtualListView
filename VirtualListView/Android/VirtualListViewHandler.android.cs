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
using Microsoft.Maui.Handlers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Microsoft.Maui
{
	public partial class VirtualListViewHandler : ViewHandler<IVirtualListView, RecyclerView>
	{
		RvAdapter adapter;
		RecyclerView recyclerView;
		LinearLayoutManager layoutManager;

		protected override RecyclerView CreateNativeView()
			=> recyclerView ??= new RecyclerView(Context);

		protected override void ConnectHandler(RecyclerView nativeView)
		{
			layoutManager = new LinearLayoutManager(Context);
			//layoutManager.Orientation = LinearLayoutManager.Horizontal;

			adapter = new RvAdapter(Context, this.VirtualView.Adapter, this);
			
			// recyclerView.AddOnScrollListener(new RvScrollListener((rv, dx, dy) =>
			// {
			// 	var x = Context.FromPixels(rv.Context, dx);
			// 	var y = Context.FromPixels(rv.Context, dy);
			// 	Element?.RaiseScrolled(new Forms.ScrolledEventArgs(x, y));
			// }));

			recyclerView.SetLayoutManager(layoutManager);
			recyclerView.SetAdapter(adapter);
			recyclerView.LayoutParameters = new ViewGroup.LayoutParams(
				ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
		}

		protected override void DisconnectHandler(RecyclerView nativeView)
		{
			recyclerView.ClearOnScrollListeners();
			recyclerView.SetAdapter(null);
			adapter.Dispose();
			adapter = null;
			layoutManager.Dispose();
			layoutManager = null;
		}

		public static void MapAdapter(VirtualListViewHandler handler, IVirtualListView virtualListView)
		{
			handler.NativeView?.SwapAdapter(handler.adapter, true);
		}

		class RvScrollListener : RecyclerView.OnScrollListener
		{
			public RvScrollListener(Action<RecyclerView, int, int> scrollHandler)
			{
				ScrollHandler = scrollHandler;
			}

			Action<RecyclerView, int, int> ScrollHandler { get; }

			public override void OnScrolled(RecyclerView recyclerView, int dx, int dy)
			{
				base.OnScrolled(recyclerView, dx, dy);

				ScrollHandler?.Invoke(recyclerView, dx, dy);
			}
		}
	}

	internal class RvItemHolder : RecyclerView.ViewHolder
	{
		public IView View { get; }
		public PositionInfo PositionInfo { get; set; }

		public RvItemHolder(IView view, View itemView)
			: base(itemView)
		{
			View = view;
		}
	}

	internal class RvAdapter : RecyclerView.Adapter
	{
		readonly VirtualListViewHandler handler;

		readonly List<IViewTemplate> templates;

		readonly object lockObj = new object();

		readonly IVirtualListViewAdapter adapter;

		RvViewHolderClickListener clickListener;

		public Context Context { get; }

		public object BindingContext { get; set; }

		internal bool HasHeader => handler?.VirtualView?.HeaderTemplate != null;
		internal bool HasFooter => handler?.VirtualView?.HeaderTemplate != null;
		internal bool HasSectionHeader => handler?.VirtualView?.SectionHeaderTemplate != null;
		internal bool HasSectionFooter => handler?.VirtualView?.SectionFooterTemplate != null;

		public override int ItemCount
			=> PositionTemplateSelector.GetTotalCount(adapter, HasHeader, HasFooter, HasSectionHeader, HasSectionFooter);

		internal RvAdapter(Context context, IVirtualListViewAdapter adapter, VirtualListViewHandler handler)
		{
			Context = context;
			this.adapter = adapter;
			this.handler = handler;

			templates = new List<IViewTemplate>();
		}

		public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
		{
			var info = PositionTemplateSelector.GetInfo(adapter, position, HasHeader, HasFooter, HasSectionHeader, HasSectionFooter);

			if (info == null)
				return;

			// The template selector doesn't infer selected properly
			// so we need to ask the listview which tracks selections about the state
			info.IsSelected = info.Kind == PositionKind.Item
				&& (handler?.VirtualView?.IsItemSelected(info.SectionIndex, info.ItemIndex) ?? false);

			var item = info.BindingContext ?? BindingContext;

			if (item != null && holder is RvItemHolder itemHolder && itemHolder.View != null)
			{
				itemHolder.PositionInfo = info;
				if (itemHolder.View is IPositionInfo viewPositionInfo)
					viewPositionInfo.PositionInfo = info;
				//itemHolder.ViewCell.View.InvalidateMeasureNonVirtual(Xamarin.Forms.Internals.InvalidationTrigger.MeasureChanged);
			}
		}

		public override int GetItemViewType(int position)
		{
			int viewType = base.GetItemViewType(position);

			var template = PositionTemplateSelector.GetTemplate(
				adapter,
				position,
				handler?.VirtualView?.HeaderTemplate,
				handler?.VirtualView?.FooterTemplate,
				handler?.VirtualView?.SectionHeaderTemplate,
				handler?.VirtualView?.SectionFooterTemplate,
				handler?.VirtualView?.ItemTemplate);

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

			var templateContent = Activator.CreateInstance(template.ViewType) as IView;

			if (templateContent == null)
				throw new InvalidCastException();

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