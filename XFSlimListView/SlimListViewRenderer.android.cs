using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
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

[assembly: Xamarin.Forms.ExportRenderer(typeof(XFSlimListView.SlimListView), typeof(XFSlimListView.SlimListViewRenderer))]

namespace XFSlimListView
{
	public class SlimListViewRenderer : ViewRenderer<SlimListView, RecyclerView>
	{
		public SlimListViewRenderer(Context context)
			: base(context)
		{
		}

		SlimListViewRvAdapter adapter;
		RecyclerView recyclerView;
		LinearLayoutManager layoutManager;

		protected override void OnElementChanged(Xamarin.Forms.Platform.Android.ElementChangedEventArgs<SlimListView> e)
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

					adapter = new SlimListViewRvAdapter(Context, e.NewElement.Adapter);
					adapter.TemplateSelector = CreateTemplateSelector();

					recyclerView.SetLayoutManager(layoutManager);
					recyclerView.SetAdapter(adapter);

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

			if (e.PropertyName == SlimListView.AdapterProperty.PropertyName)
				adapter?.NotifyDataSetChanged();
			else if (e.PropertyName == SlimListView.HeaderTemplateProperty.PropertyName
				|| e.PropertyName == SlimListView.HeaderTemplateProperty.PropertyName
				|| e.PropertyName == SlimListView.FooterTemplateProperty.PropertyName
				|| e.PropertyName == SlimListView.ItemTemplateProperty.PropertyName
				|| e.PropertyName == SlimListView.ItemTemplateSelectorProperty.PropertyName
				|| e.PropertyName == SlimListView.SectionFooterTemplateProperty.PropertyName
				|| e.PropertyName == SlimListView.SectionFooterTemplateSelectorProperty.PropertyName
				|| e.PropertyName == SlimListView.SectionHeaderTemplateProperty.PropertyName
				|| e.PropertyName == SlimListView.SectionHeaderTemplateSelectorProperty.PropertyName)
			{
				adapter.TemplateSelector = CreateTemplateSelector();
				adapter?.NotifyDataSetChanged();
			}
		}
	}

	internal class SlimListViewRvHolder : RecyclerView.ViewHolder
	{
		public Xamarin.Forms.View FormsView { get; }

		public SlimListViewRvHolder(Xamarin.Forms.View formsView, View itemView) : base(itemView)
			=> FormsView = formsView;

		public int ItemPosition { get; set; } = -1;
	}

	internal class SlimListViewRvAdapter : RecyclerView.Adapter, View.IOnClickListener
	{
		internal PositionTemplateSelector TemplateSelector { get; set; }
		
		readonly List<Xamarin.Forms.DataTemplate> templates;

		readonly object lockObj = new object();

		readonly ISlimListViewAdapter adapter;

		public Context Context { get; }

		public object BindingContext { get; set; }

		public event EventHandler<int> ItemClicked;

		public override int ItemCount
		{
			get
			{
				var sum = 0;

				if (TemplateSelector.HeaderTemplate != null)
					sum += 1;

				for (int i = 0; i < adapter.Sections; i++)
				{
					if (TemplateSelector.SectionHeaderTemplate != null || TemplateSelector.SectionHeaderTemplateSelector != null)
						sum += 1;

					sum += adapter.ItemsForSection(i);

					if (TemplateSelector.SectionFooterTemplate != null || TemplateSelector.SectionFooterTemplateSelector != null)
						sum += 1;
				}

				if (TemplateSelector.FooterTemplate != null)
					sum += 1;

				return sum;
			}
		}

		internal SlimListViewRvAdapter(
			Context context,
			ISlimListViewAdapter adapter)
		{
			Context = context;
			this.adapter = adapter;

			templates = new List<Xamarin.Forms.DataTemplate>();
		}

		public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
		{
			var info = TemplateSelector.GetInfo(adapter, position);

			var item = info.BindingContext ?? BindingContext;

			if (item != null && holder is SlimListViewRvHolder fHolder && fHolder.FormsView != null)
			{
				fHolder.FormsView.BindingContext = item;
				fHolder.ItemPosition = position;
				fHolder.ItemView.Tag = new Java.Lang.Integer(position);
				fHolder.ItemView.SetOnClickListener(this);
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

		public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
		{
			var template = templates.ElementAtOrDefault(viewType);

			var xfView = template.CreateContent() as Xamarin.Forms.View;

			var container = new Xamarin.Forms.Platform.Android.ContainerView(parent.Context, xfView)
			{
				MatchWidth = true,
				LayoutParameters = new ViewGroup.LayoutParams(
					ViewGroup.LayoutParams.MatchParent,
					ViewGroup.LayoutParams.WrapContent)
			};

			return new SlimListViewRvHolder(xfView, container);
		}

		public void OnClick(View v)
		{
			if (v.Tag is Java.Lang.Integer position)
				ItemClicked?.Invoke(this, position.IntValue());
		}
	}
}