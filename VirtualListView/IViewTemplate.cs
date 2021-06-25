
using System;

namespace Microsoft.Maui
{
	public interface IPositionInfo
	{
		PositionInfo PositionInfo { get; }
	}

	public interface IViewTemplate
	{
		Type ViewType { get; }
	}
}
