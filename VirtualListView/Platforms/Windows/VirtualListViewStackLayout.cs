using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Maui
{
	public partial class VirtualListViewStackLayout
	{
		public override Microsoft.UI.Xaml.Controls.Layout CreateNativeLayout()
			=> new Microsoft.UI.Xaml.Controls.StackLayout()
			{
				Orientation = this.Orientation == ListOrientation.Vertical
					? UI.Xaml.Controls.Orientation.Vertical
					: UI.Xaml.Controls.Orientation.Horizontal
			};
	}
}
