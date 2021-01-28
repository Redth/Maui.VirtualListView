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

[assembly: Xamarin.Forms.ExportRenderer(typeof(XFSlimListView.SlimListView), typeof(XFSlimListView.SlimListViewRenderer))]

namespace XFSlimListView
{
	public class SlimListViewRenderer : ViewRenderer<SlimListView, RecyclerView>
	{
		public SlimListViewRenderer(Context context)
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
					//layoutManager.Orientation = LinearLayoutManager.Horizontal;

					var templateSelector = CreateTemplateSelector();

					adapter = new RvAdapter(Context, e.NewElement.Adapter);
					adapter.TemplateSelector = templateSelector;

					separatorItemDecoration = new SeparatorItemDecoration();
					separatorItemDecoration.TemplateSelector = templateSelector;
					recyclerView.AddItemDecoration(separatorItemDecoration);

					SetSeparator();

					recyclerView.SetLayoutManager(layoutManager);
					recyclerView.SetAdapter(adapter);
					recyclerView.LayoutParameters = new LayoutParams(
						LayoutParams.FillParent, LayoutParams.FillParent);

					SetNativeControl(recyclerView);
				}
			}
		}

		SeparatorItemDecoration separatorItemDecoration;

		void SetSeparator()
		{
			if (recyclerView == null)
				return;

			var c = Xamarin.Forms.Platform.Android.ColorExtensions.ToAndroid(Element.SeparatorColor);

			var size = Element.SeparatorSize;
			var sizeDp = TypedValue.ApplyDimension(ComplexUnitType.Dip, (int)Element.SeparatorSize, Resources.DisplayMetrics);

			separatorItemDecoration.Update(c, size, sizeDp);

			recyclerView.AddItemDecoration(separatorItemDecoration);
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
				var templateSelector = CreateTemplateSelector();
				separatorItemDecoration.TemplateSelector = templateSelector;
				adapter.TemplateSelector = templateSelector;
				adapter?.NotifyDataSetChanged();
			}
			else if (e.PropertyName == SlimListView.SeparatorColorProperty.PropertyName
				|| e.PropertyName == SlimListView.SeparatorSizeProperty.PropertyName)
			{
				SetSeparator();
			}
		}
	}

	internal class RvHolder : RecyclerView.ViewHolder
	{
		public Xamarin.Forms.View FormsView { get; }

		public RvHolder(Xamarin.Forms.View formsView, View itemView) : base(itemView)
			=> FormsView = formsView;

		public int ItemPosition { get; set; } = -1;
	}

	internal class RvAdapter : RecyclerView.Adapter, View.IOnClickListener
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

		internal RvAdapter(Context context, ISlimListViewAdapter adapter)
		{
			Context = context;
			this.adapter = adapter;

			templates = new List<Xamarin.Forms.DataTemplate>();
		}

		public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
		{
			var info = TemplateSelector.GetInfo(adapter, position);

			var item = info.BindingContext ?? BindingContext;

			if (item != null && holder is RvHolder fHolder && fHolder.FormsView != null)
			{
				fHolder.FormsView.BindingContext = item;
				fHolder.ItemPosition = position;
				fHolder.ItemView.Tag = new Java.Lang.Integer(position);

				fHolder.FormsView.InvalidateMeasureNonVirtual(Xamarin.Forms.Internals.InvalidationTrigger.MeasureChanged);
				//fHolder.ItemView.SetOnClickListener(this);
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

			return new RvHolder(xfView, container);
		}

		public void OnClick(View v)
		{
			if (v.Tag is Java.Lang.Integer position)
			{
				var info = TemplateSelector.GetInfo(adapter, position.IntValue());

				if (info.Type == PositionInfo.PositionType.Item)
					ItemClicked?.Invoke(this, position.IntValue());
			}
				
		}
	}

	internal class SeparatorItemDecoration : RecyclerView.ItemDecoration
	{
		public SeparatorItemDecoration()
		{
			Color = Color.Transparent;
			Size = 0;
			SizeDp = 0;
		}

		public void Update(Color color, double size, double sizeDp)
		{
			Color = color;


			Size = size;
			SizeDp = (int)sizeDp;

			if (divider == null)
				divider = new ColorDrawable(Color);
			else
				divider.Color = color;
		}

		public PositionTemplateSelector TemplateSelector { get; set; }

		public Color Color { get; private set; }
		public double Size { get; private set; }

		public int SizeDp { get; private set; }
		ColorDrawable divider;

		public override void GetItemOffsets(Rect outRect, View view, RecyclerView parent, RecyclerView.State state)
		{
			base.GetItemOffsets(outRect, view, parent, state);

			var pos = parent.GetChildAdapterPosition(view);

			var h = Size / 2;

			if (pos == 0)
			{
				outRect.Bottom = (int)h;
				return;

			}
			//var excluded = 1;

			//if (pos == 0)
			//	return;

			//if (TemplateSelector?.HasGlobalHeader ?? false)
			//	excluded++;
			//if (TemplateSelector?.HasSectionHeader ?? false)
			//	excluded++;


			//if (pos <= excluded)
			//	return;

			
			outRect.Top = (int)h;
			outRect.Bottom = (int)h;
		}

		public override void OnDraw(Canvas c, RecyclerView parent, RecyclerView.State state)
		{
			int dividerLeft = parent.PaddingLeft;
			int dividerRight = parent.Width - parent.PaddingRight;

			int childCount = parent.ChildCount;

			var halfSize = (int)(SizeDp / 2);

			for (int i = 0; i < childCount - 1; i++)
			{
				View child = parent.GetChildAt(i);

				var p = (RecyclerView.LayoutParams)child.LayoutParameters;

				int dividerTop = child.Bottom + p.BottomMargin + halfSize;
				int dividerBottom = dividerTop + (int)SizeDp;

				//Console.WriteLine($"Divider Bounds: {dividerTop}, {dividerBottom}, {Size}");
				divider.SetBounds(dividerLeft, dividerTop, dividerRight, dividerBottom);
				divider.Draw(c);
			}
		}
	}
}