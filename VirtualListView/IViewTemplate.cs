
using System;

namespace Microsoft.Maui
{
    public interface IViewTemplate
	{
		IView CreateView(PositionInfo positionInfo);
	}
}
