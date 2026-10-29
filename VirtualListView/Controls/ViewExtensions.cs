#nullable enable
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.Maui.Controls;

internal static class ViewExtensions
{
	sealed record Identity(long Value);
	static readonly ConditionalWeakTable<object, Identity> identities = new();
	static long nextIdentity;

	internal static string GetStableIdentity(this object? value)
		=> value is null ? "NIL" : identities.GetValue(value,
			_ => new Identity(Interlocked.Increment(ref nextIdentity))).Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

	internal static string GetDataTemplateId(this DataTemplate? template)
		=> template.GetStableIdentity();

	internal static void RemoveLogicalView(this Element parent, IView view)
	{
		if (view is Element child && ReferenceEquals(child.Parent, parent))
			parent.RemoveLogicalChild(child);
	}

	internal static void AddLogicalView(this Element parent, IView view)
	{
		if (view is Element child && !ReferenceEquals(child.Parent, parent))
			parent.AddLogicalChild(child);
	}
}
