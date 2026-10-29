#nullable enable
using Microsoft.Maui.Controls;

namespace Microsoft.Maui;

// One measurement per retained slot, not a size cache indexed by the data source.
internal sealed class RetainedContentLayout : IDisposable
{
	readonly IView view;
	readonly Func<Action, bool> dispatch;
	readonly Action<bool> invalidate;
	Size? measured;
	Size? notified;
	double crossConstraint;
	ListOrientation orientation;
	long revision;
	long measuredRevision = -1;
	int pending;
	int disposed;

	public RetainedContentLayout(IView view, Func<Action, bool> dispatch, Action<bool> invalidate)
	{
		this.view = view;
		this.dispatch = dispatch;
		this.invalidate = invalidate;
		if (view is VisualElement element)
			element.MeasureInvalidated += MeasureInvalidated;
	}

	public Size Measure(ListOrientation axis, double cross, bool force = false)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(cross);
		if (!double.IsFinite(cross))
			throw new ArgumentOutOfRangeException(nameof(cross), "Retained content requires a finite cross-axis constraint.");

		var currentRevision = Interlocked.Read(ref revision);
		if (!force && measured is Size cached && orientation == axis && crossConstraint == cross && measuredRevision == currentRevision)
			return cached;

		var desired = axis == ListOrientation.Vertical
			? view.Measure(cross, double.PositiveInfinity)
			: view.Measure(double.PositiveInfinity, cross);
		if (!double.IsFinite(desired.Width) || !double.IsFinite(desired.Height) || desired.Width < 0 || desired.Height < 0)
			throw new InvalidOperationException("Retained content returned a non-finite or negative desired size.");

		orientation = axis;
		crossConstraint = cross;
		measuredRevision = currentRevision;
		// IView.Measure/Arrange own root margins. Native hosts must not add them again.
		measured = axis == ListOrientation.Vertical ? new Size(cross, desired.Height) : new Size(desired.Width, cross);
		notified ??= measured;
		return measured.Value;
	}

	public void Arrange(ListOrientation axis, Size accepted)
	{
		if (!double.IsFinite(accepted.Width) || !double.IsFinite(accepted.Height) || accepted.Width < 0 || accepted.Height < 0)
			throw new ArgumentOutOfRangeException(nameof(accepted));
		Measure(axis, axis == ListOrientation.Vertical ? accepted.Width : accepted.Height);
		view.Arrange(new Rect(Point.Zero, accepted));
	}

	// Round outward at the native pixel boundary so wrapping text is not clipped.
	internal static int ToPixels(double logical, double density)
	{
		if (!double.IsFinite(logical) || logical < 0 || !double.IsFinite(density) || density <= 0)
			throw new ArgumentOutOfRangeException(nameof(logical));
		return checked((int)Math.Ceiling(logical * density));
	}

	void MeasureInvalidated(object? sender, EventArgs e)
	{
		Interlocked.Increment(ref revision);
		if (Volatile.Read(ref disposed) != 0 || Interlocked.Exchange(ref pending, 1) != 0)
			return;
		if (!dispatch(() =>
		{
			Interlocked.Exchange(ref pending, 0);
			if (Volatile.Read(ref disposed) != 0)
				return;
			// A native arrange may already have remeasured, but not accepted the new row extent.
			var previous = notified;
			var next = measured.HasValue ? Measure(orientation, crossConstraint) : (Size?)null;
			notified = next;
			invalidate(previous != next || !previous.HasValue);
		}))
		{
			Interlocked.Exchange(ref pending, 0);
			throw new InvalidOperationException("The owning VirtualListView dispatcher rejected a content layout update.");
		}
	}

	public void Dispose()
	{
		Interlocked.Exchange(ref disposed, 1);
		if (view is VisualElement element)
			element.MeasureInvalidated -= MeasureInvalidated;
	}
}
