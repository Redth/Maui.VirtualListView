namespace Microsoft.Maui.Controls;

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

	bool isSelected = false;
	public bool IsSelected
	{
		get => isSelected;
		set
		{
			isSelected = value;
			this.Resources[nameof(IsSelected)] = isSelected;
			this.OnPropertyChanged(nameof(IsSelected));

			UpdateBackground();
		}
	}

	int sectionIndex = -1;
	public int SectionIndex
	{
		get => sectionIndex;
		set
		{
			sectionIndex = value;
			this.Resources[nameof(SectionIndex)] = sectionIndex;
			this.OnPropertyChanged(nameof(SectionIndex));
		}
	}

	int itemIndex = -1;
	public int ItemIndex
	{
		get => itemIndex;
		set
		{
			itemIndex = value;
			this.Resources[nameof(ItemIndex)] = itemIndex;
			this.OnPropertyChanged(nameof(ItemIndex));
			this.OnPropertyChanged(nameof(IsFirstItemInSection));
			this.OnPropertyChanged(nameof(IsNotFirstItemInSection));
			this.OnPropertyChanged(nameof(IsLastItemInSection));
			this.OnPropertyChanged(nameof(IsNotLastItemInSection));
		}
	}

	int itemsInSection = 0;

	public int ItemsInSection
	{
		get => itemsInSection;
		set
		{
			itemsInSection = value;
			this.Resources[nameof(ItemsInSection)] = itemsInSection;
			this.OnPropertyChanged(nameof(ItemsInSection));
		}
	}

	int numberOfSections = 0;
	public int NumberOfSections
	{
		get => numberOfSections;
		set
		{
			numberOfSections = value;
			this.Resources[nameof(NumberOfSections)] = numberOfSections;
			this.OnPropertyChanged(nameof(NumberOfSections));
		}
	}

	bool isGlobalHeader = false;
	public bool IsGlobalHeader
	{
		get => isGlobalHeader;
		set
		{
			isGlobalHeader = value;
			this.Resources[nameof(IsGlobalHeader)] = IsGlobalHeader;
			this.OnPropertyChanged(nameof(IsGlobalHeader));
		}
	}

	bool isGlobalFooter = false;
	public bool IsGlobalFooter
	{
		get => isGlobalFooter;
		set
		{
			isGlobalFooter = value;
			this.Resources[nameof(IsGlobalFooter)] = isGlobalFooter;
			this.OnPropertyChanged(nameof(IsGlobalFooter));
		}
	}

	bool isSectionHeader = false;
	public bool IsSectionHeader
	{
		get => isSectionHeader;
		set
		{
			isSectionHeader = value;
			this.Resources[nameof(IsSectionHeader)] = isSectionHeader;
			this.OnPropertyChanged(nameof(IsSectionHeader));
		}
	}

	bool isSectionFooter = false;
	public bool IsSectionFooter
	{
		get => isSectionFooter;
		set
		{
			isSectionFooter = value;
			this.Resources[nameof(IsSectionFooter)] = isSectionFooter;
			this.OnPropertyChanged(nameof(IsSectionFooter));
		}
	}

	PositionKind kind = PositionKind.Item;
	public PositionKind Kind
	{
		get => kind;
		set
		{
			kind = value;
			this.Resources[nameof(Kind)] = kind;
			this.OnPropertyChanged(nameof(Kind));
		}
	}


	public bool IsLastItemInSection => ItemIndex >= ItemsInSection - 1;
	public bool IsNotLastItemInSection => !IsLastItemInSection;
	public bool IsFirstItemInSection => ItemIndex == 0;
	public bool IsNotFirstItemInSection => !IsFirstItemInSection;

	void UpdateBackground()
	{
		var c = IsSelected ? SelectedBackground : UnselectedBackground;
		Background = c;
	}
}
