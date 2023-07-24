using System;
using System.Reflection;

namespace Microsoft.Maui.Controls;

internal static class ViewExtensions
{
	static PropertyInfo DataTemplateIdPropertyInfo;

	internal static string GetDataTemplateId(this DataTemplate dataTemplate)
	{
		DataTemplateIdPropertyInfo ??= dataTemplate.GetType().GetProperty("Id", BindingFlags.Instance | BindingFlags.NonPublic);

		return DataTemplateIdPropertyInfo.GetValue(dataTemplate)?.ToString();

	}

	static MethodInfo removeLogicalChildMethod = null;

	internal static void RemoveLogicalChild(this Element parent, IView view)
	{
		if (view is Element elem)
		{
			removeLogicalChildMethod ??= GetLogicalChildMethod(parent, "RemoveLogicalChildInternal", "RemoveLogicalChild");
			removeLogicalChildMethod?.Invoke(parent, new[] { elem });
		}
	}

	static MethodInfo addLogicalChildMethod = null;

	internal static void AddLogicalChild(this Element parent, IView view)
	{
		if (view is Element elem)
		{
			addLogicalChildMethod ??= GetLogicalChildMethod(parent, "AddLogicalChildInternal", "AddLogicalChild");
			addLogicalChildMethod?.Invoke(parent, new[] { elem });
		}
	}

	static MethodInfo GetLogicalChildMethod(Element parent, string internalName, string publicName)
	{
		var internalMethod = parent.GetType().GetMethod(
				internalName,
				BindingFlags.Instance | BindingFlags.NonPublic,
				new[] { typeof(Element) });

		if (internalMethod is null)
		{
			internalMethod = parent.GetType().GetMethod(
				publicName,
				BindingFlags.Instance | BindingFlags.Public,
				new[] { typeof(Element) });
		}

		return internalMethod;
	}
}

