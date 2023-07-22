using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.Maui.Controls
{
	public class VirtualViewCell : ContentView, IPositionInfo
	{
		public VirtualViewCell() : base()
		{
			UpdateBackground();
		}

		public static readonly BindableProperty SelectedBackgroundProperty =
			BindableProperty.Create(nameof(SelectedBackground), typeof(Brush), typeof(VirtualViewCell), new SolidColorBrush(Colors.Transparent),
				propertyChanged: (bindableObj, oldValue, newValue) =>
				{
					if (bindableObj is VirtualViewCell self)
					{
						self.UpdateBackground();
					}
				});

		public Brush SelectedBackground
		{
			get => (Brush)GetValue(SelectedBackgroundProperty);
			set => SetValue(SelectedBackgroundProperty, value);
		}

		public static readonly BindableProperty UnselectedBackgroundProperty =
			BindableProperty.Create(nameof(UnselectedBackground), typeof(Brush), typeof(VirtualViewCell), new SolidColorBrush(Colors.Transparent),
				propertyChanged: (bindableObj, oldValue, newValue) =>
				{
					if (bindableObj is VirtualViewCell self)
					{
						self.UpdateBackground();
					}
				});

		public Brush UnselectedBackground
		{
			get => (Brush)GetValue(UnselectedBackgroundProperty);
			set => SetValue(UnselectedBackgroundProperty, value);
		}

		public static readonly BindableProperty IsSelectedProperty =
			BindableProperty.Create(nameof(IsSelected), typeof(bool), typeof(VirtualViewCell), false,
				propertyChanged: (bindableObj, oldValue, newValue) =>
				{
					if (bindableObj is VirtualViewCell self)
					{
						self.Resources[nameof(IsSelected)] = newValue;
						self.UpdateBackground();
					}
				});

		public bool IsSelected
		{
			get => (bool)GetValue(IsSelectedProperty);
			set => SetValue(IsSelectedProperty, value);
		}

		public static readonly BindableProperty SectionIndexProperty =
			BindableProperty.Create(nameof(SectionIndex), typeof(int), typeof(VirtualViewCell), -1,
				propertyChanged: (bindableObj, oldValue, newValue) =>
				{
					if (bindableObj is VirtualViewCell self)
					{
						self.Resources[nameof(SectionIndex)] = newValue;
					}
				});

		public int SectionIndex
		{
			get => (int)GetValue(SectionIndexProperty);
			set => SetValue(SectionIndexProperty, value);
		}

		public static readonly BindableProperty ItemIndexProperty =
			BindableProperty.Create(nameof(ItemIndex), typeof(int), typeof(VirtualViewCell), -1,
				propertyChanged: (bindableObj, oldValue, newValue) =>
				{
					if (bindableObj is VirtualViewCell self)
					{
						self.Resources[nameof(ItemIndex)] = newValue;
					}
				});

		public int ItemIndex
		{
			get => (int)GetValue(ItemIndexProperty);
			set => SetValue(ItemIndexProperty, value);
		}

		public static readonly BindableProperty ItemsInSectionProperty =
			BindableProperty.Create(nameof(ItemsInSection), typeof(int), typeof(VirtualViewCell), -1,
				propertyChanged: (bindableObj, oldValue, newValue) =>
				{
					if (bindableObj is VirtualViewCell self)
					{
						self.Resources[nameof(ItemsInSection)] = newValue;
					}
				});

		public int ItemsInSection
		{
			get => (int)GetValue(ItemsInSectionProperty);
			set => SetValue(ItemsInSectionProperty, value);
		}

		public static readonly BindableProperty NumberOfSectionsProperty =
			BindableProperty.Create(nameof(NumberOfSections), typeof(int), typeof(VirtualViewCell), -1,
				propertyChanged: (bindableObj, oldValue, newValue) =>
				{
					if (bindableObj is VirtualViewCell self)
					{
						self.Resources[nameof(NumberOfSections)] = newValue;
					}
				});

		public int NumberOfSections
		{
			get => (int)GetValue(NumberOfSectionsProperty);
			set => SetValue(NumberOfSectionsProperty, value);
		}



		public static readonly BindableProperty IsGlobalHeaderProperty =
			BindableProperty.Create(nameof(IsGlobalHeader), typeof(bool), typeof(VirtualViewCell), false,
				propertyChanged: (bindableObj, oldValue, newValue) =>
				{
					if (bindableObj is VirtualViewCell self)
					{
						self.Resources[nameof(IsGlobalHeader)] = newValue;
					}
				});

		public bool IsGlobalHeader
		{
			get => (bool)GetValue(IsGlobalHeaderProperty);
			set => SetValue(IsGlobalHeaderProperty, value);
		}


		public static readonly BindableProperty IsGlobalFooterProperty =
			BindableProperty.Create(nameof(IsGlobalFooter), typeof(bool), typeof(VirtualViewCell), false,
				propertyChanged: (bindableObj, oldValue, newValue) =>
				{
					if (bindableObj is VirtualViewCell self)
					{
						self.Resources[nameof(IsGlobalFooter)] = newValue;
					}
				});

		public bool IsGlobalFooter
		{
			get => (bool)GetValue(IsGlobalFooterProperty);
			set => SetValue(IsGlobalFooterProperty, value);
		}

		public static readonly BindableProperty IsSectionHeaderProperty =
			BindableProperty.Create(nameof(IsSectionHeader), typeof(bool), typeof(VirtualViewCell), false,
				propertyChanged: (bindableObj, oldValue, newValue) =>
				{
					if (bindableObj is VirtualViewCell self)
					{
						self.Resources[nameof(IsSectionHeader)] = newValue;
					}
				});

		public bool IsSectionHeader
		{
			get => (bool)GetValue(IsSectionHeaderProperty);
			set => SetValue(IsSectionHeaderProperty, value);
		}


		public static readonly BindableProperty KindProperty =
			BindableProperty.Create(nameof(Kind), typeof(PositionKind), typeof(VirtualViewCell), PositionKind.Item,
				propertyChanged: (bindableObj, oldValue, newValue) =>
				{
					if (bindableObj is VirtualViewCell self)
					{
						self.Resources[nameof(Kind)] = newValue;
					}
				});

		public PositionKind Kind
		{
			get => (PositionKind)GetValue(KindProperty);
			set => SetValue(KindProperty, value);
		}


		public bool IsLastItemInSection => ItemIndex >= ItemsInSection - 1;
		public bool IsNotLastItemInSection => !IsLastItemInSection;
		public bool IsFirstItemInSection => ItemIndex == 0;
		public bool IsNotFirstItemInSection => !IsFirstItemInSection;


		protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			base.OnPropertyChanged(propertyName);

			if (propertyName == IsSelectedProperty.PropertyName
				|| propertyName == UnselectedBackgroundProperty.PropertyName
				|| propertyName == SelectedBackgroundProperty.PropertyName)
			{
				UpdateBackground();
			}

			if (propertyName == ItemIndexProperty.PropertyName
				|| propertyName == SectionIndexProperty.PropertyName
				|| propertyName == NumberOfSectionsProperty.PropertyName
				|| propertyName == ItemsInSectionProperty.PropertyName)
			{
				OnPropertyChanged(nameof(IsNotFirstItemInSection));
				OnPropertyChanged(nameof(IsNotLastItemInSection));
				OnPropertyChanged(nameof(IsFirstItemInSection));
				OnPropertyChanged(nameof(IsLastItemInSection));
			}
		}

		void UpdateBackground()
		{
			var c = IsSelected ? SelectedBackground : UnselectedBackground;
			Background = c;
		}
	}
}
