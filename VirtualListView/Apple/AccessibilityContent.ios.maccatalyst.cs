#nullable enable
using Foundation;
using UIKit;

namespace Microsoft.Maui;

// Reads a row's native content the way VoiceOver would, to decide whether the row can be presented
// as a single element and what that element should say.
internal static class AccessibilityContent
{
	internal readonly record struct Summary(string? Label, bool Interactive, bool Heading);

	// Traits that mean an element does something on its own, so grouping it into the row would hide it.
	const UIAccessibilityTrait InteractiveTraits = UIAccessibilityTrait.Button | UIAccessibilityTrait.Link
		| UIAccessibilityTrait.Adjustable | UIAccessibilityTrait.KeyboardKey | UIAccessibilityTrait.SearchField
		| UIAccessibilityTrait.AllowsDirectInteraction;

	static readonly NSString AccessibilityElementsKey = new("accessibilityElements");

	public static Summary Inspect(UIView root)
	{
		var walk = new Walk();
		walk.Visit(root);
		return new(RowAccessibility.ComposeLabel(walk.Parts), walk.Interactive, walk.Heading);
	}

	sealed class Walk
	{
		public readonly List<string?> Parts = new();
		public bool Interactive;
		public bool Heading;

		public void Visit(NSObject element)
		{
			if (Interactive)
				return;

			if (element is UIView view)
			{
				if (view.Hidden || view.Alpha < 0.01f || view.AccessibilityElementsHidden)
					return;
				// Controls, nested scrolling (carousels, editors) and gesture targets are interactive
				// whether or not they are accessibility elements themselves.
				if (view is UIControl or UIScrollView || HasTapGesture(view))
				{
					Interactive = true;
					return;
				}
				if (view.IsAccessibilityElement)
				{
					Read(view.AccessibilityTraits, view.AccessibilityLabel, view.AccessibilityValue);
					return;
				}
				// A custom container's explicit elements replace its subviews for VoiceOver.
				if (view.ValueForKey(AccessibilityElementsKey) is NSArray { Count: > 0 } elements)
				{
					for (nuint i = 0; i < elements.Count; i++)
						Visit(elements.GetItem<NSObject>(i));
					return;
				}
				foreach (var subview in view.Subviews)
					Visit(subview);
			}
			else if (element is UIAccessibilityElement custom && custom.IsAccessibilityElement)
				Read((UIAccessibilityTrait)custom.AccessibilityTraits, custom.AccessibilityLabel, custom.AccessibilityValue);
		}

		void Read(UIAccessibilityTrait traits, string? label, string? value)
		{
			if ((traits & InteractiveTraits) != 0)
			{
				Interactive = true;
				return;
			}
			if ((traits & UIAccessibilityTrait.Header) != 0)
				Heading = true;
			Parts.Add(label);
			Parts.Add(value);
		}

		static bool HasTapGesture(UIView view)
		{
			if (view.GestureRecognizers is not { } recognizers)
				return false;
			foreach (var recognizer in recognizers)
				if (recognizer.Enabled && recognizer is UITapGestureRecognizer or UILongPressGestureRecognizer)
					return true;
			return false;
		}
	}
}
