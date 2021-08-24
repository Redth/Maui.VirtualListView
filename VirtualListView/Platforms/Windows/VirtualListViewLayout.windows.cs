using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Maui
{
	public abstract partial class VirtualListViewLayout
	{
		public abstract Microsoft.UI.Xaml.Controls.Layout CreateNativeLayout();
	}
}
